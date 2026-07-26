namespace mediconyx_api.Middleware;

/// <summary>Thrown when a request hits a feature module the tenant has disabled. Mapped to 403.</summary>
public class ModuleDisabledException : Exception
{
    public ModuleDisabledException(string message) : base(message) { }
}
