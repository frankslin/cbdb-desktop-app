using System.Text.Json;
using Cbdb.App.Core;

namespace Cbdb.App.Avalonia;

internal static class AppSettingsStore {
    private const string AppDirectoryName = "CbdbApp";
    private const string SettingsFileName = "settings.json";

    public static UiLanguage? TryGetLastLanguage() {
        try {
            var settings = LoadSettings();
            if (settings is null || string.IsNullOrWhiteSpace(settings.LastLanguage)) {
                return null;
            }

            return Enum.TryParse<UiLanguage>(settings.LastLanguage, ignoreCase: true, out var language)
                ? language
                : null;
        } catch {
            return null;
        }
    }

    public static async Task<string?> TryGetLastSqlitePathAsync(CancellationToken cancellationToken = default) {
        try {
            var settings = await LoadSettingsAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(settings?.LastSqlitePath) ? null : settings.LastSqlitePath;
        } catch {
            return null;
        }
    }

    public static async Task SaveLastLanguageAsync(UiLanguage language, CancellationToken cancellationToken = default) {
        var settings = await LoadSettingsAsync(cancellationToken) ?? new AppSettings();
        settings.LastLanguage = language.ToString();
        await SaveSettingsAsync(settings, cancellationToken);
    }

    public static async Task SaveLastSqlitePathAsync(string sqlitePath, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(sqlitePath)) {
            return;
        }

        var settings = await LoadSettingsAsync(cancellationToken) ?? new AppSettings();
        settings.LastSqlitePath = sqlitePath;
        await SaveSettingsAsync(settings, cancellationToken);
    }

    private static string GetSettingsDirectory() {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData)) {
            localAppData = AppContext.BaseDirectory;
        }

        return Path.Combine(localAppData, AppDirectoryName);
    }

    private static string GetSettingsFilePath() {
        return Path.Combine(GetSettingsDirectory(), SettingsFileName);
    }

    private static AppSettings? LoadSettings() {
        var path = GetSettingsFilePath();
        if (!File.Exists(path)) {
            return null;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppSettings>(json);
    }

    private static async Task<AppSettings?> LoadSettingsAsync(CancellationToken cancellationToken) {
        var path = GetSettingsFilePath();
        if (!File.Exists(path)) {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<AppSettings>(
            stream,
            cancellationToken: cancellationToken
        );
    }

    private static async Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken) {
        var directory = GetSettingsDirectory();
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, $"{SettingsFileName}.tmp");
        await using (var stream = File.Create(tempPath)) {
            await JsonSerializer.SerializeAsync(stream, settings, cancellationToken: cancellationToken);
        }

        var finalPath = GetSettingsFilePath();
        if (File.Exists(finalPath)) {
            File.Delete(finalPath);
        }

        File.Move(tempPath, finalPath);
    }

    private sealed class AppSettings {
        public string? LastSqlitePath { get; set; }
        public string? LastLanguage { get; set; }
    }
}
