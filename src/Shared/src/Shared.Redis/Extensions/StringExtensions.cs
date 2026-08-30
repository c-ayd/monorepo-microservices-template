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
