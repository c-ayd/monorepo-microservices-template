using System.Reflection;
using System.Security.Claims;
using ApiGateway.Web.Transforms.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Shared.Http.Authentication;
using Shared.Http.Authentication.Structures;
using Shared.Test.Generators;
using Yarp.ReverseProxy.Transforms;

namespace ApiGateway.Test.Unit.Web.Transforms.Request
{
    public class JwtBearerTransformTest
    {
        [Fact]
        public async Task ApplyTransform_WhenUserIsAuthenticated_ShouldAddAndRemoveCorrectHeaders()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var role1 = StringGenerator.GenerateAlpha();
            var role2 = StringGenerator.GenerateAlpha();

            var request = new HttpRequestMessage();
            request.Headers.Add(HeaderNames.Authorization, "Bearer " + "TokenValue");

            var claims = new List<Claim>()
            {
                new Claim(ApiGatewayAuthKeys.Claims.Id.ClaimType, userId),
                new Claim(ApiGatewayAuthKeys.Claims.Roles.ClaimType, role1),
                new Claim(ApiGatewayAuthKeys.Claims.Roles.ClaimType, role2)
            };

            UserClaim? claim = null;
            string? claimValue = null;
            foreach (var userClaim in ApiGatewayAuthKeys.Claims.AllUserClaims)
            {
                if (userClaim.HeaderKey == ApiGatewayAuthKeys.Claims.Id.HeaderKey ||
                    userClaim.HeaderKey == ApiGatewayAuthKeys.Claims.Roles.HeaderKey)
                    continue;

                claim = userClaim;
                claimValue = StringGenerator.GenerateAlpha();

                claims.Add(new Claim(userClaim.ClaimType, claimValue));
            }

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            };

            var context = new RequestTransformContext
            {
                ProxyRequest = request,
                HttpContext = httpContext
            };
            var transform = new JwtBearerTransform();

            // Act
            var applyTranformMethod = typeof(JwtBearerTransform).GetMethod("ApplyTransform", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var applyTranform = (ValueTask)applyTranformMethod.Invoke(transform, [context])!;
            await applyTranform;

            // Assert
            Assert.True(request.Headers.Contains(ApiGatewayAuthKeys.Claims.Id.HeaderKey), $"The headers do not contain {ApiGatewayAuthKeys.Claims.Id.HeaderKey}.");
            Assert.Single(request.Headers.GetValues(ApiGatewayAuthKeys.Claims.Id.HeaderKey));
            Assert.Equal(userId, request.Headers.GetValues(ApiGatewayAuthKeys.Claims.Id.HeaderKey).ElementAt(0));

            Assert.True(request.Headers.Contains(ApiGatewayAuthKeys.Claims.Roles.HeaderKey), $"The headers do not contain {ApiGatewayAuthKeys.Claims.Roles.HeaderKey}.");
            Assert.Single(request.Headers.GetValues(ApiGatewayAuthKeys.Claims.Roles.HeaderKey));
            var roles = request.Headers.GetValues(ApiGatewayAuthKeys.Claims.Roles.HeaderKey).FirstOrDefault()!.Split(',');
            Assert.Contains(role1, roles);
            Assert.Contains(role2, roles);

            if (claim != null)
            {
                Assert.True(request.Headers.Contains(claim.HeaderKey), $"The headers do not contain {claim.HeaderKey}.");
                Assert.Single(request.Headers.GetValues(claim.HeaderKey));
                Assert.Equal(claimValue, request.Headers.GetValues(claim.HeaderKey).ElementAt(0));
            }

            Assert.False(request.Headers.Contains(HeaderNames.Authorization), $"The headers contain {HeaderNames.Authorization}.");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ApplyTransform_WhenUserIsNotAuthenticated_ShouldDoNothing(bool hasToken)
        {
            // Arrange
            var request = new HttpRequestMessage();

            DefaultHttpContext httpContext;
            if (hasToken)
            {
                request.Headers.Add(HeaderNames.Authorization, "Bearer " + "TokenValue");

                httpContext = new DefaultHttpContext()
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                };
            }
            else
            {
                httpContext = new DefaultHttpContext();
            }

            var context = new RequestTransformContext
            {
                ProxyRequest = request,
                HttpContext = httpContext
            };
            var transform = new JwtBearerTransform();

            // Act
            var applyTranformMethod = typeof(JwtBearerTransform).GetMethod("ApplyTransform", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var applyTranform = (ValueTask)applyTranformMethod.Invoke(transform, [context])!;
            await applyTranform;

            // Assert
            foreach (var userClaim in ApiGatewayAuthKeys.Claims.AllUserClaims)
            {
                Assert.False(request.Headers.Contains(userClaim.HeaderKey), $"The headers contain {ApiGatewayAuthKeys.Claims.Id.HeaderKey}.");
            }

            Assert.False(request.Headers.Contains(HeaderNames.Authorization), $"The headers contain {HeaderNames.Authorization}.");
        }
    }
}
