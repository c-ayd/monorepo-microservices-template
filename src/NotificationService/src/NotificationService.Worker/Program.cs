using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NotificationService.Worker.Abstractions;
using NotificationService.Worker.DbContexts;
using NotificationService.Worker.Options;
using NotificationService.Worker.SeedData;
using NotificationService.Worker.Services;
using NotificationService.Worker.BackgroundServices;
using Shared.AspNetCore.Helpers.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.AddOptionsFromAssembly(Assembly.GetExecutingAssembly());

var connStrings = builder.Configuration.GetSection(ConnectionStringsOptions.Key).Get<ConnectionStringsOptions>()!;
builder.Services.AddDbContext<TemplateDbContext>(_ => _.UseNpgsql(connStrings.TemplateDb));

builder.Services.AddSingleton<IEmailService, SmtpService>();

builder.Services.AddSingleton<RabbitMqConnectionService>();
builder.Services.AddSingleton<TemplateService>();

builder.Services.AddHostedService<TemplateBackgroundService>();
builder.Services.AddHostedService<EmailBackgroundService>();

var host = builder.Build();

// Seed data
await host.SeedDataTemplateDbAsync();

host.Run();
