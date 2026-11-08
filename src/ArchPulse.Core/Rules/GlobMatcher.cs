using System.Text.RegularExpressions;

namespace ArchPulse.Core.Rules;

internal static class GlobMatcher
{
    public static bool IsMatch(string value, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern) || pattern == "*")
        {
            return true;
        }

        var regex = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*", StringComparison.Ordinal)
            .Replace("\\?", ".", StringComparison.Ordinal) + "$";

        return Regex.IsMatch(
            value,
            regex,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
