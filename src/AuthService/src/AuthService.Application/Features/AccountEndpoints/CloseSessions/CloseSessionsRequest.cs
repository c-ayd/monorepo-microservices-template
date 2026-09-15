namespace AuthService.Application.Features.AccountEndpoints.CloseSessions
{
    public record CloseSessionsRequest(
        string? Password
    );
}
