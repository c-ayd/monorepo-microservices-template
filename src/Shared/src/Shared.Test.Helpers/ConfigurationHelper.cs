using Microsoft.Extensions.Configuration;

namespace Shared.Test.Helpers
{
    /// <summary>
    /// Provides methods to create configuration from JSON files
    /// </summary>
    public static class ConfigurationHelper
    {
        /// <summary>
        /// Creates configuration from 'appsettings.Test.json' file.
        /// </summary>
        /// <returns>Returns created configuration file.</returns>
        public static IConfiguration CreateConfigurationFromTestSettings()
        {
            return CreateConfigurationFromFile("appsettings.Test.json");
        }

        /// <summary>
        /// Creates configuration from a given JSON file.
        /// </summary>
        /// <param name="fileName">Name of the JSON file</param>
        /// <returns>Returns created configuration file.</returns>
        public static IConfiguration CreateConfigurationFromFile(string fileName)
        {
            return new ConfigurationBuilder()
                .AddJsonFile(fileName)
                .Build();
        }
    }
}
