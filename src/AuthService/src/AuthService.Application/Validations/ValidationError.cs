namespace AuthService.Application.Validations
{
    public record ValidationError(
        string? Message = null,
        string? Code = null,
        object? Metadata = null
    );
}
