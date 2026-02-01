namespace Prakrishta.AseemblyScanner.Helper
{
    public class RegistrationSummary
    {
        public List<string> Assemblies { get; } = new();
        public List<string> InterfaceTypes { get; } = new();
        public List<string> Registered { get; } = new();
        public List<string> DecoratorsApplied { get; } = new();
        public List<string> Excluded { get; } = new();
    }
}
