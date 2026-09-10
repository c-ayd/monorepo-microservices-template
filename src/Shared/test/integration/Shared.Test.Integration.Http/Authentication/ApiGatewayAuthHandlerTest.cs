using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Http.Authentication;
using Shared.Http.Authentication.Structures;
using Shared.Test.Generators;
using Shared.Test.Helpers.Fixtures;
using Shared.Test.Integration.Http.Collections;

namespace Shared.Test.Integration.Http.Authentication
{
    [Collection(nameof(TestHostCollection))]
    public class ApiGatewayAuthHandlerTest
    {
        private const string _roleName = "TestRole";
        
        private readonly TestHostFixture _testHostFixture;

        public ApiGatewayAuthHandlerTest(TestHostCollectionCluster collectionCluster)
        {
            _testHostFixture = collectionCluster.TestHostFixture;
        }

#pragma warning disable xUnit1013 // Public method should be marked as test
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(ApiGatewayAuthKeys.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, ApiGatewayAuthHandler>(ApiGatewayAuthKeys.AuthenticationScheme, options => { });
            services.AddAuthorization();
        }

        public static void ConfigureApp(IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        public static void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/api-gateway", (HttpContext context) => Results.Ok(new UserDto
            {
                IsAuthenticated = context.User.Identity?.IsAuthenticated,
                Name = context.User.Identity?.Name,
                Claims = context.User.Claims.Select(c => new UserDto.ClaimDto() { Type = c.Type, Value = c.Value })
            }));
            endpoints.MapGet("/api-gateway/authorized", () => Results.Ok())
                .RequireAuthorization();
            endpoints.MapGet("/api-gateway/access-granted", () => Results.Ok())
                .RequireAuthorization(policy => policy.RequireRole(_roleName));
            endpoints.MapGet("/api-gateway/forbidden", () => Results.Ok())
                .RequireAuthorization(policy => policy.RequireRole(_roleName + "a"));
        }
#pragma warning restore xUnit1013 // Public method should be marked as test

        [Fact]
        public async Task Invoke_WhenHeadersHaveUserContent_ShouldFillClaimPrincipalAndAuthorize()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            _testHostFixture.Client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.Id.HeaderKey, userId);
            _testHostFixture.Client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.Roles.HeaderKey, _roleName);

            UserClaim? claim = null;
            string? headerValue = null;
            foreach (var userClaim in ApiGatewayAuthKeys.Claims.AllUserClaims)
            {
                if (userClaim.HeaderKey == ApiGatewayAuthKeys.Claims.Id.HeaderKey ||
                    userClaim.HeaderKey == ApiGatewayAuthKeys.Claims.Roles.HeaderKey)
                    continue;

                claim = userClaim;
                headerValue = StringGenerator.GenerateAlpha();

                _testHostFixture.Client.DefaultRequestHeaders.Add(userClaim.HeaderKey, headerValue);
                break;
            }

            // Act
            var responseUser = await _testHostFixture.Client.GetFromJsonAsync<UserDto>("/api-gateway");
            var responseAuthorized = await _testHostFixture.Client.GetAsync("/api-gateway/authorized");
            var responseAccessGranted = await _testHostFixture.Client.GetAsync("/api-gateway/access-granted");
            var responseForbidden = await _testHostFixture.Client.GetAsync("/api-gateway/forbidden");

            // Assert
            _testHostFixture.Client.DefaultRequestHeaders.Remove(ApiGatewayAuthKeys.Claims.Id.HeaderKey);
            _testHostFixture.Client.DefaultRequestHeaders.Remove(ApiGatewayAuthKeys.Claims.Roles.HeaderKey);
            if (claim != null)
            {
                _testHostFixture.Client.DefaultRequestHeaders.Remove(claim.HeaderKey);
            }

            Assert.NotNull(responseUser);
            Assert.True(responseUser.IsAuthenticated, "The user is not authenticated.");
            Assert.Equal(userId, responseUser.Name);
            Assert.Equal(_roleName, responseUser.Claims.FirstOrDefault(c => c.Type == ApiGatewayAuthKeys.Claims.Roles.ClaimType)!.Value);

            if (claim != null)
            {
                Assert.Equal(headerValue, responseUser.Claims.FirstOrDefault(c => c.Type == claim.ClaimType)!.Value);
            }

            Assert.Equal(HttpStatusCode.OK, responseAuthorized.StatusCode);
            Assert.Equal(HttpStatusCode.OK, responseAccessGranted.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, responseForbidden.StatusCode);
        }

        [Fact]
        public async Task Invoke_WhenHeadersHaveNoUserContent_ShouldLeftClaimPrincipalEmptyAndNotAuthorize()
        {
            // Act
            var responseUser = await _testHostFixture.Client.GetFromJsonAsync<UserDto>("/api-gateway");
            var responseAuthorized = await _testHostFixture.Client.GetAsync("/api-gateway/authorized");
            var responseAccessGranted = await _testHostFixture.Client.GetAsync("/api-gateway/access-granted");
            var responseForbidden = await _testHostFixture.Client.GetAsync("/api-gateway/forbidden");

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
