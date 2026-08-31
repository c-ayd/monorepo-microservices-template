namespace AuthService.Application.Abstractions.DistributedCaches
{
    /// <summary>
    /// Provides methods to blacklist access tokens.
    /// </summary>
    public interface ITokenBlacklist
    {
        /// <summary>
        /// Adds all currently active access tokens of a specific account to the blacklist.
        /// </summary>
        /// <param name="accountId">ID of the account</param>
        /// <param name="accessTokenLifespan">Lifespan of an access token</param>
        Task AddAsync(string accountId, TimeSpan accessTokenLifespan);

        /// <summary>
        /// Removes the blacklist entry of a specific account.
        /// </summary>
        /// <param name="accountId">ID of the account</param>
        Task DeleteAsync(string accountId);
    }
}
