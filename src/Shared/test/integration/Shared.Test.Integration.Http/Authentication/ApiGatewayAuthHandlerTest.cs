using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shared.Http.Authentication;
using Shared.Http.Authentication.Structures;
using Shared.Test.Integration.Http.Fixtures;
using Shared.TestGenerators;

namespace Shared.Test.Integration.Http.Authentication
{
    public class ApiGatewayAuthHandlerTest : IClassFixture<TestHostFixture>
    {
        private const string RoleName = "TestRole";
        
        private readonly TestHostFixture _hostFixture;

        public ApiGatewayAuthHandlerTest(TestHostFixture hostFixture)
        {
            _hostFixture = hostFixture;

            _hostFixture.BuildAsync(configureServices =>
            {
                configureServices.AddAuthentication(ApiGatewayConstants.AuthenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, ApiGatewayAuthHandler>(ApiGatewayConstants.AuthenticationScheme, options => { });
                configureServices.AddAuthorization();
            },
            configureApp =>
            {
                configureApp.UseRouting();

                configureApp.UseAuthentication();
                configureApp.UseAuthorization();

                configureApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/", (HttpContext context) => Results.Ok(new UserDto
                    {
                        IsAuthenticated = context.User.Identity?.IsAuthenticated,
                        Name = context.User.Identity?.Name,
                        Claims = context.User.Claims.Select(c => new UserDto.ClaimDto() { Type = c.Type, Value = c.Value })
                    }));
                    endpoints.MapGet("/authorized", () => Results.Ok())
                        .RequireAuthorization();
                    endpoints.MapGet("/access-granted", () => Results.Ok())
                        .RequireAuthorization(policy => policy.RequireRole(RoleName));
                    endpoints.MapGet("/forbidden", () => Results.Ok())
                        .RequireAuthorization(policy => policy.RequireRole(RoleName + "a"));
                });
            }).GetAwaiter().GetResult();
        }

        [Fact]
        public async Task Invoke_WhenHeaderHasUserContent_ShouldFillClaimPrincipalAndAuthorize()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            _hostFixture.Client!.DefaultRequestHeaders.Add(ApiGatewayConstants.UserId.HeaderKey, userId);
            _hostFixture.Client.DefaultRequestHeaders.Add(ApiGatewayConstants.UserRoles.HeaderKey, RoleName);

            UserClaim? userClaim = null;
            string? headerValue = null;
            if (ApiGatewayConstants.UserClaims.Count > 0)
            {
                userClaim = ApiGatewayConstants.UserClaims[0];
                headerValue = StringGenerator.GenerateAlpha();

                _hostFixture.Client.DefaultRequestHeaders.Add(userClaim.HeaderKey, headerValue);
            }

            // Act
            var responseUser = await _hostFixture.Client.GetFromJsonAsync<UserDto>("/");
            var responseAuthorized = await _hostFixture.Client.GetAsync("/authorized");
            var responseAccessGranted = await _hostFixture.Client.GetAsync("/access-granted");
            var responseForbidden = await _hostFixture.Client.GetAsync("/forbidden");

            // Assert
            _hostFixture.Client.DefaultRequestHeaders.Remove(ApiGatewayConstants.UserId.HeaderKey);
            _hostFixture.Client.DefaultRequestHeaders.Remove(ApiGatewayConstants.UserRoles.HeaderKey);
            if (userClaim != null)
            {
                _hostFixture.Client.DefaultRequestHeaders.Remove(userClaim.HeaderKey);
            }

            Assert.NotNull(responseUser);
            Assert.True(responseUser.IsAuthenticated, "The user is not authenticated.");
            Assert.Equal(userId, responseUser.Name);
            Assert.Equal(RoleName, responseUser.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)!.Value);

            if (userClaim != null)
            {
                Assert.Equal(headerValue, responseUser.Claims.FirstOrDefault(c => c.Type == userClaim.ClaimType)!.Value);
            }

            Assert.Equal(HttpStatusCode.OK, responseAuthorized.StatusCode);
            Assert.Equal(HttpStatusCode.OK, responseAccessGranted.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, responseForbidden.StatusCode);
        }

        [Fact]
        public async Task Invoke_WhenHeaderHasUserContent_ShouldLeftClaimPrincipalEmptyAndNotAuthorize()
        {
            // Act
            var responseUser = await _hostFixture.Client!.GetFromJsonAsync<UserDto>("/");
            var responseAuthorized = await _hostFixture.Client!.GetAsync("/authorized");
            var responseAccessGranted = await _hostFixture.Client.GetAsync("/access-granted");
            var responseForbidden = await _hostFixture.Client.GetAsync("/forbidden");

            // Assert
            Assert.NotNull(responseUser);
            Assert.False(responseUser.IsAuthenticated, "The user is authenticated.");
            Assert.Null(responseUser.Name);
            Assert.Empty(responseUser.Claims);

            Assert.Equal(HttpStatusCode.Unauthorized, responseAuthorized.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, responseAccessGranted.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, responseForbidden.StatusCode);
        }

        private class UserDto
        {
            public bool? IsAuthenticated { get; set; }
            public string? Name { get; set; }
            public required IEnumerable<ClaimDto> Claims { get; set; }

            public class ClaimDto
            {
                public required string Type { get; set; }
                public required string Value { get; set; }
            }
        }
    }
}
