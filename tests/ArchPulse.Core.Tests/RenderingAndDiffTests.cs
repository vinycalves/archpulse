using ArchPulse.Core.Diff;
using ArchPulse.Core.Model;
using ArchPulse.Core.Rendering;

namespace ArchPulse.Core.Tests;

public sealed class RenderingAndDiffTests
{
    [Fact]
    public void MermaidRendererGroupsComponentsAndDrawsDependencies()
    {
        var graph = new ArchitectureGraph(
            [
                new ArchitectureComponent("api", "Sample.Api", ComponentKind.WebApp, "Presentation"),
                new ArchitectureComponent("domain", "Sample.Domain", ComponentKind.Library, "Domain"),
                new ArchitectureComponent("db", "PostgreSQL", ComponentKind.Database, "Infrastructure")
            ],
            [
                new ArchitectureDependency("api", "domain", DependencyKind.ProjectReference),
                new ArchitectureDependency("api", "db", DependencyKind.AspireReference)
            ]);

        var mermaid = MermaidRenderer.Render(graph);

        Assert.Contains("flowchart LR", mermaid);
        Assert.Contains("subgraph", mermaid);
        Assert.Contains("Sample.Api", mermaid);
        Assert.Contains("|ProjectReference|", mermaid);
        Assert.Contains("|AspireReference|", mermaid);
    }

    [Fact]
    public void MarkdownReportContainsSummaryViolationsAndDiagram()
    {
        var graph = new ArchitectureGraph(
            [new ArchitectureComponent("api", "Sample.Api", ComponentKind.WebApp, "Presentation")],
            []);
        var violations = new[]
        {
            new ArchitectureViolation(
                "AP002",
                ViolationSeverity.Error,
                "Layer dependency violation",
                "Sample.Api must not depend on Sample.Domain.")
        };
        var outcome = new ScanOutcome(graph, violations, ArchitectureSummary.From(graph, violations));

        var report = MarkdownReportRenderer.Render(outcome, "repo", DateTimeOffset.UnixEpoch);

        Assert.Contains("# ArchPulse architecture report", report);
        Assert.Contains("| Violations | 1 |", report);
        Assert.Contains("```mermaid", report);
        Assert.Contains("AP002", report);
    }

    [Fact]
    public void DiffReportsAddedAndRemovedNodesAndEdges()
    {
        var before = new ScanOutcome(
            new ArchitectureGraph(
                [
                    new ArchitectureComponent("a", "A", ComponentKind.Library, "Domain"),
                    new ArchitectureComponent("b", "B", ComponentKind.Library, "Infrastructure")
                ],
                [new ArchitectureDependency("a", "b", DependencyKind.ProjectReference)]),
            [],
            new ArchitectureSummary(2, 1, 0, 0, 0, 0, 0));

        var after = new ScanOutcome(
            new ArchitectureGraph(
                [
                    new ArchitectureComponent("a", "A", ComponentKind.Library, "Domain"),
                    new ArchitectureComponent("b", "B", ComponentKind.Library, "Infrastructure"),
                    new ArchitectureComponent("c", "C", ComponentKind.WebApp, "Presentation")
                ],
                [
                    new ArchitectureDependency("a", "b", DependencyKind.ProjectReference),
                    new ArchitectureDependency("c", "a", DependencyKind.ProjectReference)
                ]),
            [],
            new ArchitectureSummary(3, 2, 1, 0, 0, 0, 0));

        var diff = ArchitectureDiffer.Compare(before, after);
        var markdown = MarkdownDiffRenderer.Render(diff, "before", "after");

        Assert.True(diff.HasChanges);
        Assert.Single(diff.AddedComponents);
        Assert.Equal("C", diff.AddedComponents[0].Name);
        Assert.Single(diff.AddedDependencies);
        Assert.Contains("Added components", markdown);
        Assert.Contains("Added dependencies", markdown);
    }

    [Fact]
    public void DiffReportsNewViolations()
    {
        var graph = new ArchitectureGraph([], []);
        var violation = new ArchitectureViolation(
            "AP001",
            ViolationSeverity.Error,
            "Project reference cycle",
            "Circular dependency detected.");

        var before = new ScanOutcome(graph, [], new ArchitectureSummary(0, 0, 0, 0, 0, 0, 0));
        var after = new ScanOutcome(
            graph,
            [violation],
            new ArchitectureSummary(0, 0, 0, 1, 1, 0, 1));

        var diff = ArchitectureDiffer.Compare(before, after);

        Assert.Single(diff.NewViolations);
        Assert.Equal("AP001", diff.NewViolations[0].RuleId);
    }
}
