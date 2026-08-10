using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AuthService.Application.Abstractions.DbContexts
{
    /// <summary>
    /// Provides the base abstraction for EF Core.
    /// </summary>
    public interface IDbContextBase
    {
        DatabaseFacade Database { get; }

        /// <summary>
        /// Saves all changes made to the database.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the saving process</param>
        /// <returns>Returns the number of affected rows.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
