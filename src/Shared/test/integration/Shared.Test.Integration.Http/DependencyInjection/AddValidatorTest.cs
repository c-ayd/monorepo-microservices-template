using System.Reflection;
using Shared.Http.DependencyInjection;
using Shared.Http.Response.Structures;
using Shared.Http.Validation;
using Microsoft.Extensions.DependencyInjection;
using Shared.Test.Helpers.Fixtures;
using Shared.Test.Integration.Http.Collections;

namespace Shared.Test.Integration.Http.DependencyInjection
{
    [Collection(nameof(TestHostCollection))]
    public class AddValidatorTest
    {
        public const string ScopedServiceValue = "TestValue";

        private readonly TestHostFixture _testHostFixture;

        public AddValidatorTest(TestHostCollectionCluster collectionCluster)
        {
            _testHostFixture = collectionCluster.TestHostFixture;
        }

#pragma warning disable xUnit1013 // Public method should be marked as test
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<TestScopedService>();

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        }
#pragma warning restore xUnit1013 // Public method should be marked as test

        [Fact]
        public async Task AddValidationsFromAssembly_WhenThereIsValidations_ShouldAddThemToDIContainer()
        {
            // Assert
            var validator = _testHostFixture.Host.Services.GetRequiredService<IValidator<ValidationType1>>();
            Assert.NotNull(validator);

            var asyncValidator = _testHostFixture.Host.Services.GetRequiredService<IAsyncValidator<ValidationType2>>();
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
