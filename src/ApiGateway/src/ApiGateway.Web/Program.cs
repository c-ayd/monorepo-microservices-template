using ApiGateway.Web.Middlewares;
using Common.Logging.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Logging.AddStructuredConsoleLogging(
    builder.Environment.ApplicationName,
    builder.Environment.IsProduction());

var app = builder.Build();

app.UseLoggingMiddleware();
app.UseMiddleware<GlobalExceptionHandler>();

app.MapReverseProxy();

app.Run();
