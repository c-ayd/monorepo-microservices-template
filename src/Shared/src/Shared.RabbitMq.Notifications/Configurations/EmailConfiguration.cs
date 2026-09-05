using RabbitMQ.Client;

namespace Shared.RabbitMq.Notifications.Configurations
{
    public static class EmailConfiguration
    {
        public const string ExchangeName = "notification.events";
        public const string ExchangeType = RabbitMQ.Client.ExchangeType.Topic;
        public const string DlxName = "notification.dlx";
        public const string DlxExchangeType = RabbitMQ.Client.ExchangeType.Direct;

        public const string QueueName = "notification.email";
        public const string DlqName = "notification.email.dlq";
        public const string RoutingKey = "notification.send.email";
        public const string DeadLetterRoutingKey = "notification.send.email.dead";
    }
}
