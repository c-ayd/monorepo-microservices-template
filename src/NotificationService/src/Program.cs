using System.Reflection;
using Shared.AspNetCore.Helpers.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.AddOptionsFromAssembly(Assembly.GetExecutingAssembly());

var host = builder.Build();
host.Run();
