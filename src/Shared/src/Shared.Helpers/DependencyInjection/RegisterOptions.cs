using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Shared.Helpers.Exceptions;
using Shared.Helpers.Options;
using Microsoft.Extensions.Configuration;

namespace Shared.Helpers.DependencyInjection
{
    public static class DependencyInjection
    {
        public static void RegisterOptionsFromAssembly(this IHostApplicationBuilder builder, Assembly assembly)
        {
            RegisterOptions(builder.Services, builder.Configuration, GetOptionsFromAssembly(assembly));
        }

        public static void RegisterOptionsFromAssemblies(this IHostApplicationBuilder builder, params Assembly[] assemblies)
        {
            var options = new List<Type>();
            foreach (var assembly in assemblies)
            {
                options.AddRange(GetOptionsFromAssembly(assembly));
            }

            RegisterOptions(builder.Services, builder.Configuration, options);
        }

        private static List<Type> GetOptionsFromAssembly(Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(t => t.IsAssignableTo(typeof(IOptions)))
                .ToList();
        }
        
        private static void RegisterOptions(IServiceCollection services, IConfiguration configuration, List<Type> options)
        {
            // builder.Services.Configure<MyOptionsClass>(builder.Configuration.GetSection(MyOptionsClass.Key));

            foreach (var type in options)
            {
                var configureMethod = typeof(OptionsConfigurationServiceCollectionExtensions).GetMethods()
                    .FirstOrDefault(m => m.Name == nameof(OptionsConfigurationServiceCollectionExtensions.Configure))!
                    .MakeGenericMethod(type);

                var key = (string?)type.GetProperty(nameof(IOptions.Key), BindingFlags.Public | BindingFlags.Static)!.GetValue(null);
                if (key == null)
                    throw new OptionsKeyIsNullException(type.Name);

                configureMethod.Invoke(null, [services, configuration.GetSection(key)]);
            }
        }
    }
}
