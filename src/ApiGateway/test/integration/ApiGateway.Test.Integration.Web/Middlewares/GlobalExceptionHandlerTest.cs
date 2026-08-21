using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using ApiGateway.Test.Integration.Web.Collections;
using ApiGateway.Test.Integration.Web.Fixtures;
using ApiGateway.Web.Middlewares;
using Shared.Http.Response.Structures;

namespace ApiGateway.Test.Integration.Web.Middlewares
{
    [Collection(nameof(ApiGatewayWebCollection))]
    public class GlobalExceptionHandlerTest
    {
        private readonly HttpClient _client;

        public GlobalExceptionHandlerTest(ApiGatewayWebFixture apiGatewayWeb)
        {
            _client = apiGatewayWeb.Client;
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
