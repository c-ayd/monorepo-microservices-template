using AuthService.Test.Utility.Fixtures;

namespace AuthService.Test.Integration.Api.Collections
{
    [CollectionDefinition(nameof(AuthApiCollection))]
    public class AuthApiCollection : ICollectionFixture<AuthApiFixture>
    {
    }
}
