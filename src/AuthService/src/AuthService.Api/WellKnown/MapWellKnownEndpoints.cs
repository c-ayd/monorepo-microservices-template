using AuthService.Application.Abstractions.Authentication;
using AuthService.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Api.WellKnown
{
    public static class WellKnownEndpoints
    {
        public static void MapWellKnownEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/.well-known");

            group.MapGet("/openid-configuration", (IOptions<JwtOptions> jwtOptions) =>
            {
                return Results.Ok(new
                {
                    issuer = jwtOptions.Value.Issuer,
                    jwks_uri = $"{jwtOptions.Value.Issuer}/.well-known/jwks.json",
                    id_token_signing_alg_values_supported = new[] { "RS256" }
                });
            });

            group.MapGet("/jwks.json", (IJwtKeyService jwtKeyService) =>
            {
                return Results.Ok(new
                {
                    keys = new[]
                    {
                        new
                        {
                            kty = "RSA",
                            kid = jwtKeyService.PublicKey.KeyId,
                            use = "sig",
                            alg = "RS256",
                            n = Base64UrlEncoder.Encode(jwtKeyService.PublicKey.Parameters.Modulus),
                            e = Base64UrlEncoder.Encode(jwtKeyService.PublicKey.Parameters.Exponent)
                        }
                    }
                });
            });
        }
    }
}
