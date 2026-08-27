using AuthService.Test.Utility.Fixtures;

namespace AuthService.Test.Integration.Application.Collections
{
    [CollectionDefinition(nameof(AuthApiCollection))]
    public class AuthApiCollection : ICollectionFixture<AuthApiFixture>
    {
    }
}
