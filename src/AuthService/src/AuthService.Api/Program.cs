using System.Reflection;
using AuthService.Infrastructure;
using AuthService.Persistence;
using AuthService.Persistence.SeedData;
using Common.Logging.DependencyInjection;
using Shared.AspNetCore.Helpers.Options;

var builder = WebApplication.CreateBuilder(args);

//~ Begin - Register services from layers
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddInfrastructureServices();
//~ End

builder.AddOptionsFromAssemblies(
    Assembly.GetAssembly(typeof(AuthService.Persistence.ServiceRegistration))!,
    Assembly.GetAssembly(typeof(AuthService.Infrastructure.ServiceRegistration))!
);

builder.Logging.AddStructuredConsoleLogging(
    builder.Environment.ApplicationName,
    builder.Environment.IsDevelopment());

var app = builder.Build();

app.UseLoggingMiddleware();

// Seed data
await app.Services.SeedDataAuthDbContextAsync(app.Configuration);

app.Run();
