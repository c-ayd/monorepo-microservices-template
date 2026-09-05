using AuthService.Application.Abstractions.Crypto;
using AuthService.Application.Options;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.Crypto
{
    public class AesGcmEncryptionVersions : IAesGcmEncryptionVersions
    {
        public ushort CurrentVersion { get; private set; } = 1;

        private readonly Dictionary<ushort, string> _encryptionKeys;

        public AesGcmEncryptionVersions(IOptions<EncryptionKeysOptions> encryptionKeysOptions)
        {
            _encryptionKeys = encryptionKeysOptions.Value.AesGcm
                .ToDictionary(p => p.Version, p => p.Key);
        }

        public string GetEncryptionKey(ushort version)
        {
            return _encryptionKeys[version];
        }
    }
}
