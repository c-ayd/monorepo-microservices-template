using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using ApiGateway.Web.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Shared.Constants;
using Shared.Http.Authentication.Constants;
using Shared.Test.Helpers.Fixtures;

namespace ApiGateway.Test.Integration.Web.Collections
{
    [CollectionDefinition(nameof(ApiGatewayCollection))]
    public class ApiGatewayCollection : ICollectionFixture<ApiGatewayCollectionCluster>
    {
    }

    public class ApiGatewayCollectionCluster : IAsyncLifetime
    {
        public RedisFixture TokenBlacklistRedisFixture { get; private set; }
        public TestHostFixture DownstreamServiceFixture { get; private set; }

        public ApiGatewayWebAppFactory ApiGatewayWebApp { get; private set; } = null!;

        public ApiGatewayCollectionCluster()
        {
            TokenBlacklistRedisFixture = new RedisFixture();
            DownstreamServiceFixture = new TestHostFixture();
        }

        public async Task InitializeAsync()
        {
            await Task.WhenAll(
                TokenBlacklistRedisFixture.InitializeAsync(),
                DownstreamServiceFixture.InitializeAsync(
                    addConfiguration: null,
                    configureServices: null,
                    configureApp: null,
                    configureEndpoints: ConfigureEndpoints,
                    port: 5000)
            );

            ApiGatewayWebApp = new ApiGatewayWebAppFactory(
                new ConnectionStringsOptions()
                {
                    AuthTokenBlacklistRedis = TokenBlacklistRedisFixture.GetConnectionString()
                });
            ApiGatewayWebApp.StartServer();
        }

        public async Task DisposeAsync()
        {
            await Task.WhenAll(
                TokenBlacklistRedisFixture.DisposeAsync(),
                DownstreamServiceFixture.DisposeAsync()
            );

            await ApiGatewayWebApp.DisposeAsync();
        }

        public class ApiGatewayWebAppFactory : WebAppFactoryFixture<Program>
        {
            private readonly ConnectionStringsOptions _connectionStringsOptions;

            public ApiGatewayWebAppFactory(ConnectionStringsOptions connectionStringsOptions)
            {
                _connectionStringsOptions = connectionStringsOptions;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection([
                        new KeyValuePair<string, string?>($"{ConnectionStringsOptions.Key}:{nameof(ConnectionStringsOptions.AuthTokenBlacklistRedis)}",
                            _connectionStringsOptions.AuthTokenBlacklistRedis)
                    ]);
                });
            }
        }

        private void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
        {
            // Well known endpoints
            endpoints.MapGet("/.well-known/openid-configuration", async (context) =>
            {
                await Results.Ok(new
                {
                    issuer = "http://localhost:5000",
                    jwks_uri = "http://localhost:5000/.well-known/jwks.json",
                    id_token_signing_alg_values_supported = new[] { "RS256" }
                }).ExecuteAsync(context);
            });
            endpoints.MapGet("/.well-known/jwks.json", async (context) =>
            {
                var publicPem = await File.ReadAllTextAsync("test_jwt_public.pem");
                var rsa = RSA.Create();
                rsa.ImportFromPem(publicPem);
                var parameters = rsa.ExportParameters(false);

                await Results.Ok(new
                {
                    keys = new[]
                    {
                        new
                        {
                            kty = "RSA",
                            kid = "v1",
                            use = "sig",
                            alg = "RS256",
                            n = Base64UrlEncoder.Encode(parameters.Modulus),
                            e = Base64UrlEncoder.Encode(parameters.Exponent)
                        }
                    }
                }).ExecuteAsync(context);
            });

            // Test endpoins
            endpoints.MapGet("/test/generate-access-token", async (
                HttpContext context,
                Guid accountId,
                string[]? roles,
                bool? emailVerified,
                string? preferredLang,
                bool? issuedInPast,
                bool? expired) =>
            {
                var claims = new List<Claim>()
                {
                    new Claim(ApiGatewayAuthKeys.Claims.Id.ClaimType, accountId.ToString()),
                    new Claim(ApiGatewayAuthKeys.Claims.EmailVerified.ClaimType, emailVerified.HasValue ? emailVerified.ToString()!.ToLower() : false.ToString().ToLower()),
                    new Claim(ApiGatewayAuthKeys.Claims.PreferredLanguage.ClaimType, preferredLang != null ? preferredLang : SupportedLanguages.DefaultLanguage),
                    new Claim(ApiGatewayAuthKeys.Claims.IssuedAt.ClaimType, issuedInPast.HasValue && issuedInPast.Value ?
                        DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds().ToString() :
                        DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds().ToString()),
                };

                if (roles != null)
                {
                    foreach (var role in roles)
                    {
                        claims.Add(new Claim(ApiGatewayAuthKeys.Claims.Roles.ClaimType, role));
                    }
                }

                var privatePem = await File.ReadAllTextAsync("test_jwt_private.pem");
                var rsa = RSA.Create();
                rsa.ImportFromPem(privatePem);
                var key = new RsaSecurityKey(rsa)
                {
                    KeyId = "v1"
                };

                var token = new JwtSecurityToken(
                    issuer: "http://localhost:5000",
                    audience: "https://localhost:7000",
                    claims: claims,
                    expires: expired.HasValue && expired.Value ? DateTimeOffset.UtcNow.AddDays(-1).UtcDateTime : DateTimeOffset.UtcNow.AddDays(1).UtcDateTime,
                    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
                );

                await Results.Content(new JwtSecurityTokenHandler().WriteToken(token), "text/plain")
                    .ExecuteAsync(context);
            });
            endpoints.MapGet("/test/get-headers", async (context) =>
            {
                var headers = new Dictionary<string, string?>();
                foreach (var userClaim in ApiGatewayAuthKeys.Claims.AllUserClaims)
                {
                    if (context.Request.Headers.TryGetValue(userClaim.HeaderKey, out var headerValue))
                    {
                        headers.Add(userClaim.HeaderKey, headerValue.ToString());
                    }
                    else
                    {
                        headers.Add(userClaim.HeaderKey, null);
                    }
                }

                if (context.Request.Headers.TryGetValue(HeaderNames.Authorization, out var token))
                {
                    headers.Add(HeaderNames.Authorization, token);
                }
                else
                {
                    headers.Add(HeaderNames.Authorization, null);
                }

                await Results.Ok(headers).ExecuteAsync(context);
            });
        }
    }
}
