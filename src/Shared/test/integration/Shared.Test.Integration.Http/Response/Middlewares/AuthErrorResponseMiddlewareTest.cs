using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Http.Authentication;
using Shared.Http.Response.Middlewares;
using Shared.Test.Helpers.Fixtures;
using Shared.Test.Integration.Http.Collections;

namespace Shared.Test.Integration.Http.Response.Middlewares
{
    [Collection(nameof(TestHostCollection))]
    public class AuthErrorResponseMiddlewareTest
    {
        private const string _roleName = "TestRole";

        private readonly TestHostFixture _testHostFixture;

        public AuthErrorResponseMiddlewareTest(TestHostCollectionCluster collectionCluster)
        {
            _testHostFixture = collectionCluster.TestHostFixture;
        }

#pragma warning disable xUnit1013 // Public method should be marked as test
        public static void ConfigureApp(IApplicationBuilder app)
        {
            app.UseMiddleware<AuthErrorResponseMiddleware>();
        }

        public static void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/auth-error/anonymous", () => Results.Ok());
            endpoints.MapGet("/auth-error/authorized", () => Results.Ok())
                .RequireAuthorization();
            endpoints.MapGet("/auth-error/forbidden", () => Results.Ok())
                .RequireAuthorization(policy => policy.RequireRole(_roleName));
        }
#pragma warning restore xUnit1013 // Public method should be marked as test

        [Fact]
        public async Task Invoke_WhenResponseIs401_ShouldReturn401ErrorItem()
        {
            // Act
            var responseAnonymous = await _testHostFixture.Client.GetAsync("/auth-error/anonymous");
            var responseAuthorized = await _testHostFixture.Client.GetAsync("/auth-error/authorized");
            var responseForbidden = await _testHostFixture.Client.GetAsync("/auth-error/forbidden");

            // Assert
            Assert.Equal(HttpStatusCode.OK, responseAnonymous.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, responseAuthorized.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, responseForbidden.StatusCode);
        }

        [Fact]
        public async Task Invoke_WhenResponseIs403_ShouldReturn403ErrorItem()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            _testHostFixture.Client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.Id.HeaderKey, userId);

            // Act
            var responseAnonymous = await _testHostFixture.Client.GetAsync("/auth-error/anonymous");
            var responseAuthorized = await _testHostFixture.Client.GetAsync("/auth-error/authorized");
            var responseForbidden = await _testHostFixture.Client.GetAsync("/auth-error/forbidden");

            // Assert
            _testHostFixture.Client.DefaultRequestHeaders.Remove(ApiGatewayAuthKeys.Claims.Id.HeaderKey);
            _testHostFixture.Client.DefaultRequestHeaders.Remove(ApiGatewayAuthKeys.Claims.Roles.HeaderKey);

            Assert.Equal(HttpStatusCode.OK, responseAnonymous.StatusCode);
            Assert.Equal(HttpStatusCode.OK, responseAuthorized.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, responseForbidden.StatusCode);
        }

        [Fact]
        public async Task Invoke_WhenResponseIsNotRelatedToAuth_ShouldDoNothing()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            _testHostFixture.Client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.Id.HeaderKey, userId);
            _testHostFixture.Client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.Roles.HeaderKey, _roleName);

            // Act
            var responseAnonymous = await _testHostFixture.Client.GetAsync("/auth-error/anonymous");
            var responseAuthorized = await _testHostFixture.Client.GetAsync("/auth-error/authorized");
            var responseForbidden = await _testHostFixture.Client.GetAsync("/auth-error/forbidden");

            // Assert
            _testHostFixture.Client.DefaultRequestHeaders.Remove(ApiGatewayAuthKeys.Claims.Id.HeaderKey);
            _testHostFixture.Client.DefaultRequestHeaders.Remove(ApiGatewayAuthKeys.Claims.Roles.HeaderKey);
            
            Assert.Equal(HttpStatusCode.OK, responseAnonymous.StatusCode);
            Assert.Equal(HttpStatusCode.OK, responseAuthorized.StatusCode);
            Assert.Equal(HttpStatusCode.OK, responseForbidden.StatusCode);
        }
    }
}
