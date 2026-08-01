using System.Reflection;
using NotificationService.Worker.Abstractions;
using NotificationService.Worker.Services;
using Shared.AspNetCore.Helpers.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.AddOptionsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddSingleton<IEmailService, SmtpService>();

var host = builder.Build();
host.Run();
