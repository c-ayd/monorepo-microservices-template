using Shared.Crypto.Options;

namespace AuthService.Application.Abstractions.Crypto
{
    public interface IHashVersions
    {
        byte CurrentHashVersion { get; }

        HashOptions GetCurrentHashOption();
        HashOptions GetHashOptions(byte version);
    }
}
