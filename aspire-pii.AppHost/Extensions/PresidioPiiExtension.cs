using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace aspire_pii.AppHost.Extensions;

public static class PresidioPiiExtension
{
    public static IResourceBuilder<ContainerResource> AddPresidioCollector(
        this IDistributedApplicationBuilder builder,
        string configPath = "./config.yaml")
    {
        var collector = builder
            .AddContainer("presidio-otel-collector", "rohankapadia/presidioredactioncollector:withpresidio")
            .WithEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc")
            .WithEndpoint(port: 4318, targetPort: 4318, name: "otlp-http")
            .WithBindMount(configPath, "/app/config.yaml");

        builder.Services.TryAddLifecycleHook<PresidioEnvironmentHook>();

        return collector;
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