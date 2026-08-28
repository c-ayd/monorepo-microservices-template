using AuthService.Application.Dtos.Crypto;

namespace AuthService.Application.Abstractions.Crypto
{
    /// <summary>
    /// Provides methods to hash and verify passwords.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Hashes a password using PBKDF2.
        /// </summary>
        /// <param name="password">Plain password value to be hashed</param>
        /// <returns>Returns the hashed password.</returns>
        string Hash(string password);

        /// <summary>
        /// Verifies if two passwords match.
        /// </summary>
        /// <param name="passwordHashed">Hashed password to verify against</param>
        /// <param name="passwordPlain">Plain password to check</param>
        /// <param name="version">Version of the hashed password</param>
        /// <returns>Returns the verification result.</returns>
        EPasswordVerificationResult Verify(string passwordHashed, string passwordPlain, out byte version);
    }
}
