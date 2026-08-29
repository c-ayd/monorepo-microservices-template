using ApiGateway.Web.Middlewares;
using ApiGateway.Web.Options;
using ApiGateway.Web.Transforms.Request;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared.Http.Authentication;
using Shared.Http.Response.Middlewares;
using Shared.Logging.DependencyInjection;
using Shared.Logging.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms<JwtBearerTransform>();

builder.Logging.AddStructuredConsoleLogging(
    builder.Environment.ApplicationName,
    builder.Environment.IsProduction());

var jwtOptions = builder.Configuration.GetSection(JwtOptions.Key).Get<JwtOptions>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = jwtOptions.Authority;

        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            
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

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<LoggingScopeMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseMiddleware<AuthErrorResponseMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();
