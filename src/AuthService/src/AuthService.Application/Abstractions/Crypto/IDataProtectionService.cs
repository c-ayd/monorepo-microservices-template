using Microsoft.AspNetCore.DataProtection;

namespace AuthService.Application.Abstractions.Crypto
{
    /// <summary>
    /// Provides methods to protect values 
    /// </summary>
    public interface IDataProtectionService
    {
        IDataProtector CookieProtector { get; }

        string Protect(IDataProtector protector, string value);
        string Unprotect(IDataProtector protector, string protectedValue);
    }
}
