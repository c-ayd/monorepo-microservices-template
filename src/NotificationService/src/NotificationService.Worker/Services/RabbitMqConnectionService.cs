using Microsoft.Extensions.Options;
using NotificationService.Worker.Options;
using RabbitMQ.Client;

namespace NotificationService.Worker.Services
{
    public class RabbitMqConnectionService : IDisposable, IAsyncDisposable
    {
        private readonly TimeSpan NetworkRecoveryInterval = TimeSpan.FromSeconds(30);

        private readonly RabbitMqOptions _rabbitMqOptions;

        private readonly SemaphoreSlim _connectionSemaphore = new SemaphoreSlim(1, 1);
        public IConnection? Connection { get; private set; }

        private readonly SemaphoreSlim _disposeSemaphore = new SemaphoreSlim(1, 1);

        public RabbitMqConnectionService(IOptions<RabbitMqOptions> rabbitMqOptions)
        {
            _rabbitMqOptions = rabbitMqOptions.Value;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            await _connectionSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (Connection != null)
                    return;
                
                var factory = new ConnectionFactory()
                {
                    ClientProvidedName = "Notification Service",
                    UserName = _rabbitMqOptions.Username,
                    Password = _rabbitMqOptions.Password,
                    HostName = _rabbitMqOptions.Host,
                    Port = _rabbitMqOptions.Port,
                    VirtualHost = "/",
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = NetworkRecoveryInterval
                };

                Connection = await factory.CreateConnectionAsync(cancellationToken);
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            await _disposeSemaphore.WaitAsync();
            try
            {
                if (Connection != null)
                {
                    await Connection.CloseAsync();
                    await Connection.DisposeAsync();
                    Connection = null;
                }
            }
            finally
            {
                _disposeSemaphore.Release();

                _connectionSemaphore.Dispose();
                _disposeSemaphore.Dispose();
            }
        }
    }
}
