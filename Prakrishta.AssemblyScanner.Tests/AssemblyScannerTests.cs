namespace Prakrishta.AssemblyScanner.Tests
{
    using FluentAssertions;
    using Microsoft.Extensions.DependencyInjection;
    using Prakrishta.AseemblyScanner.Extensions;
    using Prakrishta.AssemblyScanner.Test;
    using Prakrishta.AssemblyScanner.Test.TestServices;
    using Prakrishta.AssemblyScanner.Test.TestServices.Sub;
    using Prakrishta.AssemblyScanner.Test.TestServices.SubNamespace;
    using System.Reflection;

    [TestClass]
    public class AssemblyScannerTests_Phase1
    {
        private Assembly _assembly;

        [TestInitialize]
        public void Init()
        {
            _assembly = typeof(TestService).Assembly;
        }

        // ---------------------------------------------------------
        // 1. Closed interface registration
        // ---------------------------------------------------------
        [TestMethod]
        public void Should_Register_All_Closed_Interface_Implementations()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(ITestService))
                .AsScoped());

            var provider = services.BuildServiceProvider();

            var all = provider.GetServices<ITestService>().ToList();

            all.Should().ContainSingle(s => s is TestService);
            all.Should().ContainSingle(s => s is AnotherTestService);
        }

        // ---------------------------------------------------------
        // 2. Open generic registration
        // ---------------------------------------------------------
        [TestMethod]
        public void Should_Register_Open_Generic_Implementations()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(IGenericService<>))
                .AsScoped());

            var provider = services.BuildServiceProvider();

            var instance = provider.GetService<IGenericService<int>>();

            instance.Should().BeOfType<GenericService<int>>();
        }

        // ---------------------------------------------------------
        // 3. Multiple assemblies scanning
        // ---------------------------------------------------------
        [TestMethod]
        public void Should_Scan_Multiple_Assemblies()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly, typeof(MultiAssemblyService).Assembly)
                .AddClassesAssignableTo(typeof(IMultiAssemblyService))
                .AsScoped());

            var provider = services.BuildServiceProvider();

            var instance = provider.GetService<IMultiAssemblyService>();

            instance.Should().BeOfType<MultiAssemblyService>();
        }

        // ---------------------------------------------------------
        // 4. Singleton lifetime
        // ---------------------------------------------------------
        [TestMethod]
        public void Should_Register_As_Singleton()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(ITestService))
                .AsSingleton());

            var provider = services.BuildServiceProvider();

            var s1 = provider.GetService<ITestService>();
            var s2 = provider.GetService<ITestService>();

            s1.Should().BeSameAs(s2);
        }

        // ---------------------------------------------------------
        // 5. Transient lifetime
        // ---------------------------------------------------------
        [TestMethod]
        public void Should_Register_As_Transient()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(ITestService))
                .AsTransient());

            var provider = services.BuildServiceProvider();

            var s1 = provider.GetService<ITestService>();
            var s2 = provider.GetService<ITestService>();

            s1.Should().NotBeSameAs(s2);
        }

        // ---------------------------------------------------------
        // 6. Scoped lifetime
        // ---------------------------------------------------------
        [TestMethod]
        public void Should_Register_As_Scoped()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(ITestService))
                .AsScoped());

            var provider = services.BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var s1 = scope1.ServiceProvider.GetService<ITestService>();
            var s2 = scope1.ServiceProvider.GetService<ITestService>();
            var s3 = scope2.ServiceProvider.GetService<ITestService>();

            s1.Should().BeSameAs(s2);   // same scope
            s1.Should().NotBeSameAs(s3); // different scope
        }

        // ---------------------------------------------------------
        // 7. No matching implementations
        // ---------------------------------------------------------
        [TestMethod]
        public void Should_Not_Register_When_No_Implementations_Found()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(IDisposable)) // no classes implement IDisposable
                .AsScoped());

            var provider = services.BuildServiceProvider();

            var result = provider.GetServices<IDisposable>();

            result.Should().BeEmpty();
        }

        // ---------------------------------------------------------
        // 1. Include filter
        // ---------------------------------------------------------
        [TestMethod]
        public void Should_Include_Only_Types_Matching_Predicate()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(ITestService))
                .Include(t => t.Name == nameof(TestService))
                .AsScoped());

            var provider = services.BuildServiceProvider();

            var all = provider.GetServices<ITestService>().ToList();

            all.Should().ContainSingle(s => s is TestService);
            all.Should().NotContain(s => s is AnotherTestService);
            all.Should().NotContain(s => s is SubNamespaceService);
        }

        // ---------------------------------------------------------
        // 2. Exclude filter
        // ---------------------------------------------------------
        [TestMethod]
        public void Should_Exclude_Types_Matching_Predicate()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(ITestService))
                .Exclude(t => t.Name.Contains("Another"))
                .AsScoped());

            var provider = services.BuildServiceProvider();

            var all = provider.GetServices<ITestService>().ToList();

            all.Should().ContainSingle(s => s is TestService);
            all.Should().NotContain(s => s is AnotherTestService);
        }

        // ---------------------------------------------------------
        // 3. Exclude namespace
        // ---------------------------------------------------------
        [TestMethod]
        public void Should_Exclude_Namespace()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(ITestService))
                .ExcludeNamespace("Prakrishta.Infrastructure.Test.TestServices.Sub")
                .AsScoped());

            var provider = services.BuildServiceProvider();

            var all = provider.GetServices<ITestService>().ToList();

            all.Should().NotContain(s => s is SubNamespaceService);
        }

        // ---------------------------------------------------------
        // 4. Include + Exclude combined
        // ---------------------------------------------------------
        [TestMethod]
        public void Should_Apply_Include_Then_Exclude()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(ITestService))
                .Include(t => t.Name.Contains("Service"))
                .Exclude(t => t.Name.StartsWith("Another"))
                .AsScoped());

            var provider = services.BuildServiceProvider();

            var all = provider.GetServices<ITestService>().ToList();

            all.Should().ContainSingle(s => s is TestService);
            all.Should().NotContain(s => s is AnotherTestService);
        }

        // ---------------------------------------------------------
        // 5. Include removes all types
        // ---------------------------------------------------------
        [TestMethod]
        public void Should_Register_No_Types_When_Include_Filters_All()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(ITestService))
                .Include(t => t.Name == "DoesNotExist")
                .AsScoped());

            var provider = services.BuildServiceProvider();

            var all = provider.GetServices<ITestService>();

            all.Should().BeEmpty();
        }

        // ---------------------------------------------------------
        // 1. Name convention
        // ---------------------------------------------------------
        [TestMethod]
        public void Should_Include_Types_By_Name_Convention()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(ITestService))
                .WithNameConvention("NameConvention")
                .AsScoped());

            var provider = services.BuildServiceProvider();

            var all = provider.GetServices<ITestService>().ToList();

            all.Should().ContainSingle(s => s is NameConventionService);
        }

        // ---------------------------------------------------------
        // 2. Attribute convention
        // ---------------------------------------------------------
        [TestMethod]
        public void Should_Include_Types_With_Attribute()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(ITestService))
                .WithAttribute<AutoRegisterAttribute>()
                .AsScoped());

            var provider = services.BuildServiceProvider();

            var all = provider.GetServices<ITestService>().ToList();

            all.Should().ContainSingle(s => s is AttributeBasedService);
        }

        // ---------------------------------------------------------
        // 3. Base class convention
        // ---------------------------------------------------------
        [TestMethod]
        public void Should_Include_Types_By_Base_Class()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(ITestService))
                .WithBaseClass<BaseProcessor>()
                .AsScoped());

            var provider = services.BuildServiceProvider();

            var all = provider.GetServices<ITestService>().ToList();

            all.Should().ContainSingle(s => s is DerivedProcessor);
        }

        // ---------------------------------------------------------
        // 4. Namespace convention
        // ---------------------------------------------------------
        [TestMethod]
        public void Should_Include_Types_By_Namespace()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(ITestService))
                .WithNamespace("Prakrishta.Infrastructure.Test.TestServices.SubNamespace")
                .AsScoped());

            var provider = services.BuildServiceProvider();

            var all = provider.GetServices<ITestService>().ToList();

            all.Should().ContainSingle(s => s is NamespaceBasedService);
        }

        // ---------------------------------------------------------
        // 5. Combined conventions
        // ---------------------------------------------------------
        [TestMethod]
        public void Should_Combine_Conventions_With_Include_Exclude()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(ITestService))
                .WithNameConvention("Service")
                .Exclude(t => t.Name.StartsWith("Another"))
                .AsScoped());

            var provider = services.BuildServiceProvider();

            var all = provider.GetServices<ITestService>().ToList();

            all.Should().Contain(s => s is TestService);
            all.Should().NotContain(s => s is AnotherTestService);
        }

        [TestMethod]
        public void Should_Apply_Single_Decorator_Pipeline_For_Non_Generic_Service()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(ITestService))
                .Include(t => t.Name == nameof(TestService))
                .Pipeline(p => p
                    .Use(typeof(LoggingDecorator)))
                .AsScoped());

            var provider = services.BuildServiceProvider();

            var instance = provider.GetService<ITestService>();

            instance.Should().BeOfType<LoggingDecorator>();
            ((LoggingDecorator)instance!).Inner.Should().BeOfType<TestService>();
        }

        [TestMethod]
        public void Should_Apply_Decorator_Pipeline_In_Order_For_Non_Generic_Service()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(ITestService))
                .Include(t => t.Name == nameof(TestService))
                .Pipeline(p => p
                    .Use(typeof(ValidationDecorator))
                    .Use(typeof(LoggingDecorator)))
                .AsScoped());

            var provider = services.BuildServiceProvider();

            var instance = provider.GetService<ITestService>();

            instance.Should().BeOfType<ValidationDecorator>();

            var logging = ((ValidationDecorator)instance!).Inner;
            logging.Should().BeOfType<LoggingDecorator>();

            var inner = ((LoggingDecorator)logging).Inner;
            inner.Should().BeOfType<TestService>();
        }

        [TestMethod]
        public void Should_Not_Apply_Pipeline_To_Open_Generic_Services()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(IGenericService<>))
                .Pipeline(p => p
                    .Use(typeof(LoggingDecorator))) // should be ignored for generics
                .AsScoped());

            var provider = services.BuildServiceProvider();

            var instance = provider.GetService<IGenericService<int>>();

            instance.Should().BeOfType<GenericService<int>>();
        }

        [TestMethod]
        public void Should_Preserve_Lifetime_For_Decorated_Singleton()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(ITestService))
                .Include(t => t.Name == nameof(TestService))
                .Pipeline(p => p
                    .Use(typeof(LoggingDecorator)))
                .AsSingleton());

            var provider = services.BuildServiceProvider();

            var s1 = provider.GetService<ITestService>();
            var s2 = provider.GetService<ITestService>();

            s1.Should().BeSameAs(s2);
        }

        [TestMethod]
        public void Should_Preserve_Lifetime_For_Decorated_Transient()
        {
            var services = new ServiceCollection();

            services.Scan(s => s
                .FromAssemblies(_assembly)
                .AddClassesAssignableTo(typeof(ITestService))
                .Include(t => t.Name == nameof(TestService))
                .Pipeline(p => p
                    .Use(typeof(LoggingDecorator)))
                .AsTransient());

            var provider = services.BuildServiceProvider();

            var s1 = provider.GetService<ITestService>();
            var s2 = provider.GetService<ITestService>();

            s1.Should().NotBeSameAs(s2);
        }
    }
}