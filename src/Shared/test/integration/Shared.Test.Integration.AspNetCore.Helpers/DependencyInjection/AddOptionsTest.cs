using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shared.AspNetCore.Helpers.DependencyInjection;
using Shared.AspNetCore.Helpers.Options;

namespace Shared.Test.Integration.AspNetCore.Helpers.Options
{
    public class AddOptionsTest
    {
        [Fact]
        public async Task AddOptions_WhenOptionsClassIsSet_ShouldRegisterOptionsClass()
        {
            // Arrange
            var builder = Host.CreateApplicationBuilder();

            var strValue = "TestValue";
            var intValue = 10;
            builder.Configuration.AddInMemoryCollection([
                new KeyValuePair<string, string?>("TestKey:StrValue", strValue),
                new KeyValuePair<string, string?>("TestKey:IntValue", intValue.ToString())
            ]);

            // Act
            builder.AddOptionsFromAssembly(Assembly.GetExecutingAssembly());

            var host = builder.Build();
            await host.StartAsync();

            // Assert
            var options = host.Services.GetService<IOptions<TestOptions>>();
            Assert.NotNull(options);
            Assert.Equal(strValue, options.Value.StrValue);
            Assert.Null(options.Value.NullableStrValue);
            Assert.Equal(intValue, options.Value.IntValue);

            await host.StopAsync();
            host.Dispose();
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
