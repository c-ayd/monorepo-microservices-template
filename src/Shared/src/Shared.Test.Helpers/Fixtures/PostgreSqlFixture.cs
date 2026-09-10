using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Shared.Test.Helpers.Fixtures
{
    /// <summary>
    /// Is a DbContext for test cases requiring a DB with EF Core.
    /// </summary>
    public class PostgreSqlFixture
    {
        private PostgreSqlContainer _container = null!;
        private Dictionary<string, Type>? _dbContexts;

        public async Task InitializeAsync(Dictionary<string, Type>? dbContexts = null)
        {
            _container = new PostgreSqlBuilder("postgres:18.4")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
                .Build();
            await _container.StartAsync();

            _dbContexts = dbContexts;
            if (_dbContexts != null)
            {
                foreach (var (dbName, _) in _dbContexts)
                {
                    using var dbContext = CreateDbContext(dbName);
                    await dbContext.Database.MigrateAsync();
                }
            }
        }

        public string GetConnectionString(string dbName)
        {
            return new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
            {
                Database = dbName
            }.ConnectionString;
        }

        public T CreateDbContext<T>(string dbName)
            where T : DbContext
        {
            var dbContextType = _dbContexts![dbName];       // No null check. If it is used wrong in test, it should throw an exception.
            var connString = GetConnectionString(dbName);

            var ctor = dbContextType.GetConstructor([
                typeof(DbContextOptions<>).MakeGenericType(dbContextType)
            ])!;

            var options = ((DbContextOptionsBuilder)Activator.CreateInstance(typeof(DbContextOptionsBuilder<>).MakeGenericType(dbContextType))!)
                .UseNpgsql(connString)
                .Options;

            return (T)ctor.Invoke([options]);
        }

        public DbContext CreateDbContext(string dbName)
        {
            return CreateDbContext<DbContext>(dbName);
        }

        public async Task DisposeAsync()
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
