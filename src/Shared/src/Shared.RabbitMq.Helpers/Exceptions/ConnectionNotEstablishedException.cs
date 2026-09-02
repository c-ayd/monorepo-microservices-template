namespace Shared.RabbitMq.Helpers.Exceptions
{
    public class ConnectionNotEstablishedException : Exception
    {
        public ConnectionNotEstablishedException()
            : base("The connection to RabbitMQ is not established.")
        {
        }
    }
}
