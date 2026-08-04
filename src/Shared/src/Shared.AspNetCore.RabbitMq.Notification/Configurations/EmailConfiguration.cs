namespace Shared.AspNetCore.RabbitMq.Notification.Configurations
{
    public static class EmailConfiguration
    {
        public const string ExchangeName = "notification.events";
        public const string DlxName = "notification.dlx";

        public const string QueueName = "notification.email";
        public const string DlqName = "notification.email.dlq";
        public const string RoutingKey = "notification.send.email";
        public const string DeadLetterRoutingKey = "notification.send.email.dead";
    }
}
