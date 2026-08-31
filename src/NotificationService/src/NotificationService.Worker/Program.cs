using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NotificationService.Worker.Abstractions;
using NotificationService.Worker.DbContexts;
using NotificationService.Worker.Options;
using NotificationService.Worker.SeedData;
using NotificationService.Worker.Services;
using NotificationService.Worker.BackgroundServices;
using Shared.Helpers.DependencyInjection;
using Shared.Logging.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.RegisterOptionsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddDbContext<TemplateDbContext>(_ => 
    _.UseNpgsql(builder.Configuration.GetConnectionString(nameof(ConnectionStringsOptions.TemplateDb))));

builder.Services.AddSingleton<IEmailService, SmtpService>();

builder.Services.AddSingleton<RabbitMqConnectionService>();
builder.Services.AddSingleton<TemplateService>();

builder.Services.AddHostedService<TemplateBackgroundService>();
builder.Services.AddHostedService<EmailBackgroundService>();

builder.Logging.AddStructuredConsoleLogging(
    "Notification Service",
    builder.Environment.IsProduction());

var host = builder.Build();

// Seed data
await host.SeedDataTemplateDbAsync();

host.Run();
