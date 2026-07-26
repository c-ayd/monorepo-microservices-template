using System.Reflection;
using AuthService.Infrastructure;
using AuthService.Persistence;
using AuthService.Persistence.SeedData;
using Shared.AspNetCore.Helpers.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

//~ Begin - Register services from layers
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddInfrastructureServices();
//~ End

builder.AddOptionsFromAssemblies(
    Assembly.GetAssembly(typeof(AuthService.Persistence.ServiceRegistration))!,
    Assembly.GetAssembly(typeof(AuthService.Infrastructure.ServiceRegistration))!
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Seed data
await app.Services.SeedDataAuthDbContextAsync(app.Configuration);

app.Run();
