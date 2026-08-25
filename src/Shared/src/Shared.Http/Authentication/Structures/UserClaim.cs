namespace Shared.Http.Authentication.Structures
{
    public record UserClaim(
        string ClaimType,
        string HeaderKey
    );
}
