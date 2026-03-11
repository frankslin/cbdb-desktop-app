using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace Cbdb.App.Avalonia.Tests.TestInfrastructure;

internal static class AvaloniaUiTestHelper {
    public static TControl FindRequiredControl<TControl>(TopLevel root, string name) where TControl : Control {
        return root.FindControl<TControl>(name) ?? throw new InvalidOperationException($"{name} not found.");
    }

    public static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout, string failureMessage) {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline) {
            Dispatcher.UIThread.RunJobs();
            if (condition()) {
                return;
            }

            await Task.Delay(20);
        }

        Dispatcher.UIThread.RunJobs();
        if (!condition()) {
            throw new TimeoutException(failureMessage);
        }
    }

    public static string WriteArtifact(string testName, string fileName, Action<string> writer) {
        var directory = Path.Combine(GetArtifactsRoot(), SanitizePathSegment(testName));
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, fileName);
        writer(path);
        return path;
    }

    public static string WriteJsonArtifact<T>(string testName, string fileName, T value) {
        return WriteArtifact(testName, fileName, path => {
            var json = JsonSerializer.Serialize(value, new JsonSerializerOptions {
                WriteIndented = true
            });
            File.WriteAllText(path, json);
        });
    }

    private static string GetArtifactsRoot() {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null) {
            if (current.GetFiles("Cbdb.WindowsApp.sln").Length > 0) {
                return Path.Combine(current.FullName, "artifacts", "ui-tests");
            }

            current = current.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "artifacts", "ui-tests");
    }

    private static string SanitizePathSegment(string value) {
        foreach (var invalid in Path.GetInvalidFileNameChars()) {
            value = value.Replace(invalid, '_');
        }

        return value;
    }
}
