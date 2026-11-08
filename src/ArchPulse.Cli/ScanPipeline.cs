using System.Text.Json;
using ArchPulse.Core.Analysis;
using ArchPulse.Core.Model;
using ArchPulse.Core.Rendering;
using ArchPulse.Core.Serialization;

namespace ArchPulse.Cli;

internal static class ScanPipeline
{
    public static ScanOutcome Scan(string path) =>
        new RepositoryScanner().Scan(ResolveRoot(path));

    public static string ResolveRoot(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return File.Exists(fullPath)
            ? Path.GetDirectoryName(fullPath) ?? fullPath
            : fullPath;
    }

    public static string[] WriteArtifacts(
        ScanOutcome outcome,
        string source,
        string outputDirectory)
    {
        var directory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(directory);

        var jsonPath = Path.Combine(directory, "architecture.json");
        var mermaidPath = Path.Combine(directory, "architecture.mmd");
        var reportPath = Path.Combine(directory, "report.md");

        File.WriteAllText(jsonPath, ArchPulseJson.Serialize(outcome));
        File.WriteAllText(mermaidPath, MermaidRenderer.Render(outcome.Graph));
        File.WriteAllText(
            reportPath,
            MarkdownReportRenderer.Render(outcome, source, DateTimeOffset.UtcNow));

        return [jsonPath, mermaidPath, reportPath];
    }

    public static ScanOutcome LoadOutcome(string path)
    {
        var json = File.ReadAllText(path);

        try
        {
            var outcome = ArchPulseJson.Deserialize<ScanOutcome>(json);
            if (outcome?.Graph is not null)
            {
                return outcome;
            }
        }
        catch (JsonException)
        {
            // Fall back to a raw ArchitectureGraph document.
        }

        var graph = ArchPulseJson.Deserialize<ArchitectureGraph>(json)
            ?? throw new InvalidOperationException(
                $"'{path}' is not a valid ArchPulse scan or graph.");

        return new ScanOutcome(graph, [], ArchitectureSummary.From(graph, []));
    }
}
