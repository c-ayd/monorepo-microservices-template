using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Shared.Http.DependencyInjection;
using Shared.Http.Response.Structures;
using Shared.Http.Validation;
using Shared.Test.Integration.Http.Fixtures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Shared.Test.Integration.Http.DependencyInjection
{
    public class ValidationFiltersTest : IClassFixture<TestHostFixture>
    {
        public const string ErrorCode = "TestValue";

        private readonly TestHostFixture _hostFixture;

        public ValidationFiltersTest(TestHostFixture hostFixture)
        {
            _hostFixture = hostFixture;
            _hostFixture.BuildAsync(services =>
            {
                services.RegisterValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            }, app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapPost("/validation", (HttpContext context, ValidationType1 value) => Results.NoContent().ExecuteAsync(context))
                        .AddValidation<ValidationType1>();
                    endpoints.MapPost("/async-validation", (HttpContext context, ValidationType2 value) => Results.NoContent().ExecuteAsync(context))
                        .AddAsyncValidation<ValidationType2>();
                });
            }).GetAwaiter().GetResult();
        }

        [Fact]
        public async Task AddValidation_WhenValidatorIsAddedAndValueIsCorrect_ShouldReturnNoContent()
        {
            // Arrange
            var client = _hostFixture.Client!;
            var request = new ValidationType1(10);

            // Act
            var response = await client.PostAsJsonAsync("/validation", request);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task AddValidation_WhenValidatorIsAddedAndValueIsNotCorrect_ShouldReturnBadRequest()
        {
            // Arrange
            var client = _hostFixture.Client!;
            var request = new ValidationType1(-1);

            // Act
            var response = await client.PostAsJsonAsync("/validation", request);
            var jsonResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(jsonResponse);
            Assert.Single(jsonResponse.Errors);
            Assert.Equal(ErrorCode, jsonResponse.Errors[0].Code);
        }

        [Fact]
        public async Task AddAsyncValidation_WhenValidatorIsAddedAndValueIsCorrect_ShouldReturnNoContent()
        {
            // Arrange
            var client = _hostFixture.Client!;
            var request = new ValidationType1(10);

            // Act
            var response = await client.PostAsJsonAsync("/async-validation", request);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task AddAsyncValidation_WhenValidatorIsAddedAndValueIsNotCorrect_ShouldReturnBadRequest()
        {
            // Arrange
            var client = _hostFixture.Client!;
            var request = new ValidationType1(-1);

            // Act
            var response = await client.PostAsJsonAsync("/async-validation", request);
            var jsonResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(jsonResponse);
            Assert.Single(jsonResponse.Errors);
            Assert.Equal(ErrorCode, jsonResponse.Errors[0].Code);
        }

        public record ValidationType1(int IntValue);
        public record ValidationType2(int IntValue);

        private class ErrorResponse
        {
            public List<ErrorItem> Errors { get; set; } = new List<ErrorItem>();
        }
    }

    public class ValidationFiltersTest_TestValidator : IValidator<ValidationFiltersTest.ValidationType1>
    {
        public List<ErrorItem> Validate(ValidationFiltersTest.ValidationType1 value)
        {
            var errors = new List<ErrorItem>();

            if (value.IntValue < 0)
            {
                errors.Add(new ErrorItem(ValidationFiltersTest.ErrorCode));
            }

            return errors;
        }
    }

    public class ValidationFiltersTest_TestAsyncValidator : IAsyncValidator<ValidationFiltersTest.ValidationType2>
    {
        public async Task<List<ErrorItem>> ValidateAsync(ValidationFiltersTest.ValidationType2 value, CancellationToken cancellationToken = default)
        {
            var errors = new List<ErrorItem>();

            if (value.IntValue < 0)
            {
                errors.Add(new ErrorItem(ValidationFiltersTest.ErrorCode));
            }

            return errors;
        }
    }
}
