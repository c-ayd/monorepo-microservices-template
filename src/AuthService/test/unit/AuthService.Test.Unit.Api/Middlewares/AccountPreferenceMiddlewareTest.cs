using AuthService.Api.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace AuthService.Test.Unit.Api.Middlewares
{
    public class AccountPreferenceMiddlewareTest
    {
        private readonly AccountPreferenceMiddleware _middleware;

        public AccountPreferenceMiddlewareTest()
        {
            _middleware = new AccountPreferenceMiddleware(async (context) => {});
        }

        [Fact]
        public async Task Invoke_WhenUserPreferencesExist_ShouldAddPreferencesToItems()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Append(HeaderNames.AcceptLanguage, "de-DE;q=0.9,en-US;q=0.8,en;q=0.7");

            // Act
            await _middleware.Invoke(httpContext);

            // Assert
            Assert.NotNull(httpContext.Items["PreferredLanguage"]);
            Assert.Equal("de", (string)httpContext.Items["PreferredLanguage"]!);
        }

        [Fact]
        public async Task Invoke_WhenUserPreferencesDoNotExist_ShouldAddNothingToItems()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            // Act
            await _middleware.Invoke(httpContext);

            // Assert
            Assert.NotNull(httpContext.Items["PreferredLanguage"]);
            Assert.Equal("en", (string)httpContext.Items["PreferredLanguage"]!);
        }
    }
}
