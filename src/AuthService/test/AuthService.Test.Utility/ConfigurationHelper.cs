using Microsoft.Extensions.Configuration;

namespace AuthService.Test.Utility
{
    public static class ConfigurationHelper
    {
        public static IConfiguration CreateConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.Test.json")
                .Build();
        }
    }
}
