using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.AddServiceDefaults();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("My name is John Doe.");
logger.LogInformation("My email is john.doe@outlook.com.");

await host.RunAsync();