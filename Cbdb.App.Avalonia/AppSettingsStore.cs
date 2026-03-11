using System.Text.Json;

namespace Cbdb.App.Avalonia;

internal static class AppSettingsStore {
    private const string AppDirectoryName = "CbdbApp";
    private const string SettingsFileName = "settings.json";

    public static async Task<string?> TryGetLastSqlitePathAsync(CancellationToken cancellationToken = default) {
        var path = GetSettingsFilePath();
        if (!File.Exists(path)) {
            return null;
        }

        try {
            await using var stream = File.OpenRead(path);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(
                stream,
                cancellationToken: cancellationToken
            );
            return string.IsNullOrWhiteSpace(settings?.LastSqlitePath) ? null : settings.LastSqlitePath;
        } catch {
            return null;
        }
    }

    public static async Task SaveLastSqlitePathAsync(string sqlitePath, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(sqlitePath)) {
            return;
        }

        var directory = GetSettingsDirectory();
        Directory.CreateDirectory(directory);

        var settings = new AppSettings {
            LastSqlitePath = sqlitePath
        };

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

    private sealed class AppSettings {
        public string? LastSqlitePath { get; set; }
    }
}
