using AuthDb.Initializer.Exceptions;
using AuthDb.Initializer.Options;
using AuthService.Domain.Entities;
using AuthService.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shared.Constants;

namespace AuthDb.Initializer
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            System.Console.WriteLine("AuthDB Initializer started.");

            bool force = args.Contains("--force") || args.Contains("-f");  // If true, it will try to seed data whether there is data or not in the DB.

            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();
            var seedDataOptions = configuration.GetSection("SeedData").Get<AuthDbSeedDataOptions>()!;

            await using var authDbContext = new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>()
                .UseNpgsql(configuration.GetConnectionString("AuthDb"))
                .Options);

            await InitializeAsync(force, seedDataOptions, authDbContext);

            return 0;
        }

        private static async Task InitializeAsync(bool force, AuthDbSeedDataOptions seedDataOptions, AuthDbContext authDbContext)
        {
            // Migrate
            await authDbContext.Database.MigrateAsync();

            System.Console.WriteLine("Migration completed.");

            // Seed data
            await using var transaction = await authDbContext.Database.BeginTransactionAsync();
            try
            {
                await SeedRolesAsync(force, seedDataOptions, authDbContext);
                await SeedAccountsAsync(force, seedDataOptions, authDbContext);

                await transaction.CommitAsync();

                System.Console.WriteLine("Seed data is committed to the DB.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                System.Console.WriteLine($"Something went wrong. Exiting without saving any seed data...");
                throw;
            }
        }

        private static async Task SeedRolesAsync(bool force, AuthDbSeedDataOptions seedDataOptions, AuthDbContext authDbContext)
        {
            if (await authDbContext.Roles.AnyAsync() && !force)
            {
                System.Console.WriteLine("There are already some roles in the DB. Skipping adding roles... (Use --force or -f flag if there is any new role in the seed data options).");
                return;
            }

            int counter = 0;
            foreach (var role in seedDataOptions.Roles)
            {
                if (force && await authDbContext.Roles.AnyAsync(r => r.Name == role))
                {
                    System.Console.WriteLine($"Role {role} is already in the DB. Skipping...");
                    continue;
                }

                await authDbContext.Roles.AddAsync(new Role(role));
                ++counter;
            }

            await authDbContext.SaveChangesAsync();

            System.Console.WriteLine($"{counter} role(s) are added to the DB.");
        }

        private static async Task SeedAccountsAsync(bool force, AuthDbSeedDataOptions seedDataOptions, AuthDbContext authDbContext)
        {
            if (await authDbContext.Accounts.AnyAsync() && !force)
            {
                System.Console.WriteLine("There are already some accounts in the DB. Skipping adding accounts... (Use --force or -f flag if there is any new account in the seed data options).");
                return;
            }

            var counter = 0;
            foreach (var accountDetails in seedDataOptions.Accounts)
            {
                if (force && await authDbContext.Accounts.AnyAsync(a => a.Email == accountDetails.Email))
                {
                    System.Console.WriteLine($"Account with the email {accountDetails.Email} is already in the DB. Skipping...");
                    continue;
                }

                var newAccount = new Account(
                    accountDetails.Email,
                    "123",  // The hashed password is given as a random string, which means no password will match.
                            // The email owner must use the reset password feature to start using their account.
                    SupportedLanguages.DefaultLanguage);

                if (accountDetails.Role != null)
                {
                    var role = await authDbContext.Roles.FirstOrDefaultAsync(r => r.Name == accountDetails.Role);
                    if (role == null)
                        throw new RoleNotFoundException(accountDetails.Role);

                    newAccount.Roles.Add(role);
                }

                if (accountDetails.PreferredLanguage != null)
                {
                    if (SupportedLanguages.AllLanguages.Contains(accountDetails.PreferredLanguage))
                    {
                        newAccount.PreferredLanguage = accountDetails.PreferredLanguage;
                    }
                }

                await authDbContext.Accounts.AddAsync(newAccount);
                ++counter;
            }

            await authDbContext.SaveChangesAsync();

            System.Console.WriteLine($"{counter} account(s) are added to the DB.");
        }
    }
}
