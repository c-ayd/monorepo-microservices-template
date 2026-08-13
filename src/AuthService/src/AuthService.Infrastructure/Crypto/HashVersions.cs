using System.Security.Cryptography;
using AuthService.Application.Abstractions.Crypto;
using Shared.Crypto.Options;

namespace AuthService.Infrastructure.Crypto
{
    public class HashVersions : IHashVersions
    {
        public byte CurrentHashVersion { get; private set; } = 1;

        public HashOptions GetCurrentHashOption()
        {
            return _versions[CurrentHashVersion];
        }

        public HashOptions GetHashOptions(byte version)
        {
            return _versions[version];
        }

        private static Dictionary<byte, HashOptions> _versions = new Dictionary<byte, HashOptions>()
        {
            { 1, new HashOptions(SHA256.Create, 32) }
        };
    }
}
