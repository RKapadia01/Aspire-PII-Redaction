using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<SampleService>("sampleService")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317");

builder.Build().Run();
