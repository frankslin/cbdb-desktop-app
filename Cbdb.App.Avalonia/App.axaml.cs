using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Cbdb.App.Avalonia;

public partial class App : Application {
    private AboutWindow? _aboutWindow;

    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
        ConfigureNativeMenu();
    }

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureNativeMenu() {
        var aboutItem = new NativeMenuItem("About CBDB");
        aboutItem.Click += AboutMenuItem_Click;

        var menu = new NativeMenu();
        menu.Items.Add(aboutItem);
        NativeMenu.SetMenu(this, menu);
    }

    private void AboutMenuItem_Click(object? sender, EventArgs e) {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } mainWindow }) {
            ShowAboutWindow(mainWindow);
        }
    }

    internal void ShowAboutWindow(Window owner) {
        if (_aboutWindow is { } existingWindow) {
            existingWindow.Activate();
            return;
        }

        var localizationService = owner is MainWindow mainWindow
            ? mainWindow.LocalizationService
            : new Localization.AppLocalizationService();

        var aboutWindow = new AboutWindow(localizationService);
        _aboutWindow = aboutWindow;
        aboutWindow.Closed += (_, _) => {
            if (ReferenceEquals(_aboutWindow, aboutWindow)) {
                _aboutWindow = null;
            }
        };
        aboutWindow.Show(owner);
    }
}
