namespace ArchPulse.Core.Tests;

internal sealed class TestRepository : IDisposable
{
    public TestRepository()
    {
        Root = Path.Combine(
            Path.GetTempPath(),
            "archpulse-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string Write(string relativePath, string content)
    {
        var fullPath = Path.Combine(Root, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    public void WriteProject(
        string relativeDirectory,
        string projectName,
        string sdk,
        IEnumerable<string>? projectReferences = null,
        IEnumerable<string>? packageReferences = null)
    {
        var references = string.Join(
            Environment.NewLine,
            (projectReferences ?? []).Select(reference =>
                $"    <ProjectReference Include=\"{reference}\" />"));

        var packages = string.Join(
            Environment.NewLine,
            (packageReferences ?? []).Select(package =>
                $"    <PackageReference Include=\"{package}\" Version=\"1.0.0\" />"));

        var content =
            $"""
            <Project Sdk="{sdk}">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <OutputType>{(sdk.Contains(".Web") || sdk.Contains(".Worker") ? "Exe" : "Library")}</OutputType>
              </PropertyGroup>
              <ItemGroup>
            {references}
            {packages}
              </ItemGroup>
            </Project>
            """;

        Write(Path.Combine(relativeDirectory, $"{projectName}.csproj"), content);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best effort cleanup for the test temp folder.
        }
    }
}
