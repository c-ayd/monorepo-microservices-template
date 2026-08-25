using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shared.Http.Authentication;
using Shared.Http.Response.Middlewares;
using Shared.Test.Integration.Http.Fixtures;

namespace Shared.Test.Integration.Http.Response.Middlewares
{
    public class AuthErrorResponseMiddlewareTest : IClassFixture<TestHostFixture>
    {
        private const string RoleName = "TestRole";

        private readonly TestHostFixture _hostFixture;

        public AuthErrorResponseMiddlewareTest(TestHostFixture hostFixture)
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

                configureApp.UseMiddleware<AuthErrorResponseMiddleware>();
                configureApp.UseAuthentication();
                configureApp.UseAuthorization();

                configureApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/anonymous", () => Results.Ok());
                    endpoints.MapGet("/authorized", () => Results.Ok())
                        .RequireAuthorization();
                    endpoints.MapGet("/forbidden", () => Results.Ok())
                        .RequireAuthorization(policy => policy.RequireRole(RoleName));
                });
            }).GetAwaiter().GetResult();
        }

        [Fact]
        public async Task Invoke_WhenResponseIs401_ShouldReturn401ErrorItem()
        {
            // Act
            var responseAnonymous = await _hostFixture.Client!.GetAsync("/anonymous");
            var responseAuthorized = await _hostFixture.Client.GetAsync("/authorized");
            var responseForbidden = await _hostFixture.Client.GetAsync("/forbidden");

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
            _hostFixture.Client!.DefaultRequestHeaders.Add(ApiGatewayConstants.UserId.HeaderKey, userId);

            // Act
            var responseAnonymous = await _hostFixture.Client.GetAsync("/anonymous");
            var responseAuthorized = await _hostFixture.Client.GetAsync("/authorized");
            var responseForbidden = await _hostFixture.Client.GetAsync("/forbidden");

            // Assert
            _hostFixture.Client.DefaultRequestHeaders.Remove(ApiGatewayConstants.UserId.HeaderKey);
            _hostFixture.Client.DefaultRequestHeaders.Remove(ApiGatewayConstants.UserRoles.HeaderKey);

            Assert.Equal(HttpStatusCode.OK, responseAnonymous.StatusCode);
            Assert.Equal(HttpStatusCode.OK, responseAuthorized.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, responseForbidden.StatusCode);
        }

        [Fact]
        public async Task Invoke_WhenResponseIsNotRelatedToAuth_ShouldDoNothing()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            _hostFixture.Client!.DefaultRequestHeaders.Add(ApiGatewayConstants.UserId.HeaderKey, userId);
            _hostFixture.Client.DefaultRequestHeaders.Add(ApiGatewayConstants.UserRoles.HeaderKey, RoleName);

            // Act
            var responseAnonymous = await _hostFixture.Client.GetAsync("/anonymous");
            var responseAuthorized = await _hostFixture.Client.GetAsync("/authorized");
            var responseForbidden = await _hostFixture.Client.GetAsync("/forbidden");

            // Assert
            _hostFixture.Client.DefaultRequestHeaders.Remove(ApiGatewayConstants.UserId.HeaderKey);
            _hostFixture.Client.DefaultRequestHeaders.Remove(ApiGatewayConstants.UserRoles.HeaderKey);
            
            Assert.Equal(HttpStatusCode.OK, responseAnonymous.StatusCode);
            Assert.Equal(HttpStatusCode.OK, responseAuthorized.StatusCode);
            Assert.Equal(HttpStatusCode.OK, responseForbidden.StatusCode);
        }
    }
}
