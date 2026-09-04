using System.Security.Cryptography;
using System.Text;
using Shared.Crypto.Exceptions;

namespace Shared.Crypto
{
    /// <summary>
    /// Encrypts and decrypts values using AES-256-GCM.
    /// </summary>
    public static class AesGcmEncryption
    {
        private const int _maxStackSize = 1024;

        private const int _versionSize = sizeof(ushort);
        private const int _keySize = 32;
        private const int _nonceSize = 12;
        private const int _tagSize = 16;

        /// <summary>
        /// Encrypts a given value.
        /// </summary>
        /// <param name="value">Value to encrypt</param>
        /// <param name="keyVersion">Version of the encryption key, which is used for decryption</param>
        /// <param name="key">Encryption key</param>
        /// <returns>Returns the encrypted value.</returns>
        public static string Encrypt(string value, ushort keyVersion, Func<ushort, string> key)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("The value cannot be null or empty.", nameof(value));

            var bytes = Encrypt(Encoding.UTF8.GetBytes(value).AsSpan(), keyVersion, key);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Encrypts a given value.
        /// </summary>
        /// <param name="value">Value to encrypt</param>
        /// <param name="keyVersion">Version of the encryption key, which is used for decryption</param>
        /// <param name="key">Encryption key</param>
        /// <returns>Returns the encrypted value.</returns>
        public static byte[] Encrypt(ReadOnlySpan<byte> value, ushort keyVersion, Func<ushort, string> key)
        {
            // The given key must be a specific length for AES-256-GCM.
            var keyValue = key(keyVersion);
            if (string.IsNullOrEmpty(keyValue))
                throw new ArgumentException("The value cannot be null or empty.", nameof(keyValue));

            var keyBytes = Convert.FromBase64String(keyValue).AsSpan();
            if (keyBytes.Length != _keySize)
                throw new InvalidEncryptionKeySizeException(_keySize, keyBytes.Length);

            // Encrypted value bytes are: {Version}{Nonce}{Tag}{Cipher}
            // Version bytes are 16 bits (2 bytes)
            var encryptedDataLength = _versionSize + _nonceSize + _tagSize + value.Length;
            Span<byte> bytes = encryptedDataLength <= _maxStackSize ?
                stackalloc byte[encryptedDataLength] :
                new byte[encryptedDataLength];

            var versionBytes = bytes.Slice(0, _versionSize);
            var nonceBytes = bytes.Slice(_versionSize, _nonceSize);
            var tagBytes = bytes.Slice(_versionSize + _nonceSize, _tagSize);
            var cipherBytes = bytes.Slice(_versionSize + _nonceSize + _tagSize, value.Length);

            // Fill the bytes
            versionBytes[0] = (byte)(keyVersion >> 8);
            versionBytes[1] = (byte)(keyVersion & 0xFF);
            RandomNumberGenerator.Fill(nonceBytes);

            using var aesGcm = new AesGcm(keyBytes, _tagSize);
            aesGcm.Encrypt(nonceBytes, value, cipherBytes, tagBytes);

            return bytes.ToArray();
        }

        /// <summary>
        /// Decrypts a given value.
        /// </summary>
        /// <param name="valueEncrypted">Encrypted value to decrypt</param>
        /// <param name="key">Delegate that returns the correct key based on the encrypted value's version</param>
        /// <param name="version">Version of the encrypted value</param>
        /// <returns>Returns the decrypted value.</returns>
        public static string Decrypt(string valueEncrypted, Func<ushort, string> key, out ushort version)
        {
            if (string.IsNullOrEmpty(valueEncrypted))
                throw new ArgumentException("The encrypted value cannot be null or empty.", nameof(valueEncrypted));

            var decryptedBytes = Decrypt(Convert.FromBase64String(valueEncrypted).AsSpan(), key, out version);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        public static byte[] Decrypt(ReadOnlySpan<byte> valueEncrypted, Func<ushort, string> key, out ushort version)
        {
            // Encrypted value bytes are: {Version}{Nonce}{Tag}{Cipher}
            // Version bytes are 16 bits (2 bytes)
            var versionBytes = valueEncrypted.Slice(0, _versionSize);
            version = (ushort)(versionBytes[0] << 8 | versionBytes[1]);
            var keyValue = key(version);

            // The given key must be a specific length for AES-256-GCM.
            var keyBytes = Convert.FromBase64String(keyValue).AsSpan();
            if (keyBytes.Length != _keySize)
                throw new InvalidEncryptionKeySizeException(_keySize, keyBytes.Length);

            var cipherSize = valueEncrypted.Length - (_versionSize + _nonceSize + _tagSize);

            var nonceBytes = valueEncrypted.Slice(_versionSize, _nonceSize);
            var tagBytes = valueEncrypted.Slice(_versionSize + _nonceSize, _tagSize);
            var cipherBytes = valueEncrypted.Slice(_versionSize + _nonceSize + _tagSize, cipherSize);

            // Decrypt
            Span<byte> decryptedBytes = cipherSize <= _maxStackSize ?
                stackalloc byte[cipherSize] :
                new byte[cipherSize];

            using var aesGcm = new AesGcm(keyBytes, _tagSize);
            aesGcm.Decrypt(nonceBytes, cipherBytes, tagBytes, decryptedBytes);

            return decryptedBytes.ToArray();
        }

        /// <summary>
        /// Compares an encrypted value against a plain value.
        /// </summary>
        /// <param name="valueEncrypted">Encrypted value to check</param>
        /// <param name="valuePlain">Plain value to compare against</param>
        /// <param name="key">Delegate that returns the correct key based on the encrypted value's version</param>
        /// <param name="version">Version of the encrypted value</param>
        /// <returns>Returns the comparison result.</returns>
        public static bool Compare(string valueEncrypted, string valuePlain, Func<ushort, string> key, out ushort version)
        {
            if (string.IsNullOrEmpty(valueEncrypted))
                throw new ArgumentException($"The encrypted value cannot be null or empty.", nameof(valueEncrypted));
            if (string.IsNullOrEmpty(valuePlain))
                throw new ArgumentException("The plain value cannot be null or empty.", nameof(valuePlain));

            return Compare(Convert.FromBase64String(valueEncrypted).AsSpan(), Encoding.UTF8.GetBytes(valuePlain).AsSpan(), key, out version);
        }

        public static bool Compare(ReadOnlySpan<byte> valueEncrypted, ReadOnlySpan<byte> valuePlain, Func<ushort, string> key, out ushort version)
        {
            var valueDecrypted = Decrypt(valueEncrypted, key, out version);

            // To prevent timing attack, the comparison is made over CryptographicOperations
            return CryptographicOperations.FixedTimeEquals(valueDecrypted, valuePlain);
        }
    }
}
