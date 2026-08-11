using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Application.Abstractions.DbContexts
{
    /// <summary>
    /// Provides an access to AuthDbContext.
    /// </summary>
    public interface IAuthDbContext : IDbContextBase
    {
        DbSet<Account> Accounts { get; }
        DbSet<Role> Roles { get; }
        DbSet<Session> Sessions { get; }
        DbSet<Token> Tokens { get; }
    }
}
