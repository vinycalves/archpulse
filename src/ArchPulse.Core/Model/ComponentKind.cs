namespace ArchPulse.Core.Model;

/// <summary>
/// The role a component plays in the architecture graph.
/// </summary>
public enum ComponentKind
{
    WebApp,
    Worker,
    Library,
    AppHost,
    Test,
    Service,
    Database,
    Cache,
    Broker,
    External
}
