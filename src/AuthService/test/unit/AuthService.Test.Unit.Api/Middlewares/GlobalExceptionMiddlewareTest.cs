using AuthService.Api.Middlewares;
using AuthService.Test.Utility.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Test.Unit.Api.Middlewares
{
    public class GlobalExceptionMiddlewareTest : IClassFixture<LoggerFixture<GlobalExceptionMiddleware>>
    {
        private readonly LoggerFixture<GlobalExceptionMiddleware> _loggerFixture;

        public GlobalExceptionMiddlewareTest(LoggerFixture<GlobalExceptionMiddleware> loggerFixture)
        {
            _loggerFixture = loggerFixture;
        }

        [Fact]
        public async Task Invoke_WhenExceptionIsThrown_ShouldReturnInternalServerError()
        {
            // Arrange
            var middleware = new GlobalExceptionMiddleware(
                async (context) => throw new Exception("Test exception"),
                _loggerFixture);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOptions<JsonOptions>();
            var httpContext = new DefaultHttpContext()
            {
                RequestServices = services.BuildServiceProvider()
            };

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task Invoke_WhenExceptionIsNotThrown_ShouldContinueExecution()
        {
            // Arrange
            var middleware = new GlobalExceptionMiddleware(
                async (context) => context.Response.StatusCode = StatusCodes.Status204NoContent,
                _loggerFixture);

            var httpContext = new DefaultHttpContext();

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            Assert.Equal(StatusCodes.Status204NoContent, httpContext.Response.StatusCode);
        }
    }
}
