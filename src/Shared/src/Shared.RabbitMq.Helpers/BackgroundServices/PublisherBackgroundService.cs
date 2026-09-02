using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Shared.RabbitMq.Helpers.Exceptions;
using Shared.RabbitMq.Helpers.Structs;

namespace Shared.RabbitMq.Helpers.BackgroundServices
{
    public abstract class PublisherBackgroundService : BackgroundService
    {
        private readonly TimeSpan GraceTime = TimeSpan.FromSeconds(5);

        private readonly ConnectionFactory _connectionFactory;
        private readonly List<Publisher> _publishers;
        private readonly TimeSpan _retryPublishTime;
        private readonly ILogger _logger;

        private IConnection? _connection;
        private Dictionary<int, Message> _rejectedMessages;

        public PublisherBackgroundService(
            ConnectionFactory connectionFactory,
            List<Publisher> publishers,
            TimeSpan retryPublishTime,
            ILogger logger)
        {
            _connectionFactory = connectionFactory;
            _connectionFactory.AutomaticRecoveryEnabled = false;
            _connectionFactory.TopologyRecoveryEnabled = false;

            _publishers = publishers;
            _retryPublishTime = retryPublishTime;
            _logger = logger;

            _rejectedMessages = new Dictionary<int, Message>();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await InitializeAsync(cancellationToken);

                await base.StartAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Something went wrong. Message: {Message}",
                    exception.Message);

                throw;
            }
        }

        private async Task InitializeAsync(CancellationToken cancellationToken)
        {
            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            foreach (var publisher in _publishers)
            {
                await publisher.InitializeAsync(_connection!, cancellationToken);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_connection == null)
            {
                var exception = new ConnectionNotEstablishedException();
                _logger.LogError(exception, "The connection to RabbitMQ is null. Message: {Message}",
                    exception.Message);

                throw exception;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay((int)_retryPublishTime.TotalMilliseconds, stoppingToken);
                
                foreach (var publisher in _publishers)
                {
                    // Check connection and channel statuses
                    if (!_connection.IsOpen)
                    {
                        await InitializeAsync(stoppingToken);
                    }
                    else if (publisher.Channel == null || !publisher.Channel!.IsOpen)
                    {
                        await publisher.InitializeAsync(_connection!, stoppingToken);
                    }

                    // Retry publishing messages that are not acknowledged or delivered
                    foreach (var message in publisher.PendingMessages.ToArray())
                    {
                        if (message.Value.IsPending)
                            continue;

                        if (message.Value.RetryCount >= publisher.MaxRetry)
                        {
                            _rejectedMessages.TryAdd(message.Value.GetHashCode(), message.Value);
                            publisher.PendingMessages.TryRemove(message.Key, out var _);
                            continue;
                        }

                        ++message.Value.RetryCount;
                        await publisher.PublishMessageAsync(message.Value, stoppingToken);
                    }

                    foreach (var message in publisher.DroppedMessages.ToArray())
                    {
                        if (message.Value.RetryCount >= publisher.MaxRetry)
                        {
                            _rejectedMessages.TryAdd(message.Value.GetHashCode(), message.Value);
                            publisher.DroppedMessages.TryRemove(message.Key, out var _);
                            continue;
                        }

                        ++message.Value.RetryCount;
                        await publisher.PublishMessageAsync(message.Value, stoppingToken);
                    }

                    // Save the messages that are rejected many times
                    if (_rejectedMessages.Count > 0)
                    {
                        try
                        {
                            await SaveRejectedMessagesAsync(_rejectedMessages.Values, isShuttingDown: false, stoppingToken);
                            _rejectedMessages.Clear();
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogWarning("The saving rejected messages operation is cancelled. The process will rerun in the StopAsync method.");
                        }
                        catch (Exception exception)
                        {
                            _logger.LogCritical(exception, "Someting went wrong while saving the rejected messages. The process will rerun in {RetryPubishTime} seconds. Message: {Message}",
                                _retryPublishTime.TotalSeconds,
                                exception.Message);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves the messages that are rejected.
        /// </summary>
        /// <param name="rejectedMessages">Messages that are attempted to send to RabbitMQ many times but rejected</param>
        /// <param name="isShuttingDown">Whether this is the last call for saving the rejected messages</param>
        /// <param name="cancellationToken">Token to cancel the saving process</param>
        protected abstract Task SaveRejectedMessagesAsync(
            IEnumerable<Message> rejectedMessages,
            bool isShuttingDown,
            CancellationToken cancellationToken = default);

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // Wait for a small amount of time in case RabbitMQ fires some events
            await Task.Delay(GraceTime);

            // Add the last messages that are pending or are dropped to the rejected messages
            foreach (var publisher in _publishers)
            {
                foreach (var message in publisher.PendingMessages)
                {
                    _rejectedMessages.TryAdd(message.Value.GetHashCode(), message.Value);
                }
                foreach (var message in publisher.DroppedMessages)
                {
                    _rejectedMessages.TryAdd(message.Value.GetHashCode(), message.Value);
                }
            }

            if (_rejectedMessages.Count > 0)
            {
                try
                {
                    await SaveRejectedMessagesAsync(_rejectedMessages.Values, isShuttingDown: true);
                }
                catch (Exception exception)
                {
                    _logger.LogCritical(exception, "Someting went wrong while saving the rejected messages. Message: {Message}",
                        exception.Message);
                }
            }
            
            foreach (var publisher in _publishers)
            {
                if (publisher.Channel != null)
                {
                    await publisher.Channel!.CloseAsync();
                    await publisher.Channel!.DisposeAsync();
                }
            }

            if (_connection != null)
            {
                await _connection!.CloseAsync();
                await _connection!.DisposeAsync();
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
