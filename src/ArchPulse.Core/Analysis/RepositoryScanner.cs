using ArchPulse.Core.Model;
using ArchPulse.Core.Rules;
using ArchPulse.Core.Scanning;

namespace ArchPulse.Core.Analysis;

/// <summary>
/// Entry point for scanning a repository on disk.
/// </summary>
public sealed class RepositoryScanner
{
    public ScanOutcome Scan(string rootPath)
    {
        var root = Path.GetFullPath(rootPath);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"Repository root '{root}' was not found.");
        }

        var projects = ProjectDiscovery.Discover(root);
        var appHosts = AppHostDiscovery.Discover(projects);
        var graph = new ArchitectureGraphBuilder().Build(projects, appHosts);
        var rules = ArchitectureRules.Load(root);
        var violations = new ArchitectureRuleEngine(rules).Evaluate(projects, graph, appHosts);
        var summary = ArchitectureSummary.From(graph, violations);

        return new ScanOutcome(graph, violations, summary);
    }
}
