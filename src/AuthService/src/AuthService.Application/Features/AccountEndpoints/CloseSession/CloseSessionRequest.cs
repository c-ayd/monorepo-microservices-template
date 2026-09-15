namespace AuthService.Application.Features.AccountEndpoints.CloseSession
{
    public record CloseSessionRequest(
        string? Password
    );
}
