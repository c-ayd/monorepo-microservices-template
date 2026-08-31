using AuthService.Application.Abstractions.Crypto;
using Microsoft.AspNetCore.DataProtection;

namespace AuthService.Infrastructure.Crypto
{
    public class DataProtectionService : IDataProtectionService
    {
        public IDataProtector CookieProtector { get; private set; }

        public DataProtectionService(IDataProtectionProvider dataProtection)
        {
            CookieProtector = dataProtection.CreateProtector("AuthService-Cookies-v1");
        }

        public string Protect(IDataProtector protector, string value)
        {
            return protector.Protect(value);
        }

        public string Unprotect(IDataProtector protector, string protectedValue)
        {
            return protector.Unprotect(protectedValue);
        }
    }
}
