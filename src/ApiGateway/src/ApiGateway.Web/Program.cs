using Common.Logging.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Logging.AddStructuredConsoleLogging(
    builder.Environment.ApplicationName,
    builder.Environment.IsDevelopment());

var app = builder.Build();

app.UseLoggingMiddleware();

app.MapReverseProxy();

app.Run();
