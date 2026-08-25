using System.Reflection;
using AuthService.Api.BackgroundServices;
using AuthService.Infrastructure;
using AuthService.Persistence;
using AuthService.Persistence.SeedData;
using Shared.Logging.DependencyInjection;
using Shared.Helpers.DependencyInjection;
using AuthService.Api.WellKnown;
using Shared.Http.Authentication;
using Shared.Http.Response.Middlewares;
using Microsoft.AspNetCore.Authentication;
using AuthService.Api.Middlewares;
using Shared.Logging.Middlewares;

var builder = WebApplication.CreateBuilder(args);

//~ Begin - Register services from layers
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddInfrastructureServices();
//~ End

builder.Services.AddHostedService<RabbitMqInitializerBackgroundService>();

builder.AddOptionsFromAssemblies(
    Assembly.GetAssembly(typeof(AuthService.Persistence.ServiceRegistration))!,
    Assembly.GetAssembly(typeof(AuthService.Infrastructure.ServiceRegistration))!
);

builder.Services.AddAuthentication(ApiGatewayConstants.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, ApiGatewayAuthHandler>(ApiGatewayConstants.AuthenticationScheme, options => { });
builder.Services.AddAuthorization();

builder.Logging.AddStructuredConsoleLogging(
    builder.Environment.ApplicationName,
    builder.Environment.IsProduction());

var app = builder.Build();

app.UseMiddleware<LoggingScopeMiddleware>();
app.UseMiddleware<GlobalExceptionHandler>();

app.UseMiddleware<AuthErrorResponseMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapWellKnownEndpoints();

// Seed data
await app.Services.SeedDataAuthDbContextAsync(app.Configuration);

app.Run();
