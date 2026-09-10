using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NotificationService.Worker.DbContexts;
using NotificationService.Worker.Options;
using Shared.Test.Helpers.Fixtures;

namespace NotificationService.Test.Integration.Worker.Collections
{
    [CollectionDefinition(nameof(WorkerCollection))]
    public class WorkerCollection : ICollectionFixture<WorkerCollectionCluster>
    {
    }

    public class WorkerCollectionCluster : IAsyncLifetime
    {
        public const string TemplateDbName = "template-db";

        public PostgreSqlFixture PostgreSqlFixture { get; private set; }
        public RabbitMqFixture RabbitMqFixture { get; private set; }
        public SmtpFixture SmtpFixture { get; private set; }

        public NotificationWebAppFactory NotificationWebApp { get; private set; } = null!;

        public WorkerCollectionCluster()
        {
            PostgreSqlFixture = new PostgreSqlFixture();
            RabbitMqFixture = new RabbitMqFixture();
            SmtpFixture = new SmtpFixture();
        }

        public async Task InitializeAsync()
        {
            await Task.WhenAll(
                PostgreSqlFixture.InitializeAsync(new Dictionary<string, Type>()
                {
                    { TemplateDbName, typeof(TemplateDbContext) }
                }),
                RabbitMqFixture.InitializeAsync(),
                SmtpFixture.InitializeAsync()
            );

            var rabbitMqOptions = RabbitMqFixture.GetRabbitMqOptions();
            var smtpOptions = SmtpFixture.GetSmtpOptions();
            NotificationWebApp = new NotificationWebAppFactory(
                new ConnectionStringsOptions()
                {
                    TemplateDb = PostgreSqlFixture.GetConnectionString(TemplateDbName)
                },
                new RabbitMqOptions()
                {
                    Username = rabbitMqOptions.Username,
                    Password = rabbitMqOptions.Password,
                    Host = rabbitMqOptions.Host,
                    Port = rabbitMqOptions.Port,
                },
                new SmtpOptions()
                {
                    Username = "",
                    Password = "",
                    SenderEmail = smtpOptions.Email,
                    SenderDisplayName = smtpOptions.DisplayName,
                    Server = smtpOptions.Server,
                    Port = smtpOptions.Port,
                    EnableSsl = smtpOptions.EnableSsl
                });
            NotificationWebApp.StartServer();
        }

        public async Task DisposeAsync()
        {
            await Task.WhenAll(
                PostgreSqlFixture.DisposeAsync(),
                RabbitMqFixture.DisposeAsync(),
                SmtpFixture.DisposeAsync()
            );
            
            await NotificationWebApp.DisposeAsync();
        }

        public class NotificationWebAppFactory : WebApplicationFixture<Program>
        {
            private readonly ConnectionStringsOptions _connectionStringsOptions;
            private readonly RabbitMqOptions _rabbitMqOptions;
            private readonly SmtpOptions _smtpOptions;

            public NotificationWebAppFactory(
                ConnectionStringsOptions connectionStringsOptions,
                RabbitMqOptions rabbitMqOptions,
                SmtpOptions smtpOptions)
            {
                _connectionStringsOptions = connectionStringsOptions;
                _rabbitMqOptions = rabbitMqOptions;
                _smtpOptions = smtpOptions;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.Configure(_ => { });

                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection([
                        new KeyValuePair<string, string?>($"{ConnectionStringsOptions.Key}:{nameof(ConnectionStringsOptions.TemplateDb)}",
                            _connectionStringsOptions.TemplateDb),

                        new KeyValuePair<string, string?>($"{RabbitMqOptions.Key}:{nameof(RabbitMqOptions.Username)}",
                            _rabbitMqOptions.Username),
                        new KeyValuePair<string, string?>($"{RabbitMqOptions.Key}:{nameof(RabbitMqOptions.Password)}",
                            _rabbitMqOptions.Password),
                        new KeyValuePair<string, string?>($"{RabbitMqOptions.Key}:{nameof(RabbitMqOptions.Host)}",
                            _rabbitMqOptions.Host),
                        new KeyValuePair<string, string?>($"{RabbitMqOptions.Key}:{nameof(RabbitMqOptions.Port)}",
                            _rabbitMqOptions.Port.ToString()),

                        new KeyValuePair<string, string?>($"{SmtpOptions.Key}:{nameof(SmtpOptions.Username)}",
                            _smtpOptions.Username),
                        new KeyValuePair<string, string?>($"{SmtpOptions.Key}:{nameof(SmtpOptions.Password)}",
                            _smtpOptions.Password),
                        new KeyValuePair<string, string?>($"{SmtpOptions.Key}:{nameof(SmtpOptions.SenderEmail)}",
                            _smtpOptions.SenderEmail),
                        new KeyValuePair<string, string?>($"{SmtpOptions.Key}:{nameof(SmtpOptions.SenderDisplayName)}",
                            _smtpOptions.SenderDisplayName),
                        new KeyValuePair<string, string?>($"{SmtpOptions.Key}:{nameof(SmtpOptions.Server)}",
                            _smtpOptions.Server),
                        new KeyValuePair<string, string?>($"{SmtpOptions.Key}:{nameof(SmtpOptions.Port)}",
                            _smtpOptions.Port.ToString()),
                        new KeyValuePair<string, string?>($"{SmtpOptions.Key}:{nameof(SmtpOptions.EnableSsl)}",
                            _smtpOptions.EnableSsl.ToString()),
                    ]);
                });
            }
        }
    }
}
