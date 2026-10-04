using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NotificationService.Worker.Abstractions;
using NotificationService.Worker.DbContexts;
using NotificationService.Worker.Options;
using NotificationService.Worker.Services;
using NotificationService.Worker.BackgroundServices;
using Shared.AspNetCore.Helpers.DependencyInjection;
using Shared.Logging.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.RegisterOptionsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddDbContext<TemplateDbContext>(_ => 
    _.UseNpgsql(builder.Configuration.GetConnectionString(nameof(ConnectionStringsOptions.TemplateDb))));

builder.Services.AddSingleton<IEmailService, SmtpService>();

builder.Services.AddSingleton<ITemplateService, TemplateService>();

builder.Services.AddHostedService<TemplateBackgroundService>();
builder.Services.AddHostedService<EmailBackgroundService>();

builder.Logging.AddStructuredConsoleLogging(
    builder.Environment.IsProduction(),
    healthEndpoint: "/health");

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();
