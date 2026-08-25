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
                new Claim(ApiGatewayConstants.UserId.ClaimType, userId),
                new Claim(ApiGatewayConstants.UserRoles.ClaimType, role1),
                new Claim(ApiGatewayConstants.UserRoles.ClaimType, role2)
            };

            UserClaim? userClaim = null;
            string? claimValue = null;
            if (ApiGatewayConstants.UserClaims.Count > 0)
            {
                userClaim = ApiGatewayConstants.UserClaims[0];
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
            Assert.True(request.Headers.Contains(ApiGatewayConstants.UserId.HeaderKey), $"The headers do not contain {ApiGatewayConstants.UserId.HeaderKey}.");
            Assert.Single(request.Headers.GetValues(ApiGatewayConstants.UserId.HeaderKey));
            Assert.Equal(userId, request.Headers.GetValues(ApiGatewayConstants.UserId.HeaderKey).ElementAt(0));

            Assert.True(request.Headers.Contains(ApiGatewayConstants.UserRoles.HeaderKey), $"The headers do not contain {ApiGatewayConstants.UserRoles.HeaderKey}.");
            Assert.Single(request.Headers.GetValues(ApiGatewayConstants.UserRoles.HeaderKey));
            var roles = request.Headers.GetValues(ApiGatewayConstants.UserRoles.HeaderKey).FirstOrDefault()!.Split(',');
            Assert.Contains(role1, roles);
            Assert.Contains(role2, roles);

            if (userClaim != null)
            {
                Assert.True(request.Headers.Contains(userClaim.HeaderKey), $"The headers do not contain {userClaim.HeaderKey}.");
                Assert.Single(request.Headers.GetValues(userClaim.HeaderKey));
                Assert.Equal(claimValue, request.Headers.GetValues(userClaim.HeaderKey).ElementAt(0));
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
            Assert.False(request.Headers.Contains(ApiGatewayConstants.UserId.HeaderKey), $"The headers contain {ApiGatewayConstants.UserId.HeaderKey}.");
            Assert.False(request.Headers.Contains(ApiGatewayConstants.UserRoles.HeaderKey), $"The headers contain {ApiGatewayConstants.UserRoles.HeaderKey}.");

            foreach (var userClaim in ApiGatewayConstants.UserClaims)
            {
                Assert.False(request.Headers.Contains(userClaim.HeaderKey), $"The headers contain {ApiGatewayConstants.UserId.HeaderKey}.");
            }

            Assert.False(request.Headers.Contains(HeaderNames.Authorization), $"The headers contain {HeaderNames.Authorization}.");
        }
    }
}
