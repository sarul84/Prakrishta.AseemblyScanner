
namespace Prakrishta.AseemblyScanner.Extensions
{
    using Microsoft.Extensions.DependencyInjection;
    using Prakrishta.AseemblyScanner;

    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Scans the specified assemblies for types to register with the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection to which discovered services will be registered.</param>
        /// <param name="action">The assemblies to scan for service types. At least one assembly must be specified.</param>
        /// <returns>An AssemblyScanner instance that can be used to further configure the scanning and registration process.</returns>
        public static IServiceCollection Scan(this IServiceCollection services, Action<AssemblyScanner> action)
        {
            var scanner = new AssemblyScanner(services);
            action(scanner);
            scanner.Execute();
            return services;
        }

        /// <summary>
        /// Decorates all registrations of the specified service type in the service collection with the given decorator
        /// type.
        /// </summary>
        /// <remarks>The decorator type must have a constructor that accepts an instance of the service
        /// type being decorated. This method replaces all existing registrations of the specified service type with
        /// decorated versions, preserving their original lifetimes. If multiple registrations exist for the service
        /// type, each will be decorated individually.</remarks>
        /// <param name="services">The service collection to modify. Cannot be null.</param>
        /// <param name="serviceType">The type of the service to decorate. All registrations matching this type will be wrapped by the decorator.
        /// Cannot be null.</param>
        /// <param name="decoratorType">The type of the decorator to apply. Must have a constructor that accepts the decorated service type as a
        /// parameter. Cannot be null.</param>
        /// <returns>The same IServiceCollection instance, with the specified service type registrations decorated by the given
        /// decorator type.</returns>
        public static IServiceCollection Decorate(this IServiceCollection services, Type serviceType, Type decoratorType)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(serviceType);
            ArgumentNullException.ThrowIfNull(decoratorType);

            if (serviceType.IsGenericTypeDefinition || decoratorType.IsGenericTypeDefinition)
                throw new InvalidOperationException("Decorator pipelines are supported only for non-generic services.");

            var descriptors = services
                .Where(d => d.ServiceType == serviceType)
                .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);

                Func<IServiceProvider, object> innerFactory;

                if (descriptor.ImplementationFactory != null)
                {
                    innerFactory = descriptor.ImplementationFactory;
                }
                else if (descriptor.ImplementationInstance != null)
                {
                    var instance = descriptor.ImplementationInstance;
                    innerFactory = _ => instance;
                }
                else
                {
                    var implType = descriptor.ImplementationType!;
                    innerFactory = sp => ActivatorUtilities.CreateInstance(sp, implType);
                }

                var newDescriptor = ServiceDescriptor.Describe(
                    descriptor.ServiceType,
                    sp =>
                    {
                        var inner = innerFactory(sp);
                        return CreateDecoratorInstance(sp, decoratorType, inner);
                    },
                    descriptor.Lifetime);

                services.Add(newDescriptor);
            }

            return services;
        }

        private static object CreateDecoratorInstance(IServiceProvider provider, Type decoratorType, object inner)
        {
            var actualDecoratorType = decoratorType;

            var ctor = actualDecoratorType
                .GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault()
                ?? throw new InvalidOperationException($"No public constructor found for decorator type {actualDecoratorType}.");

            var parameters = ctor.GetParameters();
            var args = new object?[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];

                if (p.ParameterType.IsInstanceOfType(inner) ||
                    p.ParameterType.IsAssignableFrom(inner.GetType()))
                {
                    args[i] = inner;
                }
                else
                {
                    args[i] = provider.GetRequiredService(p.ParameterType);
                }
            }

            return Activator.CreateInstance(actualDecoratorType, args)!;
        }
    }
}
