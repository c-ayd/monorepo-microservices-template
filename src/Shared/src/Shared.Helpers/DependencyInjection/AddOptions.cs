using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Shared.Helpers.Exceptions;
using Shared.Helpers.Options;

namespace Shared.Helpers.DependencyInjection
{
    public static class DependencyInjection
    {
        public static void AddOptionsFromAssembly(this IHostApplicationBuilder builder, Assembly assembly)
        {
            var options = assembly.GetTypes()
                .Where(t => t.IsAssignableTo(typeof(IOptions)))
                .ToList();
            
            RegisterOptions(builder, options);
        }

        public static void AddOptionsFromAssemblies(this IHostApplicationBuilder builder, params Assembly[] assemblies)
        {
            var options = new List<Type>();
            foreach (var assembly in assemblies)
            {
                options.AddRange(assembly.GetTypes()
                    .Where(t => t.IsAssignableTo(typeof(IOptions)))
                    .ToList());
            }

            RegisterOptions(builder, options);
        }

        private static void RegisterOptions(IHostApplicationBuilder builder, List<Type> options)
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

                configureMethod.Invoke(null, [builder.Services, builder.Configuration.GetSection(key)]);
            }
        }
    }
}
