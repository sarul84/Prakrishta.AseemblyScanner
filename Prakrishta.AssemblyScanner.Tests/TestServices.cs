namespace Prakrishta.AssemblyScanner.Test
{
    public interface ITestService { }
    public class TestService : ITestService { }

    public class AnotherTestService : ITestService { }

    public interface IGenericService<T> { }
    public class GenericService<T> : IGenericService<T> { }

    // Used for multi-assembly scanning test
    public interface IMultiAssemblyService { }
    public class MultiAssemblyService : IMultiAssemblyService { }
}

namespace Prakrishta.AssemblyScanner.Test.TestServices.Sub
{
    public class SubNamespaceService : ITestService { }
}

namespace Prakrishta.AssemblyScanner.Test.TestServices
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoRegisterAttribute : Attribute { }

    public abstract class BaseProcessor { }
    public class DerivedProcessor : BaseProcessor, ITestService { }

    [AutoRegister]
    public class AttributeBasedService : ITestService { }

    public class NameConventionService : ITestService { }

    public class LoggingDecorator : ITestService
    {
        public ITestService Inner { get; }

        public LoggingDecorator(ITestService inner) => Inner = inner;
    }

    public class ValidationDecorator(ITestService inner) : ITestService
    {
        public ITestService Inner { get; } = inner;
    }

    namespace SubNamespace
    {
        public class NamespaceBasedService : ITestService { }
    }
}