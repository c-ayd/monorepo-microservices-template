using AuthService.Infrastructure.MessageBrokers;
using AuthService.Test.Integration.Api.Collections;
using AuthService.Test.Utility.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Test.Integration.Api.BackgroundServices
{
    [Collection(nameof(AuthApiCollection))]
    public class RabbitMqInitializerBackgroundServiceTest
    {
        private const int TimeoutInSeconds = 30;

        private readonly AuthApiFixture _authApiFixture;

        public RabbitMqInitializerBackgroundServiceTest(AuthApiFixture authApiFixture)
        {
            _authApiFixture = authApiFixture;
        }

        [Fact]
        public async Task StartAsync_WhenApplicationStarts_ShouldEstablishConnection()
        {
            // Act
            // The background service automatically calls StartAsync method

            // Assert
            await using var scope = _authApiFixture.Factory.Services.CreateAsyncScope();
            var rabbitMqConnService = scope.ServiceProvider.GetRequiredService<RabbitMqConnectionService>();

            var elapsedTimeInSeconds = 0;
            while (true)
            {
                if (rabbitMqConnService.Connection != null)
                    break;

                await Task.Delay(1000);

                ++elapsedTimeInSeconds;
                if (elapsedTimeInSeconds >= TimeoutInSeconds)
                    Assert.Fail($"Timeout. The test case waited for {TimeoutInSeconds} seconds but the connection was not established.");
            }
        }
    }
}
