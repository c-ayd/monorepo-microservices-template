using System.Reflection;
using AuthService.Api.BackgroundServices;
using AuthService.Infrastructure;
using AuthService.Persistence;
using Shared.Logging.DependencyInjection;
using Shared.AspNetCore.Helpers.DependencyInjection;
using AuthService.Api.WellKnown;
using Shared.Http.Authentication;
using Shared.Http.Response.Middlewares;
using Microsoft.AspNetCore.Authentication;
using AuthService.Api.Middlewares;
using Shared.Logging.Middlewares;
using AuthService.Application.Features.AccountEndpoints;
using AuthService.Application;
using Shared.Http.DependencyInjection;
using Shared.Http.Authentication.Constants;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddInfrastructureServices();
builder.Services.AddApplicationServices();

builder.Services.AddHealthChecks();

builder.Services.AddHostedService<RabbitMqPublisherBackgroundService>();
builder.Services.AddHostedService<RedisInitializerBackgroundServices>();

builder.RegisterOptionsFromAssemblies(
    Assembly.GetAssembly(typeof(AuthService.Persistence.ServiceRegistration))!,
    Assembly.GetAssembly(typeof(AuthService.Infrastructure.ServiceRegistration))!,
    Assembly.GetAssembly(typeof(AuthService.Application.ServiceRegistration))!
);

builder.Services.AddValidatorsFromAssembly(Assembly.GetAssembly(typeof(AuthService.Application.ServiceRegistration))!);

builder.Services.AddAuthentication(ApiGatewayAuthKeys.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, ApiGatewayAuthHandler>(ApiGatewayAuthKeys.AuthenticationScheme, options => { });
builder.Services.AddAuthorization();

builder.Logging.AddStructuredConsoleLogging(
    builder.Environment.IsProduction(),
    healthEndpoint: "/health");

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<LoggingMiddleware>();

app.UseMiddleware<AuthErrorResponseMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<AccountPreferenceMiddleware>();

app.MapWellKnownEndpoints();
app.MapAccountEndpoints();

app.MapHealthChecks("/health");

app.Run();
