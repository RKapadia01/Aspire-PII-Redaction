var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.aspire_pii_ApiService>("apiservice");

builder.AddProject<Projects.aspire_pii_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
