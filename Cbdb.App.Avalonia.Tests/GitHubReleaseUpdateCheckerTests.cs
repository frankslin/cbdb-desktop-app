using Xunit;

namespace Cbdb.App.Avalonia.Tests;

public sealed class GitHubReleaseUpdateCheckerTests {
    [Theory]
    [InlineData("0.3.2", "v0.3.3", true)]
    [InlineData("0.3.2", "v0.3.2", false)]
    [InlineData("0.3.2-beta1", "v0.3.2", true)]
    [InlineData("0.3.3", "v0.3.2", false)]
    public void SemanticVersion_ComparisonMatchesExpectedUpdateDirection(string currentVersion, string latestVersion, bool isUpdateAvailable) {
        Assert.True(SemanticVersion.TryParse(currentVersion, out var current));
        Assert.True(SemanticVersion.TryParse(latestVersion, out var latest));
        Assert.NotNull(current);
        Assert.NotNull(latest);

        Assert.Equal(isUpdateAvailable, latest!.CompareTo(current!) > 0);
    }

    [Theory]
    [InlineData("0.3")]
    [InlineData("0.3.2-beta1")]
    [InlineData("v0.3.2")]
    public void SemanticVersion_TryParseAcceptsProjectVersionFormats(string text) {
        Assert.True(SemanticVersion.TryParse(text, out _));
    }
}
