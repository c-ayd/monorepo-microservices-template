using AuthService.Application.Options;
using AuthService.Persistence.DistributedCaches;
using AuthService.Test.Integration.Persistence.Collections;
using Microsoft.Extensions.Options;
using Shared.Test.Helpers.Fixtures;

namespace AuthService.Test.Integration.Persistence.DistributedCaches
{
    [Collection(nameof(TokenBlacklistCollection))]
    public class TokenBlacklistTest
    {
        private readonly RedisFixture _tokenBlacklistRedisFixture;

        private readonly TokenBlacklist _tokenBlacklist;

        public TokenBlacklistTest(TokenBlacklistCollectionCluster collectionCluster)
        {
            _tokenBlacklistRedisFixture = collectionCluster.TokenBlacklistRedisFixture;

            var connStrings = new ConnectionStringsOptions()
            {
                AuthDb = "",
                AuthRejectedMessagesDb = "",
                AuthDataProtectionRedis = "",
                AuthTokenBlacklistRedis = _tokenBlacklistRedisFixture.GetConnectionString()
            };

            _tokenBlacklist = new TokenBlacklist(Options.Create(connStrings));
            _tokenBlacklist.ConnectAsync(default).GetAwaiter().GetResult();
        }

        [Fact]
        public async Task AddAsync_WhenEntryWithAccountIdDoesNotExist_ShouldCreateNewEntry()
        {
            // Arrange
            var accountId = Guid.NewGuid().ToString();
            var expirationTime = TimeSpan.FromMinutes(5);
            var now = DateTimeOffset.UtcNow;

            // Act
            await _tokenBlacklist.AddAsync(accountId, expirationTime);

            // Assert
            var db = _tokenBlacklistRedisFixture.GetDatabase();
            var result = await db.StringGetAsync(accountId);
            var remainingTime = await db.KeyTimeToLiveAsync(accountId);

            Assert.True(result.HasValue, "An entry with a given account ID is not found.");
            Assert.InRange((expirationTime - remainingTime!).Value.TotalMinutes, -1, 1);

            var value = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(result.ToString()));
            Assert.InRange((value - now).TotalMinutes, -1, 1);
        }

        [Fact]
        public async Task AddAsync_WhenEntryWithAccountIdExists_ShouldReplaceValueAndResetExpirationTime()
        {
            // Arrange
            var accountId = Guid.NewGuid().ToString();
            var expirationTime = TimeSpan.FromMinutes(5);
            var now = DateTimeOffset.UtcNow;

            await _tokenBlacklistRedisFixture.GetDatabase().StringSetAsync(accountId, DateTimeOffset.UtcNow.AddMonths(-1).ToUnixTimeSeconds(), TimeSpan.FromHours(1));

            // Act
            await _tokenBlacklist.AddAsync(accountId, expirationTime);

            // Assert
            var result = await _tokenBlacklistRedisFixture.GetDatabase().StringGetAsync(accountId);
            var remainingTime = await _tokenBlacklistRedisFixture.GetDatabase().KeyTimeToLiveAsync(accountId);

            Assert.True(result.HasValue, "An entry with a given account ID is not found.");
            Assert.InRange((expirationTime - remainingTime!).Value.TotalMinutes, -1, 1);

            var value = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(result.ToString()));
            Assert.InRange((value - now).TotalMinutes, -1, 1);
        }

        [Fact]
        public async Task DeleteAsync_WhenEntryWithAccountIdDoesNotExist_Should()
        {
            // Act
            await _tokenBlacklist.DeleteAsync(Guid.NewGuid().ToString());
        }

        [Fact]
        public async Task DeleteAsync_WhenEntryWithAccountIdExists_ShouldDeleteEntry()
        {
            // Arrange
            var accountId = Guid.NewGuid().ToString();

            await _tokenBlacklistRedisFixture.GetDatabase().StringSetAsync(accountId, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), TimeSpan.FromMinutes(5));

            // Act
            await _tokenBlacklist.DeleteAsync(accountId);

            // Assert
            var result = await _tokenBlacklistRedisFixture.GetDatabase().StringGetAsync(accountId);
            Assert.False(result.HasValue, "An entry with a given account ID exists.");
        }
    }
}
