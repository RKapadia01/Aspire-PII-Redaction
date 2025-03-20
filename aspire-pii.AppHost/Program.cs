var builder = DistributedApplication.CreateBuilder(args);

var otelCollector = builder.AddContainer("otel-collector", "rohankapadia/presidioredactioncollector:withpresidio")
    .WithEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc")
    .WithEndpoint(port: 4318, targetPort: 4318, name: "otlp-http")
    .WithBindMount("./config.yaml", "/app/config.yaml");

var apiService = builder.AddProject<Projects.aspire_pii_ApiService>("apiservice")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")
    .WaitFor(otelCollector);

builder.AddProject<Projects.aspire_pii_Web>("webfrontend")
    .WaitFor(otelCollector)
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();