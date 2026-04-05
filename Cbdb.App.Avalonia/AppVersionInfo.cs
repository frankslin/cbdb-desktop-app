using System.Reflection;

namespace Cbdb.App.Avalonia;

internal static class AppVersionInfo {
    public static string GetDisplayVersion() {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, "DisplayVersion", StringComparison.Ordinal))?.Value
            ?? (assembly.GetName().Version is { } assemblyVersion
                ? $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}"
                : "0.0.0");
    }

    public static string GetInformationalVersion() {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
    }

    public static string GetDisplayVersionWithInformational() {
        var version = GetDisplayVersion();
        var informationalVersion = GetInformationalVersion();
        return string.IsNullOrWhiteSpace(informationalVersion)
            ? version
            : $"{version} ({informationalVersion})";
    }
}
