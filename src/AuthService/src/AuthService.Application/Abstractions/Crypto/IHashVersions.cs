using Shared.Crypto.Options;

namespace AuthService.Application.Abstractions.Crypto
{
    /// <summary>
    /// Provides methods to get available hash versions.
    /// </summary>
    public interface IHashVersions
    {
        byte CurrentHashVersion { get; }

        /// <summary>
        /// Gets the hash options based on <see cref="IHashVersions.CurrentHashVersion"/>.
        /// </summary>
        /// <returns>Returns the current hash options.</returns>
        HashOptions GetCurrentHashOptions();
        /// <summary>
        /// Gets the hash options based on a given version.
        /// </summary>
        /// <param name="version">Version of the hash options</param>
        /// <returns>Returns the hash options.</returns>
        HashOptions GetHashOptions(byte version);
    }
}
