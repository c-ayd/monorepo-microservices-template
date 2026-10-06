using StackExchange.Redis;

namespace Shared.Redis
{
    /// <summary>
    /// Handles Redis connection.
    /// </summary>
    public abstract class RedisConnection
    {
        internal ConnectionMultiplexer? Connection { get; private set; }
        private SemaphoreSlim _connectionSemaphore = new SemaphoreSlim(1, 1);

        private readonly string _connectionString;

        public RedisConnection(string connectionString)
        {
            _connectionString = connectionString;
        }

        internal async Task ConnectAsync(CancellationToken cancellationToken)
        {
            // This method should be called only once. This semaphore is used as a safeguard.
            await _connectionSemaphore.WaitAsync(cancellationToken);

            if (Connection != null)
            {
                _connectionSemaphore.Release();
                return;
            }

            // Since the connection to Redis would be required for services to function, the code below
            // is not wrapped with a catch block and should throw an exception if something goes wrong.
            try
            {
                Connection = await ConnectionMultiplexer.ConnectAsync(_connectionString);
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        /// <summary>
        /// Gets a database with a given index in the Redis instance.
        /// </summary>
        /// <param name="db">Index of the database. -1 for the default database</param>
        /// <returns>Returns the database with the given index in the Redis instance.</returns>
        protected IDatabase GetDatabase(int db = -1)
        {
            return Connection!.GetDatabase(db);
        }
    }
}
