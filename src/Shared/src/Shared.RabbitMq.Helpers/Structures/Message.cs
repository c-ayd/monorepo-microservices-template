using RabbitMQ.Client;

namespace Shared.RabbitMq.Helpers.Structures
{
    /// <summary>
    /// Represents a message that is sent to RabbitMQ.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// The name of the publisher that has sent or has tried to sent this message
        /// </summary>
        public string PublisherName { get; init; }

        /// <summary>
        /// The name of the exchange that this message is sent to
        /// </summary>
        public string ExchangeName { get; init; }

        /// <summary>
        /// The routing key for this message
        /// </summary>
        public string RoutingKey { get; init; }

        /// <summary>
        /// The properties of the message
        /// </summary>
        public BasicProperties Properties { get; init; }

        /// <summary>
        /// The message body
        /// </summary>
        public byte[] Body { get; init; }

        internal ulong DeliveryTag { get; set; }
        internal bool IsPending { get; set; }
        internal int RetryCount { get; set; }

        public Message(
            string publisherName,
            string exchangeName,
            string routingKey,
            BasicProperties properties,
            byte[] body)
        {
            PublisherName = publisherName;
            ExchangeName = exchangeName;
            RoutingKey = routingKey;
            Properties = properties;
            Body = body;
            DeliveryTag = 0;
            IsPending = true;
            RetryCount = 0;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                PublisherName,
                ExchangeName,
                RoutingKey,
                Properties.CorrelationId!.GetHashCode());
        }
    }
}
