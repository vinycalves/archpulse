using ArchPulse.Api.Contracts;
using ArchPulse.Core.Diff;
using ArchPulse.Core.Model;
using ArchPulse.Core.Rendering;
using ArchPulse.Core.Serialization;
using ArchPulse.Infrastructure.Persistence;
using ArchPulse.Infrastructure.Sources;

namespace ArchPulse.Api.Endpoints;

public static class ScanEndpoints
{
    public static IEndpointRouteBuilder MapScanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scans").WithTags("Scans");

        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);
        group.MapPost("/demo", CreateDemoAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapGet("/{id:guid}/diagram", GetDiagramAsync);
        group.MapGet("/{id:guid}/report", GetReportAsync);
        group.MapGet("/{id:guid}/diff/{baselineId:guid}", GetDiffAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);

        return app;
    }

    private static async Task<IResult> ListAsync(
        ScanRepository repository,
        CancellationToken cancellationToken)
    {
        var scans = await repository.ListAsync(30, cancellationToken);
        return Results.Ok(scans.Select(ToSummaryDto));
    }

    private static async Task<IResult> CreateAsync(
        CreateScanRequest request,
        ScanRepository repository,
        CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null)
        {
            return validation;
        }

        var job = await repository.CreateAsync(
            request.SourceType.Trim().ToLowerInvariant(),
            request.SourceValue.Trim(),
            cancellationToken);

        return Results.Accepted($"/api/scans/{job.Id}", ToDto(job));
    }

    private static async Task<IResult> CreateDemoAsync(
        IConfiguration configuration,
        ScanRepository repository,
        CancellationToken cancellationToken)
    {
        var samplePath = configuration["Demo:SamplePath"];
        if (string.IsNullOrWhiteSpace(samplePath) || !Directory.Exists(samplePath))
        {
            return Results.Problem(
                detail: "The demo sample is not configured. Set Demo:SamplePath or use a GitHub URL.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var displayPath = configuration["Demo:SampleDisplayPath"] ?? samplePath;
        var job = await repository.CreateAsync(
            ScanSourceTypes.Path,
            displayPath,
            cancellationToken);

        return Results.Accepted($"/api/scans/{job.Id}", ToDto(job));
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ScanRepository repository,
        CancellationToken cancellationToken)
    {
        var job = await repository.GetAsync(id, cancellationToken);
        return job is null ? Results.NotFound() : Results.Ok(ToDto(job));
    }

    private static async Task<IResult> GetDiagramAsync(
        Guid id,
        ScanRepository repository,
        CancellationToken cancellationToken)
    {
        var job = await repository.GetAsync(id, cancellationToken);
        if (job is null)
        {
            return Results.NotFound();
        }

        return job.Mermaid is null
            ? Results.Conflict("The scan has not produced a diagram yet.")
            : Results.Text(job.Mermaid, "text/plain; charset=utf-8");
    }

    private static async Task<IResult> GetReportAsync(
        Guid id,
        ScanRepository repository,
        CancellationToken cancellationToken)
    {
        var job = await repository.GetAsync(id, cancellationToken);
        if (job is null)
        {
            return Results.NotFound();
        }

        return job.ReportMarkdown is null
            ? Results.Conflict("The scan has not produced a report yet.")
            : Results.Text(job.ReportMarkdown, "text/markdown; charset=utf-8");
    }

    private static async Task<IResult> GetDiffAsync(
        Guid id,
        Guid baselineId,
        string? format,
        ScanRepository repository,
        CancellationToken cancellationToken)
    {
        var currentJob = await repository.GetAsync(id, cancellationToken);
        var baselineJob = await repository.GetAsync(baselineId, cancellationToken);
        if (currentJob is null || baselineJob is null)
        {
            return Results.NotFound();
        }

        var current = ToOutcome(currentJob);
        var baseline = ToOutcome(baselineJob);
        if (current is null || baseline is null)
        {
            return Results.Conflict("Both scans must be completed before a diff can be generated.");
        }

        var diff = ArchitectureDiffer.Compare(baseline, current);
        if (string.Equals(format, "markdown", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Text(
                MarkdownDiffRenderer.Render(diff, baselineJob.SourceValue, currentJob.SourceValue),
                "text/markdown; charset=utf-8");
        }

        return Results.Ok(diff);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ScanRepository repository,
        CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteAsync(id, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static IResult? Validate(CreateScanRequest request)
    {
        var sourceType = request.SourceType?.Trim().ToLowerInvariant();
        var errors = new Dictionary<string, string[]>();

        if (sourceType is not (ScanSourceTypes.Path or ScanSourceTypes.GitHub))
        {
            errors["sourceType"] = ["Use 'path' or 'github'."];
        }

        if (string.IsNullOrWhiteSpace(request.SourceValue))
        {
            errors["sourceValue"] = ["A repository URL or local path is required."];
        }

        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    private static ScanOutcome? ToOutcome(ScanJob job)
    {
        if (job.GraphJson is null || job.ViolationsJson is null || job.SummaryJson is null)
        {
            return null;
        }

        var graph = ArchPulseJson.Deserialize<ArchitectureGraph>(job.GraphJson);
        var violations = ArchPulseJson.Deserialize<List<ArchitectureViolation>>(job.ViolationsJson);
        var summary = ArchPulseJson.Deserialize<ArchitectureSummary>(job.SummaryJson);

        return graph is null || violations is null || summary is null
            ? null
            : new ScanOutcome(graph, violations, summary);
    }

    private static ScanSummaryDto ToSummaryDto(ScanJob job) =>
        new(
            job.Id,
            job.Status,
            job.SourceType,
            job.SourceValue,
            job.CreatedAt,
            job.CompletedAt,
            job.Error,
            job.SummaryJson is null
                ? null
                : ArchPulseJson.Deserialize<ArchitectureSummary>(job.SummaryJson));

    private static ScanDto ToDto(ScanJob job) =>
        new(
            job.Id,
            job.Status,
            job.SourceType,
            job.SourceValue,
            job.CreatedAt,
            job.StartedAt,
            job.CompletedAt,
            job.Error,
            job.SummaryJson is null
                ? null
                : ArchPulseJson.Deserialize<ArchitectureSummary>(job.SummaryJson),
            job.GraphJson is null
                ? null
                : ArchPulseJson.Deserialize<ArchitectureGraph>(job.GraphJson),
            job.ViolationsJson is null
                ? null
                : ArchPulseJson.Deserialize<List<ArchitectureViolation>>(job.ViolationsJson),
            job.Mermaid,
            job.ReportMarkdown);
}
