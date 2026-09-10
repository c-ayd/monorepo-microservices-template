using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Helpers.DependencyInjection;
using Shared.Helpers.Options;
using Shared.Test.Helpers.Fixtures;
using Shared.Test.Integration.Helpers.Collections;

namespace Shared.Test.Integration.Helpers.Options
{
    [Collection(nameof(TestHostCollection))]
    public class AddOptionsTest
    {
        private const string _strValue = "TestValue";
        private const int _intValue = 10;

        private readonly TestHostFixture _testHostFixture;

        public AddOptionsTest(TestHostCollectionCluster collectionCluster)
        {
            _testHostFixture = collectionCluster.TestHostFixture;
        }

#pragma warning disable xUnit1013 // Public method should be marked as test
        public static void AddConfiguration(IConfigurationBuilder builder)
        {
            builder.AddInMemoryCollection([
                new KeyValuePair<string, string?>("TestKey:StrValue", _strValue),
                new KeyValuePair<string, string?>("TestKey:IntValue", _intValue.ToString())
            ]);
        }

        public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var getOptionsMethodInfo = typeof(DependencyInjection).GetMethod("GetOptionsFromAssembly", BindingFlags.NonPublic | BindingFlags.Static)!;
            var options = (List<Type>)getOptionsMethodInfo.Invoke(null, [Assembly.GetExecutingAssembly()])!;

            var registerMethodInfo = typeof(DependencyInjection).GetMethod("RegisterOptions", BindingFlags.NonPublic | BindingFlags.Static)!;
            registerMethodInfo.Invoke(null, [services, configuration, options]);
        }
#pragma warning restore xUnit1013 // Public method should be marked as test

        [Fact]
        public async Task AddOptions_WhenOptionsClassIsSet_ShouldRegisterOptionsClass()
        {
            // Assert
            var options = _testHostFixture.Host.Services.GetService<IOptions<TestOptions>>();
            Assert.NotNull(options);
            Assert.Equal(_strValue, options.Value.StrValue);
            Assert.Null(options.Value.NullableStrValue);
            Assert.Equal(_intValue, options.Value.IntValue);
        }

        private class TestOptions : IOptions
        {
            public static string Key => "TestKey";

            public required string StrValue { get; set; }
            public string? NullableStrValue { get; set; }
            public required int IntValue { get; set; }
        }
    }
}
