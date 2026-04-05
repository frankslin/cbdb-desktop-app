using System.Net.Http.Headers;
using System.Text.Json;

namespace Cbdb.App.Avalonia;

internal sealed class GitHubReleaseUpdateChecker {
    internal const string LatestReleaseApiUrl = "https://api.github.com/repos/frankslin/cbdb-desktop-app/releases/latest";
    internal const string ReleasesPageUrl = "https://github.com/frankslin/cbdb-desktop-app/releases";

    private readonly HttpClient _httpClient;

    public GitHubReleaseUpdateChecker(HttpClient? httpClient = null) {
        _httpClient = httpClient ?? CreateHttpClient();
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default) {
        var currentVersionText = AppVersionInfo.GetDisplayVersion();
        if (!SemanticVersion.TryParse(currentVersionText, out var currentVersion)) {
            return UpdateCheckResult.Failed(currentVersionText, "Could not parse the current app version.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApiUrl);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync<GitHubLatestReleaseResponse>(
            stream,
            cancellationToken: cancellationToken);

        if (release is null || string.IsNullOrWhiteSpace(release.TagName)) {
            return UpdateCheckResult.Failed(currentVersionText, "GitHub did not return a usable latest release.");
        }

        if (!SemanticVersion.TryParse(release.TagName, out var latestVersion)) {
            return UpdateCheckResult.Failed(currentVersionText, $"Could not parse the latest release tag '{release.TagName}'.");
        }

        return new UpdateCheckResult(
            currentVersionText,
            release.TagName,
            ReleasesPageUrl,
            release.PublishedAt,
            latestVersion!.CompareTo(currentVersion!) > 0,
            null);
    }

    private static HttpClient CreateHttpClient() {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CBDB", AppVersionInfo.GetDisplayVersion()));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    private sealed record GitHubLatestReleaseResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("tag_name")] string? TagName,
        [property: System.Text.Json.Serialization.JsonPropertyName("html_url")] string? HtmlUrl,
        [property: System.Text.Json.Serialization.JsonPropertyName("published_at")] DateTimeOffset? PublishedAt);
}

internal sealed record UpdateCheckResult(
    string CurrentVersion,
    string? LatestVersion,
    string? ReleaseUrl,
    DateTimeOffset? PublishedAt,
    bool IsUpdateAvailable,
    string? ErrorMessage) {
    public static UpdateCheckResult Failed(string currentVersion, string errorMessage) =>
        new(currentVersion, null, null, null, false, errorMessage);

    public static UpdateCheckResult Checking(string currentVersion) =>
        new(currentVersion, null, null, null, false, null);
}

internal sealed class SemanticVersion : IComparable<SemanticVersion> {
    private readonly string? _prerelease;

    private SemanticVersion(int major, int minor, int patch, string? prerelease) {
        Major = major;
        Minor = minor;
        Patch = patch;
        _prerelease = string.IsNullOrWhiteSpace(prerelease) ? null : prerelease.Trim();
    }

    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }

    public static bool TryParse(string? text, out SemanticVersion? version) {
        version = null;
        if (string.IsNullOrWhiteSpace(text)) {
            return false;
        }

        var trimmed = text.Trim();
        if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase)) {
            trimmed = trimmed[1..];
        }

        var plusIndex = trimmed.IndexOf('+');
        if (plusIndex >= 0) {
            trimmed = trimmed[..plusIndex];
        }

        string? prerelease = null;
        var dashIndex = trimmed.IndexOf('-');
        if (dashIndex >= 0) {
            prerelease = trimmed[(dashIndex + 1)..];
            trimmed = trimmed[..dashIndex];
        }

        var parts = trimmed.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length is < 2 or > 3) {
            return false;
        }

        if (!int.TryParse(parts[0], out var major) || !int.TryParse(parts[1], out var minor)) {
            return false;
        }

        var patch = 0;
        if (parts.Length == 3 && !int.TryParse(parts[2], out patch)) {
            return false;
        }

        version = new SemanticVersion(major, minor, patch, prerelease);
        return true;
    }

    public int CompareTo(SemanticVersion? other) {
        if (other is null) {
            return 1;
        }

        var coreComparison = Major.CompareTo(other.Major);
        if (coreComparison != 0) {
            return coreComparison;
        }

        coreComparison = Minor.CompareTo(other.Minor);
        if (coreComparison != 0) {
            return coreComparison;
        }

        coreComparison = Patch.CompareTo(other.Patch);
        if (coreComparison != 0) {
            return coreComparison;
        }

        if (_prerelease is null && other._prerelease is null) {
            return 0;
        }

        if (_prerelease is null) {
            return 1;
        }

        if (other._prerelease is null) {
            return -1;
        }

        return StringComparer.OrdinalIgnoreCase.Compare(_prerelease, other._prerelease);
    }
}
