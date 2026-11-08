using System.Xml.Linq;

namespace ArchPulse.Core.Scanning;

/// <summary>
/// Finds .NET projects under a repository root and extracts the information
/// ArchPulse needs without running MSBuild.
/// </summary>
public static class ProjectDiscovery
{
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", ".vs", ".idea", "node_modules", ".data", "artifacts",
        "packages", "TestResults", "samples"
    };

    public static IReadOnlyList<ProjectInfo> Discover(string rootPath)
    {
        var root = Path.GetFullPath(rootPath);
        var projects = new List<ProjectInfo>();

        foreach (var path in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories))
        {
            if (IsIgnored(root, path))
            {
                continue;
            }

            projects.Add(Parse(root, path));
        }

        return projects
            .OrderBy(project => project.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsIgnored(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment => IgnoredDirectories.Contains(segment));
    }

    private static ProjectInfo Parse(string root, string path)
    {
        var document = XDocument.Load(path);
        var project = document.Root
            ?? throw new InvalidOperationException($"Project file '{path}' has no root element.");

        var projectDirectory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException($"Cannot resolve the directory of '{path}'.");

        var sdk = project.Attribute("Sdk")?.Value ?? string.Empty;
        var sdkElements = project.Elements("Sdk")
            .Select(element => element.Attribute("Name")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value));
        var allSdks = string.Join(';', new[] { sdk }.Concat(sdkElements!));

        var properties = project.Elements("PropertyGroup")
            .Elements()
            .GroupBy(element => element.Name.LocalName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Last().Value,
                StringComparer.OrdinalIgnoreCase);

        var projectReferences = project.Elements("ItemGroup")
            .Elements("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFullPath(Path.Combine(
                projectDirectory,
                value!.Replace('\\', Path.DirectorySeparatorChar))))
            .ToArray();

        var packageReferences = project.Elements("ItemGroup")
            .Elements("PackageReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var name = Path.GetFileNameWithoutExtension(path);
        var isTest = IsTestProject(name, properties, packageReferences);
        var relativePath = Path.GetRelativePath(root, path).Replace('\\', '/');
        var outputType = properties.GetValueOrDefault("OutputType");

        return new ProjectInfo(
            Name: name,
            FullPath: path,
            RelativePath: relativePath,
            Sdk: allSdks,
            OutputType: outputType,
            IsTest: isTest,
            ProjectReferencePaths: projectReferences,
            PackageReferences: packageReferences,
            Properties: properties);
    }

    private static bool IsTestProject(
        string name,
        IReadOnlyDictionary<string, string> properties,
        IReadOnlyList<string> packageReferences)
    {
        if (properties.TryGetValue("IsTestProject", out var value)
            && bool.TryParse(value, out var isTestProject)
            && isTestProject)
        {
            return true;
        }

        if (name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".Test", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return packageReferences.Any(package =>
            package.Equals("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase)
            || package.Equals("xunit", StringComparison.OrdinalIgnoreCase)
            || package.Equals("xunit.v3", StringComparison.OrdinalIgnoreCase)
            || package.Equals("NUnit", StringComparison.OrdinalIgnoreCase)
            || package.Equals("MSTest.TestFramework", StringComparison.OrdinalIgnoreCase));
    }
}
