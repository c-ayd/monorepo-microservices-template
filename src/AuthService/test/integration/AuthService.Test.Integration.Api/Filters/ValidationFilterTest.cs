using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using AuthService.Api.Filters;
using AuthService.Application.Validations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace AuthService.Test.Integration.Api.Filters
{
    public class ValidationFilterTest : IAsyncLifetime
    {
        private IHost _host = null!;
        private HttpClient _client = null!;

        private const string _endpoint = "/test";
        private const string _endpointAsync = "/test-async";

        private const int ValidationMaxValue = 10;

        public async Task InitializeAsync()
        {
            _host = await Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(builder =>
                {
                    builder.UseTestServer()
                        .Configure(app =>
                        {
                            app.UseRouting();
                            app.UseEndpoints(configure =>
                            {
                                configure.MapPost(_endpoint, async (TestRequest request) => Results.Ok())
                                    .AddValidation(new TestRequestValidation());
                                configure.MapPost(_endpointAsync, async (TestRequest request) => Results.Ok())
                                    .AddAsyncValidation(new TestRequestValidationAsync());
                            });
                        });
                })
                .StartAsync();

            _client = _host.GetTestClient();
        }

        public async Task DisposeAsync()
        {
            await _host.StopAsync();
            _client.Dispose();
        }

        private record TestRequest(
            int IntValue
        );

        private class TestRequestValidation : IValidator<TestRequest>
        {
            public List<ValidationError> Validate(TestRequest value)
            {
                var errors = new List<ValidationError>();

                if (value.IntValue > ValidationMaxValue)
                {
                    errors.Add(new ValidationError());
                    return errors;
                }

                return errors;
            }
        }

        private class TestRequestValidationAsync : IValidatorAsync<TestRequest>
        {
            public async Task<List<ValidationError>> ValidateAsync(TestRequest value, CancellationToken cancellationToken = default)
            {
                var errors = new List<ValidationError>();

                await Task.Yield();
                if (value.IntValue > ValidationMaxValue)
                {
                    errors.Add(new ValidationError());
                    return errors;
                }
                await Task.Yield();

                return errors;
            }
        }

        [Fact]
        public async Task Validate_WhenRequestIsValid_ShouldReturnOk()
        {
            // Arrange
            var request = new TestRequest(ValidationMaxValue - 1);
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, MediaTypeNames.Application.Json);

            // Act
            var response = await _client.PostAsync(_endpoint, content);

            // Arrange
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Validate_WhenRequestIsInvalid_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new TestRequest(ValidationMaxValue + 1);
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, MediaTypeNames.Application.Json);

            // Act
            var response = await _client.PostAsync(_endpoint, content);

            // Arrange
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ValidateAsync_WhenRequestIsValid_ShouldReturnOk()
        {
            // Arrange
            var request = new TestRequest(ValidationMaxValue - 1);
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, MediaTypeNames.Application.Json);

            // Act
            var response = await _client.PostAsync(_endpointAsync, content);

            // Arrange
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ValidateAsync_WhenRequestIsInvalid_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new TestRequest(ValidationMaxValue + 1);
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, MediaTypeNames.Application.Json);

            // Act
            var response = await _client.PostAsync(_endpointAsync, content);

            // Arrange
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
