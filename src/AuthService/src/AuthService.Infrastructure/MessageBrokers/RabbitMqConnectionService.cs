using AuthService.Infrastructure.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AuthService.Infrastructure.MessageBrokers
{
    public class RabbitMqConnectionService : IDisposable, IAsyncDisposable
    {
        private const int NetworkRecoveryIntervalInSeconds = 30;

        private readonly RabbitMqOptions _rabbitMqOptions;

        public IConnection? Connection { get; private set; }

        public RabbitMqConnectionService(IOptions<RabbitMqOptions> rabbitMqOptions)
        {
            _rabbitMqOptions = rabbitMqOptions.Value;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            // This method should not be called twice. This is only for a safeguard.
            if (Connection != null)
                return;

            var factory = new ConnectionFactory()
            {
                ClientProvidedName = "Auth Service",
                UserName = _rabbitMqOptions.Username,
                Password = _rabbitMqOptions.Password,
                HostName = _rabbitMqOptions.Host,
                Port = _rabbitMqOptions.Port,
                VirtualHost = "/",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(NetworkRecoveryIntervalInSeconds)
            };

            Connection = await factory.CreateConnectionAsync(cancellationToken);
        }

        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            if (Connection != null)
            {
                await Connection.CloseAsync();
                await Connection.DisposeAsync();
            }
        }
    }
}
