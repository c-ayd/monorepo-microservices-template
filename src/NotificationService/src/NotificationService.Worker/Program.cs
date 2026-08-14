using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NotificationService.Worker.Abstractions;
using NotificationService.Worker.DbContexts;
using NotificationService.Worker.Options;
using NotificationService.Worker.Services;
using Shared.AspNetCore.Helpers.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.AddOptionsFromAssembly(Assembly.GetExecutingAssembly());

var connStrings = builder.Configuration.GetSection(ConnectionStringsOptions.Key).Get<ConnectionStringsOptions>()!;
builder.Services.AddDbContext<TemplateDbContext>(_ => _.UseNpgsql(connStrings.TemplateDb));

builder.Services.AddSingleton<IEmailService, SmtpService>();

builder.Services.AddSingleton<RabbitMqConnectionService>();

builder.Services.AddHostedService<EmailWorker>();

var host = builder.Build();
host.Run();
