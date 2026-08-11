using System.Reflection;
using Common.Http.DependencyInjection;
using Common.Http.Response.Structures;
using Common.Http.Validation;
using Common.Test.Integration.Http.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Test.Integration.Http.DependencyInjection
{
    public class AddValidatorTest : IClassFixture<TestHostFixture>
    {
        public const string ScopedServiceValue = "TestValue";

        private readonly TestHostFixture _hostFixture;

        public AddValidatorTest(TestHostFixture hostFixture)
        {
            _hostFixture = hostFixture;
        }

        [Fact]
        public async Task AddValidationsFromAssembly_WhenThereIsValidations_ShouldAddThemToDIContainer()
        {
            // Act
            await _hostFixture.BuildAsync(services =>
            {
                services.AddScoped<TestScopedService>();

                services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            }, null);

            // Assert
            var validator = _hostFixture.TestHost!.Services.GetRequiredService<IValidator<ValidationType1>>();
            Assert.NotNull(validator);

            var asyncValidator = _hostFixture.TestHost!.Services.GetRequiredService<IAsyncValidator<ValidationType2>>();
            Assert.NotNull(asyncValidator);

            var errors = validator.Validate(new ValidationType1());
            Assert.Single(errors);
            Assert.Equal(ScopedServiceValue, errors[0].Code);

            errors = await asyncValidator.ValidateAsync(new ValidationType2());
            Assert.Single(errors);
            Assert.Equal(ScopedServiceValue, errors[0].Code);
        }

        public class TestScopedService
        {
            public string StrValue { get; set; } = ScopedServiceValue;
        }

        public record ValidationType1();
        public record ValidationType2();
    }

    public class AddValidatorTest_TestValidator : IValidator<AddValidatorTest.ValidationType1>
    {
        public AddValidatorTest.TestScopedService ScopedService { get; private set; }

        public AddValidatorTest_TestValidator(AddValidatorTest.TestScopedService scopedService)
        {
            ScopedService = scopedService;
        }

        public List<ErrorItem> Validate(AddValidatorTest.ValidationType1 value)
        {
            return new List<ErrorItem>()
            {
                new ErrorItem(AddValidatorTest.ScopedServiceValue)
            };
        }
    }

    public class AddValidatorTest_TestAsyncValidator : IAsyncValidator<AddValidatorTest.ValidationType2>
    {
        public AddValidatorTest.TestScopedService ScopedService { get; private set; }

        public AddValidatorTest_TestAsyncValidator(AddValidatorTest.TestScopedService scopedService)
        {
            ScopedService = scopedService;
        }

        public async Task<List<ErrorItem>> ValidateAsync(AddValidatorTest.ValidationType2 value, CancellationToken cancellationToken = default)
        {
            return new List<ErrorItem>()
            {
                new ErrorItem(AddValidatorTest.ScopedServiceValue)
            };
        }
    }
}
