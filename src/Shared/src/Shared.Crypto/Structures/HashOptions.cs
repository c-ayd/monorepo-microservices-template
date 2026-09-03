using System.Security.Cryptography;

namespace Shared.Crypto.Structures
{
    public record HashOptions(
        Func<HashAlgorithm> Algorithm,
        int SaltSize
    );
}
