namespace AuthService.Application.Features.AccountEndpoints.Register
{
    public record RegisterRequest(
        string? Email,
        string? Password
    );
}
