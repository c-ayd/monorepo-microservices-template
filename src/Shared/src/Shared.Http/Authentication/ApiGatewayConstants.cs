using System.Security.Claims;
using Shared.Http.Authentication.Structures;

namespace Shared.Http.Authentication
{
    public static class ApiGatewayConstants
    {
        public const string AuthenticationScheme = "API-Gateway";

        public static readonly UserClaim UserId = new UserClaim(ClaimTypes.NameIdentifier, "X-User-Id");
        public static readonly UserClaim UserRoles = new UserClaim(ClaimTypes.Role, "X-User-Roles");

        public static readonly List<UserClaim> UserClaims = new List<UserClaim>()
        {
            new UserClaim("email_verified", "X-User-Email-Verified")
        };
    }
}
