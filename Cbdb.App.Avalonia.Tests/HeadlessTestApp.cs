using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;

[assembly: AvaloniaTestApplication(typeof(Cbdb.App.Avalonia.Tests.HeadlessTestApp))]

namespace Cbdb.App.Avalonia.Tests;

public static class HeadlessTestApp {
    public static AppBuilder BuildAvaloniaApp() {
        return AppBuilder.Configure<Cbdb.App.Avalonia.App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions {
                UseHeadlessDrawing = false
            });
    }
}
