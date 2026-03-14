using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Cbdb.App.Avalonia.Localization;
using System.Reflection;

namespace Cbdb.App.Avalonia;

public partial class AboutWindow : Window {
    private readonly AppLocalizationService _localizationService;
    private TextBlock _txtTitle = null!;
    private TextBlock _txtBody = null!;
    private Button _btnClose = null!;

    public AboutWindow() : this(new AppLocalizationService()) {
    }

    public AboutWindow(AppLocalizationService localizationService) {
        _localizationService = localizationService;
        InitializeComponent();
        InitializeControls();
        _localizationService.LanguageChanged += OnLanguageChanged;
        ApplyLocalization();
        Closed += (_, _) => _localizationService.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e) {
        ApplyLocalization();
    }

    private void ApplyLocalization() {
        Title = _localizationService.Get("about.title");
        _txtTitle.Text = "CBDB";
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "0.0.0";
        _txtBody.Text = string.Format(_localizationService.Get("about.version"), version)
            + Environment.NewLine + Environment.NewLine
            + _localizationService.Get("about.body");
        _btnClose.Content = _localizationService.Get("about.close");
    }

    private void BtnClose_Click(object? sender, RoutedEventArgs e) {
        Close();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls() {
        _txtTitle = this.FindControl<TextBlock>("TxtTitle") ?? throw new InvalidOperationException("TxtTitle not found.");
        _txtBody = this.FindControl<TextBlock>("TxtBody") ?? throw new InvalidOperationException("TxtBody not found.");
        _btnClose = this.FindControl<Button>("BtnClose") ?? throw new InvalidOperationException("BtnClose not found.");
    }
}
