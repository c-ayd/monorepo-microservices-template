namespace NotificationService.Worker.Exceptions
{
    public class EmailBackgroundServiceConnectionException : Exception
    {
        public EmailBackgroundServiceConnectionException(string message)
            : base(message)
        {
        }
    }
}
