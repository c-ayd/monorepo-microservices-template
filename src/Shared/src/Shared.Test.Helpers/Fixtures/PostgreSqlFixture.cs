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
        private BidirectionalDictionary<string, Type> _dbContexts = new BidirectionalDictionary<string, Type>();

        public async Task InitializeAsync(Dictionary<string, Type>? dbContexts = null)
        {
            _container = new PostgreSqlBuilder("postgres:18.4")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
                .Build();
            await _container.StartAsync();

            if (dbContexts != null)
            {
                _dbContexts.Add(dbContexts);
                foreach (var (dbName, dbContextType) in _dbContexts)
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

        public DbContext CreateDbContext(string dbName)
        {
            _dbContexts.TryGetValue(dbName, out var dbContexType);
            return CreateDbContext<DbContext>(dbName, dbContexType!); // No null check. If it is used wrong in test, it should throw an exception.
        }

        public T CreateDbContext<T>()
            where T : DbContext
        {
            _dbContexts.TryGetKey(typeof(T), out var dbName);
            return CreateDbContext<T>(dbName!, typeof(T)); // No null check. If it is used wrong in test, it should throw an exception.
        }

        private T CreateDbContext<T>(string dbName, Type dbContextType)
            where T : DbContext
        {
            var connString = GetConnectionString(dbName);

            var ctor = dbContextType.GetConstructor([
                typeof(DbContextOptions<>).MakeGenericType(dbContextType)
            ])!;

            var options = ((DbContextOptionsBuilder)Activator.CreateInstance(typeof(DbContextOptionsBuilder<>).MakeGenericType(dbContextType))!)
                .UseNpgsql(connString)
                .Options;

            return (T)ctor.Invoke([options]);
        }

        public async Task DisposeAsync()
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }

        private class BidirectionalDictionary<TKey, TValue>
            where TKey : notnull
            where TValue : notnull
        {
            private Dictionary<TKey, TValue> _forward = new Dictionary<TKey, TValue>();
            private Dictionary<TValue, TKey> _backward = new Dictionary<TValue, TKey>();

            public void Add(Dictionary<TKey, TValue> dictionary)
            {
                foreach (var item in dictionary)
                {
                    _forward.Add(item.Key, item.Value);
                    _backward.Add(item.Value, item.Key);
                }
            }

            public bool TryGetValue(TKey key, out TValue? value)
            {
                return _forward.TryGetValue(key, out value);
            }

            public bool TryGetKey(TValue value, out TKey? key)
            {
                return _backward.TryGetValue(value, out key);
            }

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                return _forward.GetEnumerator();
            }
        }
    }
}
