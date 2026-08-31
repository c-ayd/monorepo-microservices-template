using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Shared.Redis.Extensions
{
    public static class StringExtensions
    {
        private static readonly JsonSerializerOptions JsonWriteOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly JsonSerializerOptions JsonReadOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Converts a given value to a JSON string and saves the string to Redis DB.
        /// </summary>
        /// <typeparam name="T">Type of the given value</typeparam>
        /// <param name="key">Key of the Redis entry</param>
        /// <param name="value">Value to serialize to a JSON string</param>
        /// <param name="expirationTime">Lifespan of the entry. The default value is 1 hour</param>
        /// <param name="slideExpirationTime">Relative expiration time since the value is accessed</param>
        /// <param name="cancellationToken">Token to cancel the saving process</param>
        public static async Task SaveAsStringAsync<T>(this IDistributedCache redis,
            string key,
            T value,
            TimeSpan? expirationTime = null,
            TimeSpan? slideExpirationTime = null,
            CancellationToken cancellationToken = default)
        {
            var options = new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = expirationTime ?? TimeSpan.FromHours(1),
                SlidingExpiration = slideExpirationTime
            };

            var json = JsonSerializer.Serialize(value, JsonWriteOptions);
            await redis.SetStringAsync(key, json, options, cancellationToken);
        }

        /// <summary>
        /// Converts a JSON string entry from Redis DB to a given type and returns the value.
        /// </summary>
        /// <typeparam name="T">Type to deserialize the JSON string to</typeparam>
        /// <param name="key">Key of the Redis entry</param>
        /// <param name="cancellationToken">Token to cancel the fetching operation</param>
        /// <returns>Returns the converted value.</returns>
        public static async Task<(bool isKeyFound, T? value)> LoadAsStringAsync<T>(this IDistributedCache redis,
            string key,
            CancellationToken cancellationToken = default)
        {
            var json = await redis.GetStringAsync(key, cancellationToken);
            if (json == null)
                return (false, default);

            var value = JsonSerializer.Deserialize<T>(json, JsonReadOptions);
            return (true, value);
        }
    }
}
