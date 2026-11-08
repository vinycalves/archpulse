using System.Text.RegularExpressions;

namespace ArchPulse.Core.Analysis;

public static partial class ComponentId
{
    public static string ForProject(string name) => $"project:{Slug(name)}";

    public static string ForResource(string name) => $"resource:{Slug(name)}";

    public static string ForExternal(string name) => $"external:{Slug(name)}";

    public static string Slug(string value) =>
        NonAlphaNumeric().Replace(value.ToLowerInvariant(), "-").Trim('-');

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonAlphaNumeric();
}
