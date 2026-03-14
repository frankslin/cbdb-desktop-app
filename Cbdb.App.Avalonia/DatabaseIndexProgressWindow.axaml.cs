using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Cbdb.App.Avalonia.Localization;

namespace Cbdb.App.Avalonia;

public partial class DatabaseIndexProgressWindow : Window {
    private readonly AppLocalizationService _localizationService;
    private TextBlock _txtTitle = null!;
    private TextBlock _txtBody = null!;
    private ProgressBar _progressMain = null!;
    private TextBlock _txtStep = null!;

    public DatabaseIndexProgressWindow() : this(new AppLocalizationService()) {
    }

    public DatabaseIndexProgressWindow(AppLocalizationService localizationService) {
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
        Title = _localizationService.Get("db_index.progress_title");
        _txtTitle.Text = _localizationService.Get("db_index.progress_title");
        if (string.IsNullOrWhiteSpace(_txtBody.Text)) {
            _txtBody.Text = _localizationService.Get("db_index.progress_initial");
        }
    }

    public void UpdateProgress(string bodyText, string stepText, int completedSteps, int totalSteps) {
        _txtBody.Text = bodyText;
        _txtStep.Text = stepText;
        _progressMain.Maximum = Math.Max(1, totalSteps);
        _progressMain.Value = Math.Clamp(completedSteps, 0, Math.Max(1, totalSteps));
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls() {
        _txtTitle = this.FindControl<TextBlock>("TxtTitle") ?? throw new InvalidOperationException("TxtTitle not found.");
        _txtBody = this.FindControl<TextBlock>("TxtBody") ?? throw new InvalidOperationException("TxtBody not found.");
        _progressMain = this.FindControl<ProgressBar>("ProgressMain") ?? throw new InvalidOperationException("ProgressMain not found.");
        _txtStep = this.FindControl<TextBlock>("TxtStep") ?? throw new InvalidOperationException("TxtStep not found.");
    }
}
