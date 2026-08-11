using System.Reflection;
using NotificationService.Worker.Abstractions;
using NotificationService.Worker.Services;
using NotificationService.Workers;
using Shared.AspNetCore.Helpers.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.AddOptionsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddSingleton<IEmailService, SmtpService>();

builder.Services.AddSingleton<RabbitMqConnectionService>();

builder.Services.AddHostedService<EmailWorker>();

var host = builder.Build();
host.Run();
