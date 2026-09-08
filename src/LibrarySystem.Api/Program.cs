using Carter;
using FluentValidation;
using LibrarySystem.Api.Extensions;
using LibrarySystem.Api.Shared.Exceptions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddStructuredLogs();

builder.Services.AddOpenApiWithExamples();

builder.AddCarter();
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GrpcExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

string serviceUrl = builder.Configuration.GetRequiredValue("GrpcSettings:ServiceUrl");
string healthCheckUrl = builder.Configuration.GetRequiredValue("GrpcSettings:HealthCheckUrl");

builder.Services.AddInfrastructureClients(serviceUrl);
builder.Services.AddInfrastructureHealthChecks(healthCheckUrl);

var app = builder.Build();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Library System API v1");
    });
}

app.UseExceptionHandler();
app.MapCarter();
app.MapInfrastructureHealthChecks();

app.Run();

public interface IApiMarker { }