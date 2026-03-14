using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Cbdb.App.Avalonia.Localization;

namespace Cbdb.App.Avalonia;

public partial class ConfirmActionWindow : Window {
    private readonly AppLocalizationService _localizationService;
    private readonly string _titleText;
    private readonly string _bodyText;
    private TextBlock _txtTitle = null!;
    private TextBlock _txtBody = null!;
    private Button _btnNo = null!;
    private Button _btnYes = null!;

    public ConfirmActionWindow() : this(new AppLocalizationService(), string.Empty, string.Empty) {
    }

    public ConfirmActionWindow(AppLocalizationService localizationService, string titleText, string bodyText) {
        _localizationService = localizationService;
        _titleText = titleText;
        _bodyText = bodyText;
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
        Title = _titleText;
        _txtTitle.Text = _titleText;
        _txtBody.Text = _bodyText;
        _btnNo.Content = _localizationService.Get("dialog.no");
        _btnYes.Content = _localizationService.Get("dialog.yes");
    }

    private void BtnNo_Click(object? sender, RoutedEventArgs e) {
        Close(false);
    }

    private void BtnYes_Click(object? sender, RoutedEventArgs e) {
        Close(true);
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls() {
        _txtTitle = this.FindControl<TextBlock>("TxtTitle") ?? throw new InvalidOperationException("TxtTitle not found.");
        _txtBody = this.FindControl<TextBlock>("TxtBody") ?? throw new InvalidOperationException("TxtBody not found.");
        _btnNo = this.FindControl<Button>("BtnNo") ?? throw new InvalidOperationException("BtnNo not found.");
        _btnYes = this.FindControl<Button>("BtnYes") ?? throw new InvalidOperationException("BtnYes not found.");
    }
}
