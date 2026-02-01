//----------------------------------------------------------------------------------
// <copyright file="AssemblyScanner.cs" company="Prakrishta Technologies">
//     Copyright (c) 2026 Prakrishta Technologies. All rights reserved.
// </copyright>
// <author>Arul Sengottaiyan</author>
// <date>01/30/2026</date>
// <summary>The class that helps to register interfaces and implementation in an assembly</summary>
//-----------------------------------------------------------------------------------

namespace Prakrishta.AseemblyScanner
{
    using Microsoft.Extensions.DependencyInjection;
    using Prakrishta.AseemblyScanner.Enum;
    using Prakrishta.AseemblyScanner.Extensions;
    using Prakrishta.AseemblyScanner.Helper;
    using Prakrishta.Infrastructure.Helper;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public sealed class AssemblyScanner(IServiceCollection services)
    {
        private readonly IServiceCollection _services = services;
        private readonly List<Assembly> _assemblies = new();
        private readonly List<Type> _interfaceTypes = new();
        private readonly List<Func<TypeInfo, bool>> _includePredicates = new();
        private readonly List<Func<TypeInfo, bool>> _excludePredicates = new();
        private List<Type> _pipelineDecorators = new();
        private ServiceLifetime _lifetime = ServiceLifetime.Scoped;
        private RegistrationStrategy _strategy = RegistrationStrategy.Append;
        private Action<string>? _log;
        private RegistrationSummary? _summary;

        public AssemblyScanner FromAssemblies(params Assembly[] assemblies)
        {
            _assemblies.AddRange(assemblies);
            return this;
        }

        public AssemblyScanner FromAssemblyOf<T>()
        {
            _assemblies.Add(typeof(T).Assembly);
            return this;
        }

        public AssemblyScanner AddClassesAssignableTo(Type interfaceType)
        {
            _interfaceTypes.Add(interfaceType);
            return this;
        }

        public AssemblyScanner Include(Func<TypeInfo, bool> predicate)
        {
            _includePredicates.Add(predicate);
            return this;
        }

        public AssemblyScanner Exclude(Func<TypeInfo, bool> predicate)
        {
            _excludePredicates.Add(predicate);
            return this;
        }

        public AssemblyScanner ExcludeNamespace(string ns)
        {
            _excludePredicates.Add(t => t.Namespace?.StartsWith(ns) == true);
            return this;
        }

        public AssemblyScanner WithNameConvention(params string[] tokens)
        {
            _includePredicates.Add(t => tokens.Any(tok => t.Name.Contains(tok)));
            return this;
        }

        public AssemblyScanner WithAttribute<TAttribute>() where TAttribute : Attribute
        {
            _includePredicates.Add(t => t.GetCustomAttribute<TAttribute>() != null);
            return this;
        }

        public AssemblyScanner WithBaseClass<TBase>()
        {
            var baseType = typeof(TBase);
            _includePredicates.Add(t => t.IsSubclassOf(baseType));
            return this;
        }

        public AssemblyScanner WithNamespace(string ns)
        {
            _includePredicates.Add(t => t.Namespace?.StartsWith(ns) == true);
            return this;
        }

        public AssemblyScanner Pipeline(Action<DecoratorPipelineBuilder> configure)
        {
            var builder = new DecoratorPipelineBuilder();
            configure(builder);

            foreach (var decorator in builder.Build())
                _pipelineDecorators.Add(decorator);

            return this;
        }

        public AssemblyScanner WithDiagnostics(Action<string> log)
        {
            _log = log;
            return this;
        }
        public AssemblyScanner WithStrategy(RegistrationStrategy strategy)
        {
            _strategy = strategy;
            return this;
        }

        public AssemblyScanner WithSummary(out RegistrationSummary summary)
        {
            _summary = new RegistrationSummary();
            summary = _summary;
            return this;
        }

        public AssemblyScanner AsScoped()
        {
            _lifetime = ServiceLifetime.Scoped;
            return this;
        }

        public AssemblyScanner AsSingleton()
        {
            _lifetime = ServiceLifetime.Singleton;
            return this;
        }

        public AssemblyScanner AsTransient()
        {
            _lifetime = ServiceLifetime.Transient;
            return this;
        }

        internal void Execute()
        {
            var pipeline = _pipelineDecorators.ToList();

            foreach (var assembly in _assemblies)
            {
                _summary?.Assemblies.Add(assembly.FullName!);
                Log($"Scanning assembly: {assembly.FullName}");

                foreach (var interfaceType in _interfaceTypes)
                {
                    _summary?.InterfaceTypes.Add(interfaceType.FullName!);
                    Log($"Scanning for interface: {interfaceType.FullName}");

                    var implementations = GetTypesAssignableTo(assembly, interfaceType);

                    if (_includePredicates.Count > 0)
                    {
                        implementations = [.. implementations.Where(t => _includePredicates.Any(p => p(t)))];
                    }

                    var excluded  = implementations.Where(t => _excludePredicates.Any(p => p(t))).ToList();

                    foreach (var e in implementations)
                    {
                        _summary?.Excluded.Add(e.Name);
                        Log($"Excluded {e.Name} by exclude predicate");
                    }

                    implementations = [.. implementations.Except(excluded)];

                    var implList = implementations.ToList();

                    foreach (var impl in implList)
                    {
                        foreach (var iface in impl.ImplementedInterfaces)
                        {
                            if (!MatchesInterface(iface, interfaceType))
                                continue;

                            var serviceType = iface.IsGenericType
                                ? iface.GetGenericTypeDefinition()
                                : iface;

                            Register(serviceType, impl.AsType());

                            _summary?.Registered.Add($"{serviceType.Name} → {impl.Name}");
                        }
                    }

                    if (pipeline.Any() && implList.Count > 1)
                        throw new InvalidOperationException($"Cannot apply pipeline to {interfaceType.Name} with multiple implementations.");

                    if (_pipelineDecorators.Any() 
                        && !interfaceType.IsGenericTypeDefinition 
                        && implList.Count == 1)
                    {
                        Decorate(interfaceType);
                    }

                }
            }
            _pipelineDecorators = new List<Type>();
        }

        private void Register(Type serviceType, Type implementationType)
        {
            var existing = _services.Where(s => s.ServiceType == serviceType).ToList();

            _summary?.Registered.Add($"{serviceType.Name} → {implementationType.Name}");

            switch (_strategy)
            {
                case RegistrationStrategy.Skip:
                    if (existing.Count != 0)
                    {
                        Log($"Skipping registration of {serviceType.Name} → {implementationType.Name} (already registered)");
                        return;
                    }
                    break;

                case RegistrationStrategy.Replace:
                    foreach (var e in existing)
                    {
                        Log($"Replacing existing registration {serviceType.Name} → {e.ImplementationType?.Name}");
                        _services.Remove(e);
                    }
                    break;

                case RegistrationStrategy.Append:
                default:
                    break;
            }

            Log($"Registering {serviceType.Name} → {implementationType.Name}");

            var descriptor = new ServiceDescriptor(serviceType, implementationType, _lifetime);
            _services.Add(descriptor);
        }

        private void Decorate(Type serviceType)
        {
            foreach (var decorator in _pipelineDecorators.AsEnumerable().Reverse<Type>())
            {
                _services.Decorate(serviceType, decorator);
                _summary?.DecoratorsApplied.Add($"{decorator.Name} → {serviceType.Name}");
                Log($"Applied decorator {decorator.Name} to {serviceType.Name}");
            }
        }

        private static bool MatchesInterface(Type iface, Type compareType)
        {
            if (compareType.IsGenericTypeDefinition)
            {
                return iface.IsGenericType &&
                       iface.GetGenericTypeDefinition() == compareType;
            }

            return iface == compareType;
        }

        private static IReadOnlyList<TypeInfo> GetTypesAssignableTo(Assembly assembly, Type compareType)
        {
            return [.. assembly.DefinedTypes
                                .Where(t =>
                                    t.IsClass &&
                                    !t.IsAbstract &&
                                    !t.Name.EndsWith("Decorator") &&
                                    t.ImplementedInterfaces.Any(i => MatchesInterface(i, compareType)))];
        }

        private void Log(string message)
        {
            _log?.Invoke(message);
        }
    }
}
