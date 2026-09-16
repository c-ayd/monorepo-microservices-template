using System.Reflection;
using ApiGateway.Web.BackgroundServices;
using ApiGateway.Web.Middlewares;
using ApiGateway.Web.Options;
using ApiGateway.Web.Services;
using ApiGateway.Web.Transforms.Request;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared.Helpers.DependencyInjection;
using Shared.Http.Authentication.Constants;
using Shared.Http.Response.Middlewares;
using Shared.Logging.DependencyInjection;
using Shared.Logging.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms<JwtBearerTransform>();

builder.Services.AddHostedService<RedisInitializerBackgroundServices>();

builder.Services.AddSingleton<TokenBlacklist>();

builder.RegisterOptionsFromAssembly(Assembly.GetExecutingAssembly());

builder.Logging.AddStructuredConsoleLogging(
    "API Gateway",
    builder.Environment.IsProduction());

var jwtOptions = builder.Configuration.GetSection(JwtOptions.Key).Get<JwtOptions>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = jwtOptions.Authority;
        options.RequireHttpsMetadata = false;   // TLS is terminated at the gateway since the Auth Service is in the K8S cluster with no public access.

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            
            ValidIssuer = jwtOptions.Authority,
            ValidAudience = jwtOptions.Audience,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],

            NameClaimType = ApiGatewayAuthKeys.Claims.Id.ClaimType,
            RoleClaimType = ApiGatewayAuthKeys.Claims.Roles.ClaimType
        };

        options.BackchannelHttpHandler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization(config =>
{
    config.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());

    config.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

app.UseMiddleware<LoggingScopeMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseMiddleware<AuthErrorResponseMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();
