using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Cbdb.App.Avalonia.Localization;
using System.Diagnostics;

namespace Cbdb.App.Avalonia;

public partial class AboutWindow : Window {
    private readonly AppLocalizationService _localizationService;
    private readonly UpdateCheckState? _updateCheckState;
    private readonly Func<string>? _getUpdateStatusText;
    private TextBlock _txtTitle = null!;
    private TextBlock _txtBody = null!;
    private TextBlock _txtUpdateStatus = null!;
    private Button _btnCheckUpdates = null!;
    private Button _btnOpenLatestRelease = null!;
    private Button _btnClose = null!;
    private string? _latestReleaseUrl;

    public AboutWindow() : this(new AppLocalizationService()) {
    }

    public AboutWindow(AppLocalizationService localizationService) : this(localizationService, null, null, null) {
    }

    internal AboutWindow(
        AppLocalizationService localizationService,
        UpdateCheckState? updateCheckState,
        Func<string>? getUpdateStatusText,
        string? latestReleaseUrl) {
        _localizationService = localizationService;
        _updateCheckState = updateCheckState;
        _getUpdateStatusText = getUpdateStatusText;
        _latestReleaseUrl = latestReleaseUrl;
        InitializeComponent();
        InitializeControls();
        _localizationService.LanguageChanged += OnLanguageChanged;
        if (_updateCheckState is not null) {
            _updateCheckState.Changed += UpdateCheckState_Changed;
        }
        ApplyLocalization();
        Closed += (_, _) => {
            _localizationService.LanguageChanged -= OnLanguageChanged;
            if (_updateCheckState is not null) {
                _updateCheckState.Changed -= UpdateCheckState_Changed;
            }
        };
    }

    private void OnLanguageChanged(object? sender, EventArgs e) {
        ApplyLocalization();
    }

    private void UpdateCheckState_Changed(object? sender, EventArgs e) {
        ApplyLocalization();
    }

    private void ApplyLocalization() {
        Title = _localizationService.Get("about.title");
        _txtTitle.Text = "CBDB Desktop";
        _txtBody.Text = AppVersionInfo.GetDisplayVersionWithInformational()
            + Environment.NewLine + Environment.NewLine
            + _localizationService.Get("about.body");
        _latestReleaseUrl = _updateCheckState?.LastResult?.ReleaseUrl ?? _latestReleaseUrl;
        _txtUpdateStatus.Text = _getUpdateStatusText?.Invoke() ?? _localizationService.Get("about.update_not_checked");
        _btnCheckUpdates.Content = _localizationService.Get("about.check_updates");
        _btnOpenLatestRelease.Content = _localizationService.Get("about.open_latest_release");
        _btnOpenLatestRelease.IsVisible = !string.IsNullOrWhiteSpace(_latestReleaseUrl);
        _btnOpenLatestRelease.IsEnabled = !string.IsNullOrWhiteSpace(_latestReleaseUrl);
        _btnClose.Content = _localizationService.Get("about.close");
    }

    private void BtnClose_Click(object? sender, RoutedEventArgs e) {
        Close();
    }

    private async void BtnCheckUpdates_Click(object? sender, RoutedEventArgs e) {
        _btnCheckUpdates.IsEnabled = false;
        _btnOpenLatestRelease.IsEnabled = false;
        _btnOpenLatestRelease.IsVisible = false;
        _latestReleaseUrl = null;
        _txtUpdateStatus.Text = _localizationService.Get("about.update_checking");

        try {
            if (_updateCheckState is not null) {
                var result = await _updateCheckState.CheckAsync();
                _latestReleaseUrl = result.ReleaseUrl;
                _txtUpdateStatus.Text = _getUpdateStatusText?.Invoke() ?? _txtUpdateStatus.Text;
            }
        } finally {
            _btnCheckUpdates.IsEnabled = true;
            _btnOpenLatestRelease.IsVisible = !string.IsNullOrWhiteSpace(_latestReleaseUrl);
            _btnOpenLatestRelease.IsEnabled = !string.IsNullOrWhiteSpace(_latestReleaseUrl);
        }
    }

    private void BtnOpenLatestRelease_Click(object? sender, RoutedEventArgs e) {
        if (string.IsNullOrWhiteSpace(_latestReleaseUrl)) {
            return;
        }

        Process.Start(new ProcessStartInfo {
            FileName = _latestReleaseUrl,
            UseShellExecute = true
        });
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls() {
        _txtTitle = this.FindControl<TextBlock>("TxtTitle") ?? throw new InvalidOperationException("TxtTitle not found.");
        _txtBody = this.FindControl<TextBlock>("TxtBody") ?? throw new InvalidOperationException("TxtBody not found.");
        _txtUpdateStatus = this.FindControl<TextBlock>("TxtUpdateStatus") ?? throw new InvalidOperationException("TxtUpdateStatus not found.");
        _btnCheckUpdates = this.FindControl<Button>("BtnCheckUpdates") ?? throw new InvalidOperationException("BtnCheckUpdates not found.");
        _btnOpenLatestRelease = this.FindControl<Button>("BtnOpenLatestRelease") ?? throw new InvalidOperationException("BtnOpenLatestRelease not found.");
        _btnClose = this.FindControl<Button>("BtnClose") ?? throw new InvalidOperationException("BtnClose not found.");
    }
}
