# Prakrishta.AssemblyScanner
A lightweight, fluent, production‑grade assembly scanning library for .NET. Designed for clean architecture, developer experience, and extensibility.

It supports:

- Multi‑assembly scanning
- Open/closed generic interface registration
- Include/exclude filtering
- Naming, attribute, base‑class, and namespace conventions
- Ordered decorator pipelines (non‑generic)
- Diagnostics logging
- Registration strategies (Append, Skip, Replace)
- Summary reporting for debugging and CI visibility

✨ Features

✔ Fluent API

```
services.Scan(s => s
    .FromAssemblies(assembly1, assembly2)
    .AddClassesAssignableTo(typeof(IHandler<>))
    .AsScoped());
```

✔ Multi‑assembly scanning

Scan any number of assemblies in a single fluent chain.

✔ Open generic support

```
services.Scan(s => s
    .FromAssemblyOf<Program>()
    .AddClassesAssignableTo(typeof(IRepository<>))
    .AsScoped());
```

✔ Include / Exclude filtering

```
services.Scan(s => s
    .FromAssemblies(assembly)
    .AddClassesAssignableTo(typeof(IService))
    .Include(t => t.Name.EndsWith("Service"))
    .Exclude(t => t.Name.Contains("Legacy")));
```

✔ Conventions
```
services.Scan(s => s
    .FromAssemblies(assembly)
    .AddClassesAssignableTo(typeof(IValidator<>))
    .WithNameConvention("Validator")
    .WithAttribute<AutoRegisterAttribute>()
    .WithBaseClass<BaseProcessor>()
    .WithNamespace("MyApp.Services"));
```

✔ Decorator Pipelines (non‑generic)
```
services.Scan(s => s
    .FromAssemblies(assembly)
    .AddClassesAssignableTo(typeof(ICommandHandler))
    .Pipeline(p => p
        .Use(typeof(ValidationDecorator))
        .Use(typeof(LoggingDecorator)))
    .AsScoped());
```

✔ Diagnostics

```
services.Scan(s => s
    .FromAssemblies(assembly)
    .AddClassesAssignableTo(typeof(IService))
    .WithDiagnostics(Console.WriteLine));
```

✔ Registration Strategy

```
services.Scan(s => s
    .FromAssemblies(assembly)
    .AddClassesAssignableTo(typeof(IService))
    .WithStrategy(RegistrationStrategy.Replace));
```

✔ Summary Reporting

```
services.Scan(s => s
    .FromAssemblies(assembly)
    .AddClassesAssignableTo(typeof(IService))
    .WithSummary(out var summary));
```

📦 Installation
```
dotnet add package Prakrishta.AssemblyScanner
```

🚀 Quick Start

```
services.Scan(s => s
    .FromAssemblies(typeof(Program).Assembly)
    .AddClassesAssignableTo(typeof(IMyService))
    .AsScoped());
```


🧠 Decorator Pipelines

Pipelines wrap the resolved service in the order defined:

```
.Pipeline(p => p
    .Use(typeof(ValidationDecorator))
    .Use(typeof(LoggingDecorator)))
```

Produces:

```
ValidationDecorator
    → LoggingDecorator
        → ConcreteService
```

Important:

Decorators must not be auto‑registered as services.

Use naming conventions (e.g., *Decorator) to exclude them.

🧹 Filtering

Include only types matching a predicate:

```
.Include(t => t.Name.EndsWith("Handler"))
```

Exclude types:

```
.Exclude(t => t.Name.Contains("Legacy"))
```

Exclude namespace:

```
.ExcludeNamespace("MyApp.Legacy")
```

📏 Conventions

Name convention

```
.WithNameConvention("Service", "Handler")
```

Attribute convention
.WithAttribute<AutoRegisterAttribute>()


Base class convention

```
.WithBaseClass<BaseProcessor>()
```

Namespace convention

```
.WithNamespace("MyApp.Services")
```


🛡 Validation Rules

The scanner enforces:

- Decorators must implement the service interface
- Decorators must have a constructor accepting the service type
- Pipelines cannot be applied when multiple implementations exist
- Pipelines cannot be applied to open generics
  
These rules prevent circular dependencies and invalid registrations.

📊 Summary Reporting

```
services.Scan(s => s
    .FromAssemblies(assembly)
    .AddClassesAssignableTo(typeof(IService))
    .WithSummary(out var summary));

Console.WriteLine("Registered:");
foreach (var r in summary.Registered)
    Console.WriteLine($" - {r}");
```

Summary includes:

- Assemblies scanned
- Interface types scanned
- Registered mappings
- Decorators applied
- Excluded types

🧪 Testing

The library includes a full MSTest suite covering:

- Multi‑assembly scanning
- Open/closed generics
- Include/exclude filters
- Conventions
- Decorator pipelines
- Registration strategies
- Diagnostics
- Summary reporting

📄 License

MIT License.
