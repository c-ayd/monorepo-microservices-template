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
using AuthService.Application.Features.AccountEndpoints;
using AuthService.Application;
using Shared.Http.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

//~ Begin - Register services from layers
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddInfrastructureServices();
builder.Services.AddApplicationServices();
//~ End

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

builder.Services.AddDataProtection()
    .SetApplicationName("AuthService")
    .PersistKeysToStackExchangeRedis(
        () => RedisInitializerBackgroundServices.DataProtection.Connection!.GetDatabase(),
        "AuthDataProtection")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

builder.Logging.AddStructuredConsoleLogging(
    "Auth Service",
    builder.Environment.IsProduction());

var app = builder.Build();

app.UseMiddleware<LoggingScopeMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseMiddleware<AuthErrorResponseMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<AccountPreferenceMiddleware>();

app.MapWellKnownEndpoints();
app.MapAccountEndpoints();

// Seed data
await app.Services.SeedDataAuthDbContextAsync(app.Configuration);

app.Run();
