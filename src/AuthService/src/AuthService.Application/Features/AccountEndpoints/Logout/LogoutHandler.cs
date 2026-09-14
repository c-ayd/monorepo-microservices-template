using System.Net;
using System.Security.Cryptography;
using AuthService.Application.Abstractions.Crypto;
using AuthService.Application.Abstractions.DbContexts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Crypto;
using Shared.Crypto.Exceptions;
using Shared.Http.Authentication;
using Shared.Http.Response;

namespace AuthService.Application.Features.AccountEndpoints.Logout
{
    public class LogoutHandler
    {
        public static async Task<IResult> Handle(
            HttpContext context,
            IDataProtectionService dataProtectionService,
            IHashVersions hashVersions,
            IAuthDbContext authDbContext)
        {
            // If the user does not have the cookie values, the session data is lost on the client side.
            // In this case, a response with the OK code is still returned, so that the client can log out on the frontend.
            if (context.Request.Cookies[CookieKeys.SessionId] == null || context.Request.Cookies[CookieKeys.RefreshToken] == null)
                return JsonResponseBuilder.Success(HttpStatusCode.NoContent);

            string sessionId, refreshToken;
            try
            {
                sessionId = dataProtectionService.Unprotect(dataProtectionService.CookieProtector, context.Request.Cookies[CookieKeys.SessionId]!);
                refreshToken = dataProtectionService.Unprotect(dataProtectionService.CookieProtector, context.Request.Cookies[CookieKeys.RefreshToken]!);
            }
            catch (CryptographicException)
            {
                // If the decryption throws an error, it means the values are altered. In this case, no session data should be deleted.
                // A response with the OK code is still returned, so that the client can log out on the frontend.
                return JsonResponseBuilder.Success(HttpStatusCode.NoContent);
            }
            
            var session = await authDbContext.Sessions
                .Where(s => s.Id == Guid.Parse(sessionId))
                .Select(s => new
                {
                    s.RefreshTokenHashed,
                    s.AccountId
                })
                .FirstOrDefaultAsync();

            // If the account ID and refresh tokens of the session do not match, do not delete the session. Nevertheless, 
            // a response with the OK code is still returned, so that the client can log out on the frontend.
            try
            {
                if (session == null ||
                    session.AccountId != Guid.Parse(context.User.Identity!.Name!) ||
                    !ValueHasher.Verify(session.RefreshTokenHashed, refreshToken, hashVersions.GetHashOptions, out var _))
                    return JsonResponseBuilder.Success(HttpStatusCode.NoContent);
            }
            catch (Exception exception) when (
                exception is CryptographicException ||
                exception is FormatException ||
                exception is HashLengthMismatchException)
            {
                return JsonResponseBuilder.Success(HttpStatusCode.NoContent);
            }

            await authDbContext.Sessions
                .Where(s => s.Id == Guid.Parse(sessionId))
                .ExecuteDeleteAsync();

            return JsonResponseBuilder.Success(HttpStatusCode.NoContent);
        }
    }
}
