namespace AuthService.Application.Abstractions.Crypto
{
    /// <summary>
    /// Provides methods to get available encryption keys.
    /// </summary>
    public interface IAesGcmEncryptionVersions
    {
        ushort CurrentVersion { get; }

        /// <summary>
        /// Gets the encryption key based on a given version.
        /// </summary>
        /// <param name="version">Version of the encryption</param>
        /// <returns>Returns the encryption key.</returns>
        string GetEncryptionKey(ushort version);
    }
}
