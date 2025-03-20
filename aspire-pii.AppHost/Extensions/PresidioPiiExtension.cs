using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace aspire_pii.AppHost.Extensions;

public static class PresidioPiiExtension
{
    private const string DashboardOtlpUrlVariableName = "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL";
    private const string DashboardOtlpUrlDefaultValue = "http://localhost:18889";
    
    public static IResourceBuilder<ContainerResource> AddPresidioCollector(
        this IDistributedApplicationBuilder builder,
        string configPath = "./config.yaml")
    {
        var url = builder.Configuration[DashboardOtlpUrlVariableName] ?? DashboardOtlpUrlDefaultValue;
        var dashboardOtlpEndpoint = ReplaceLocalhostWithContainerHost(url, builder.Configuration);
        
        var collector = builder
            .AddContainer("presidio-otel-collector", "rohankapadia/presidioredactioncollector:withpresidio")
            .WithEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc")
            .WithEndpoint(port: 4318, targetPort: 4318, name: "otlp-http")
            .WithBindMount(configPath, "/app/config.yaml")
            .WithEnvironment("ASPIRE_OTLP_ENDPOINT", dashboardOtlpEndpoint);

        builder.Services.TryAddLifecycleHook<PresidioEnvironmentHook>();

        return collector;
    }
    
    /// <summary>
    /// Replaces localhost references with container-friendly host names.
    /// Adapted from [Original Repository Name] (https://github.com/practical-otel/opentelemetry-aspire-collector)
    /// under Apache 2.0 license.
    /// </summary>
    private static string ReplaceLocalhostWithContainerHost(string value, IConfiguration configuration)
    {
        var hostName = configuration["AppHost:ContainerHostname"] ?? "host.docker.internal";

        return value.Replace("localhost", hostName, StringComparison.OrdinalIgnoreCase)
            .Replace("127.0.0.1", hostName)
            .Replace("[::1]", hostName);
    }
}

public class PresidioEnvironmentHook : IDistributedApplicationLifecycleHook
{
    private readonly ILogger<PresidioEnvironmentHook> _logger;

    public PresidioEnvironmentHook(ILogger<PresidioEnvironmentHook> logger)
    {
        _logger = logger;
    }

    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        var resources = appModel.GetProjectResources();
        var collectorResource = appModel.Resources.FirstOrDefault(r => r.Name == "presidio-otel-collector");

        if (collectorResource == null)
        {
            _logger.LogWarning("No Presidio collector resource found");
            return Task.CompletedTask;
        }

        if (!collectorResource.TryGetEndpoints(out var allEndpoints))
        {
            _logger.LogWarning("No endpoints found for the Presidio collector");
            return Task.CompletedTask;
        }

        var endpointsList = allEndpoints.ToList();

        var grpcEndpoint = endpointsList.FirstOrDefault(e => e.Name == "otlp-grpc");
        var httpEndpoint = endpointsList.FirstOrDefault(e => e.Name == "otlp-http");
        var endpoint = grpcEndpoint ?? httpEndpoint;
        
        if (endpoint == null)
        {
            _logger.LogWarning("No suitable endpoint for the Presidio collector");
            return Task.CompletedTask;
        }

        if (resources.Count() == 0)
        {
            _logger.LogInformation("No resources to add Environment Variables to");
            return Task.CompletedTask;
        }

        foreach (var resourceItem in resources)
        {
            _logger.LogDebug($"Forwarding Telemetry for {resourceItem.Name} to the Presidio collector");

            resourceItem.Annotations.Add(new EnvironmentCallbackAnnotation((EnvironmentCallbackContext context) =>
            {
                if (context.EnvironmentVariables.ContainsKey("OTEL_EXPORTER_OTLP_ENDPOINT"))
                    context.EnvironmentVariables.Remove("OTEL_EXPORTER_OTLP_ENDPOINT");
                
                context.EnvironmentVariables.Add("OTEL_EXPORTER_OTLP_ENDPOINT", endpoint.AllocatedEndpoint.Address);
            }));
        }

        return Task.CompletedTask;
    }
}