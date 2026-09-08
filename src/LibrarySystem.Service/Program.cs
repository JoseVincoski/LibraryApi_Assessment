using FluentValidation;
using LibrarySystem.Service.Extensions;
using LibrarySystem.Service.Infrastructure.Interceptors;

var builder = WebApplication.CreateBuilder(args);

builder.AddStructuredLogs();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<ExceptionInterceptor>();
    options.Interceptors.Add<ValidationInterceptor>();
});
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddInfrastructureHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.ApplyMigrations();
}

app.MapGrpcServices();
app.MapInfrastructureHealthChecks();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();

public interface IServiceMarker { }