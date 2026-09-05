namespace Shared.RabbitMq.Notifications.Templates
{
    public static class EmailTemplates
    {
        /// <summary>
        /// This template requires the following parameters.
        /// <para>
        /// Body Parameters:
        /// <br/>
        /// {0} - Token value
        /// </para>
        /// </summary>
        public const string EmailVerification = "email_verification";
    }
}
