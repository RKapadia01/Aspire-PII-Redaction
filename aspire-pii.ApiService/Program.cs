using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.Logging.ClearProviders();
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/pii", ([FromBody] PiiData input) =>
{
    
    logger.LogInformation("Received PII data: {Text}", input.Text);
    
    Console.WriteLine("Received PII data:");
    Console.WriteLine(input.Text);
    
    return Results.Ok(new { Message = "PII data posted successfully." });
});

app.MapDefaultEndpoints();

app.Run();

record PiiData()
{
    public string? Text { get; set; }
}