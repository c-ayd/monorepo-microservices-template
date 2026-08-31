using System.Net;
using System.Security.Claims;
using AuthService.Application.Abstractions.Authentication;
using AuthService.Application.Abstractions.Crypto;
using AuthService.Application.Abstractions.DbContexts;
using AuthService.Application.Dtos.Crypto;
using AuthService.Application.Options;
using AuthService.Application.Validations.Constraints;
using AuthService.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Crypto;
using Shared.Http.Authentication;
using Shared.Http.Response;
using Shared.Http.Response.Structures;

namespace AuthService.Application.Features.AccountEndpoints.Login
{
    public class LoginHandler
    {
        public static async Task<IResult> Handle(
            LoginRequest request,
            IAuthDbContext authDbContext,
            IPasswordHasher passwordHasher,
            IOptions<AccountLockOptions> accountLockOptions,
            IJwtService jwtService,
            IHashVersions hashVersions,
            HttpContext context,
            IDataProtectionService dataProtectionService,
            ILogger<LoginHandler> logger)
        {
            // Check if the account exists
            var account = await authDbContext.Accounts
                .Where(a => a.Email == request.Email)
                .Include(a => a.Roles)
                .FirstOrDefaultAsync();

            if (account == null)
                return JsonResponseBuilder.Error(
                    HttpStatusCode.BadRequest,
                    [
                        new ErrorItem("auth_wrong_credentials", "The credentials are wrong.")
                    ]
                );

            // Check if the account is banned
            if (account.IsBanned)
                return JsonResponseBuilder.Error(
                    HttpStatusCode.Forbidden,
                    [
                        new ErrorItem("auth_account_banned", "The account is banned.")
                    ]
                );

            // Check if the account is locked
            if (account.IsLocked)
            {
                if (DateTimeOffset.UtcNow < account.UnlockDate)
                {
                    ++account.FailedLoginAttempts;

                    await authDbContext.SaveChangesAsync();

                    return JsonResponseBuilder.Error(
                        HttpStatusCode.Locked,
                        [
                            new ErrorItem("auth_account_locked", "The account is locked.")
                        ],
                        metadata: new
                        {
                            UnlockDate = account.UnlockDate.Value.ToUnixTimeMilliseconds()
                        }
                    );
                }

                account.IsLocked = false;
                account.FailedLoginAttempts = 0;
            }

            // Verify password
            var passwordVerificationResult = passwordHasher.Verify(account.PasswordHashed!, request.Password!, out var version);
            switch (passwordVerificationResult)
            {
                case EPasswordVerificationResult.Fail:
                    ++account.FailedLoginAttempts;
                    if (account.FailedLoginAttempts >= accountLockOptions.Value.NumberOfFailedAttempsBeforeLock)
                    {
                        var multiplier = Math.Min(account.FailedLoginAttempts - accountLockOptions.Value.NumberOfFailedAttempsBeforeLock + 1,
                            accountLockOptions.Value.MaxLockTimeMultiplier);

                        account.IsLocked = true;
                        account.UnlockDate = DateTimeOffset.UtcNow.AddMinutes(accountLockOptions.Value.LockTimeInMinutes * multiplier);

                        await authDbContext.SaveChangesAsync();

                        return JsonResponseBuilder.Error(
                            HttpStatusCode.Locked,
                            [
                                new ErrorItem("auth_account_locked_login", "The account is locked due to failed login attemps.")
                            ],
                            metadata: new
                            {
                                UnlockDate = account.UnlockDate.Value.ToUnixTimeMilliseconds()
                            }
                        );
                    }

                    await authDbContext.SaveChangesAsync();

                    return JsonResponseBuilder.Error(
                        HttpStatusCode.BadRequest,
                        [
                            new ErrorItem("auth_wrong_credentials", "The credentials are wrong.")
                        ],
                        metadata: new
                        {
                            account.FailedLoginAttempts
                        }
                    );
                case EPasswordVerificationResult.VersionNotFound:
                    logger.LogError("The version of the hashed password could not be found. Version: {Version}",
                        version);

                    return JsonResponseBuilder.Error(
                        HttpStatusCode.InternalServerError,
                        [
                            new ErrorItem("internal_server_error", "Something went wrong.")
                        ]
                    );
                case EPasswordVerificationResult.LengthMismatch:
                    logger.LogError("The expected length of the hashed password does not match.");

                    return JsonResponseBuilder.Error(
                        HttpStatusCode.InternalServerError,
                        [
                            new ErrorItem("internal_server_error", "Something went wrong.")
                        ]
                    );
                case EPasswordVerificationResult.Success:
                    account.FailedLoginAttempts = 0;
                    break;
                case EPasswordVerificationResult.SuccessRehashNeeded:
                    account.FailedLoginAttempts = 0;
                    account.PasswordHashed = passwordHasher.Hash(request.Password!);
                    break;
                default:
                    logger.LogError("The result of the password verification is out of the range. Result: {Result}",
                        (int)passwordVerificationResult);

                    return JsonResponseBuilder.Error(
                        HttpStatusCode.InternalServerError,
                        [
                            new ErrorItem("internal_server_error", "Something went wrong.")
                        ]
                    );
            }

            // Generate JWT
            var claims = new List<Claim>()
            {
                new Claim(ApiGatewayAuthKeys.Claims.Id.ClaimType, account.Id.ToString()),
                new Claim(ApiGatewayAuthKeys.Claims.EmailVerified.ClaimType, account.IsEmailVerified.ToString().ToLower()),
                new Claim(ApiGatewayAuthKeys.Claims.PreferredLanguage.ClaimType, account.PreferredLanguage?.ToString() ?? 
                    AccountConstraints.SuppoertedLanguages[0]),
                new Claim(ApiGatewayAuthKeys.Claims.IssuedAt.ClaimType, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            foreach (var role in account.Roles)
            {
                claims.Add(new Claim(ApiGatewayAuthKeys.Claims.Roles.ClaimType, role.Name));
            }

            var jwt = jwtService.GenerateTokens(claims);

            // Add a new session to the DB
            var newSession = new Session(
                account.Id,
                ValueHasher.Hash(jwt.RefreshToken, hashVersions.CurrentHashVersion, hashVersions.GetHashOptions),
                jwt.RefreshTokenExpirationDate,
                context.Connection.RemoteIpAddress,
                context.Request.Headers.UserAgent);

            await authDbContext.Sessions.AddAsync(newSession);
            await authDbContext.SaveChangesAsync();

            // Add the session ID and refresh token to the cookies
            var protectedSessionId = dataProtectionService.Protect(dataProtectionService.CookieProtector, newSession.Id.ToString());
            context.Response.Cookies.Append(CookieKeys.SessionId, protectedSessionId, new CookieOptions()
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = jwt.RefreshTokenExpirationDate
            });

            var protectedRefreshToken = dataProtectionService.Protect(dataProtectionService.CookieProtector, jwt.RefreshToken);
            context.Response.Cookies.Append(CookieKeys.RefreshToken, protectedRefreshToken, new CookieOptions()
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = jwt.RefreshTokenExpirationDate
            });

            return JsonResponseBuilder.SuccessWithData(
                HttpStatusCode.OK,
                data: new
                {
                    jwt.AccessToken,
                    account.Roles,
                },
                metadata: new
                {
                    account.IsEmailVerified,
                    account.PreferredLanguage
                }
            );
        }
    }
}
