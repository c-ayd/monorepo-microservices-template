namespace AuthService.Application.Dtos.Authentication
{
    public record JwtDto(
        string AccessToken,
        DateTimeOffset AccessTokenExpirationDate,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpirationDate
    );
}
