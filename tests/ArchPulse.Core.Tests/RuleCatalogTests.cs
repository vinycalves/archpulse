using ArchPulse.Core.Rules;

namespace ArchPulse.Core.Tests;

public sealed class RuleCatalogTests
{
    [Fact]
    public void CatalogHasUniqueIdsAndCompleteContent()
    {
        var ids = RuleCatalog.All.Select(rule => rule.Id).ToArray();

        Assert.Equal(ids.Length, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(RuleCatalog.All, rule =>
        {
            Assert.False(string.IsNullOrWhiteSpace(rule.Id));
            Assert.False(string.IsNullOrWhiteSpace(rule.Title));
            Assert.False(string.IsNullOrWhiteSpace(rule.Description));
            Assert.False(string.IsNullOrWhiteSpace(rule.Rationale));
        });
    }

    [Fact]
    public void FindsRulesCaseInsensitively()
    {
        var rule = RuleCatalog.Find("ap002");

        Assert.NotNull(rule);
        Assert.Equal("AP002", rule.Id);
    }
}
