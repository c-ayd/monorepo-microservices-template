using AuthService.Domain.Entities;
using AuthService.Persistence.DbContexts;
using AuthService.Persistence.Exceptions;
using AuthService.Persistence.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Persistence.SeedData
{
    public static class AuthDbContextSeedData
    {
        public static async Task SeedDataAuthDbContextAsync(this IServiceProvider services, IConfiguration configuration)
        {
            using var scope = services.CreateAsyncScope();
            using var authDbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            var seedDataOptions = configuration.GetSection(SeedDataOptions.Key).Get<SeedDataOptions>()!;

            await authDbContext.Database.MigrateAsync();

            await AddDefaultRolesAsync(authDbContext, seedDataOptions);
            await AddDefaultAccountsAsync(authDbContext, seedDataOptions);
        }

        private static async Task AddDefaultRolesAsync(AuthDbContext authDbContext, SeedDataOptions options)
        {
            if (await authDbContext.Roles.AnyAsync())
                return;

            foreach (var role in options.AuthDb.Roles)
            {
                await authDbContext.Roles.AddAsync(new Role(role));
            }

            await authDbContext.SaveChangesAsync();
        }

        private static async Task AddDefaultAccountsAsync(AuthDbContext authDbContext, SeedDataOptions options)
        {
            if (await authDbContext.Accounts.AnyAsync())
                return;

            foreach (var accountRolePair in options.AuthDb.Accounts)
            {
                var role = await authDbContext.Roles.FirstOrDefaultAsync(r => r.Name == accountRolePair.Role);
                if (role == null)
                    throw new SeedDataEntryNotFoundException($"A role named {accountRolePair.Role} is not found in the database.");

                var account = new Account(accountRolePair.Email, "123");

                account.IsEmailVerified = true;
                account.Roles.Add(role);
                await authDbContext.Accounts.AddAsync(account);
            }

            await authDbContext.SaveChangesAsync();
        }
    }
}
