using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shared.RabbitMq.Helpers.BackgroundServices
{
    public abstract class ConsumerBackgroundService : BackgroundService
    {
        private readonly TimeSpan _graceTime = TimeSpan.FromSeconds(5);

        private readonly ConnectionFactory _connectionFactory;
        private readonly TimeSpan _connectionCheckTime;
        private readonly string _queueName;
        private readonly ushort _prefetchCount;
        private readonly ILogger _logger;

        private IConnection? _connection;
        public IChannel? Channel { get; private set; }
        private string? _consumerTag;

        public ConsumerBackgroundService(
            ConnectionFactory connectionFactory,
            TimeSpan connectionCheckTime,
            string queueName,
            ushort prefetchCount,
            ILogger logger)
        {
            _connectionFactory = connectionFactory;
            _connectionFactory.AutomaticRecoveryEnabled = false;
            _connectionFactory.TopologyRecoveryEnabled = false;

            _connectionCheckTime = connectionCheckTime;
            _queueName = queueName;
            _prefetchCount = prefetchCount;
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await InitializeConnectionAsync(cancellationToken);

                await base.StartAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Something went wrong. Message: {Message}",
                    exception.Message);

                throw;
            }
        }

        private async Task InitializeConnectionAsync(CancellationToken cancellationToken)
        {
            if (_connection != null)
            {
                if (_connection.IsOpen)
                {
                    await _connection.CloseAsync();
                }

                await _connection.DisposeAsync();
            }

            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            
            await InitializeChannelAsync(cancellationToken);
        }

        private async Task InitializeChannelAsync(CancellationToken cancellationToken)
        {
            if (Channel != null)
            {
                if (Channel.IsOpen)
                {
                    await Channel.CloseAsync();
                }

                await Channel.DisposeAsync();
            }

            Channel = await _connection!.CreateChannelAsync(cancellationToken: cancellationToken);

            await DeclareExchangesAsync(cancellationToken);
            await DeclareQueuesAsync(cancellationToken);

            await Channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: _prefetchCount,
                global: false,
                cancellationToken: cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(Channel);
            consumer.ReceivedAsync += ReceivedAsync;

            _consumerTag = await Channel.BasicConsumeAsync(
                queue: _queueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Declares exchanges.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the declarations</param>
        protected abstract Task DeclareExchangesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Declares queues.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the declarations</param>
        protected abstract Task DeclareQueuesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Is called when a message is received.
        /// </summary>
        /// <param name="obj">Sender</param>
        /// <param name="args">Delivery arguments</param>
        protected abstract Task ReceivedAsync(object obj, BasicDeliverEventArgs args);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_connectionCheckTime, stoppingToken);

                    if (_connection == null || !_connection.IsOpen)
                    {
                        await InitializeConnectionAsync(stoppingToken);
                    }
                    else if (Channel == null || !Channel.IsOpen)
                    {
                        await InitializeChannelAsync(stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("The consumer is cancelled.");
                }
                catch (Exception exception)
                {
                    _logger.LogCritical(exception, "Someting went wrong while checking connection. The process will rerun in {RetryPubishTime} seconds. Message: {Message}",
                        _connectionCheckTime.TotalSeconds,
                        exception.Message);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // Wait for a small amount of time in case there are still messages that are being processed
            if (Channel != null && Channel.IsOpen && _consumerTag != null)
            {
                await Channel.BasicCancelAsync(_consumerTag);
            }
            await Task.Delay(_graceTime);

            if (Channel != null)
            {
                await Channel.CloseAsync();
                await Channel.DisposeAsync();
            }

            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
