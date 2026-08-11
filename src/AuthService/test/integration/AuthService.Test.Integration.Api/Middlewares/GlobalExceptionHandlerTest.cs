using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using AuthService.Api.Middlewares;
using AuthService.Test.Integration.Api.Collections;
using AuthService.Test.Utility.Fixtures;
using Common.Http.Response.Structures;

namespace AuthService.Test.Integration.Api.Middlewares
{
    [Collection(nameof(AuthApiCollection))]
    public class GlobalExceptionHandlerTest
    {
        private readonly HttpClient _client;

        public GlobalExceptionHandlerTest(AuthApiFixture authApiFixture)
        {
            _client = authApiFixture.Client;
        }

        [Fact]
        public async Task Invoke_WhenExceptionIsThrown_ShouldReturn500AndCorrectFormat()
        {
            // Act
            var response = await _client.GetAsync("/test/exception");

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            var jsonResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            Assert.NotNull(jsonResponse);
            Assert.Single(jsonResponse.Errors);

            var internalServerError = typeof(GlobalExceptionHandler).GetField("_internalServerError", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.Equal(internalServerError!.GetValue(null), jsonResponse.Errors[0]);
        }

        [Fact]
        public async Task Invoke_WhenExceptionIsNotThrown_ShouldContinueExecution()
        {
            // Act
            var response = await _client.GetAsync("/test/no-exception");

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
        private class ErrorResponse
        {
            public List<ErrorItem> Errors { get; set; } = new List<ErrorItem>();
        }
    }
}
