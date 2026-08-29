namespace AuthService.Application.Features.AccountEndpoints.Login
{
    public record LoginRequest(
        string? Email,
        string? Password
    );
}
