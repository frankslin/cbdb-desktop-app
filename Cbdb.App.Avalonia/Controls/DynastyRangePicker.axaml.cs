using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Core;
using Cbdb.App.Data;

namespace Cbdb.App.Avalonia.Controls;

public partial class DynastyRangePicker : UserControl {
    private readonly IDynastyLookupService _dynastyLookupService = new SqliteDynastyLookupService();
    private IReadOnlyList<DynastyOption> _dynasties = Array.Empty<DynastyOption>();

    private AppLocalizationService? _localizationService;
    private CheckBox _chkUseDynasty = null!;
    private ComboBox _cmbDynastyFrom = null!;
    private TextBlock _lblDynastyTo = null!;
    private ComboBox _cmbDynastyTo = null!;

    public DynastyRangePicker() {
        InitializeComponent();
        InitializeControls();
    }

    public bool UseDynastyRange => _chkUseDynasty.IsChecked == true;

    public DynastyOption? SelectedFrom => _cmbDynastyFrom.SelectedItem as DynastyOption;

    public DynastyOption? SelectedTo => _cmbDynastyTo.SelectedItem as DynastyOption;

    public int OptionCount => _dynasties.Count;

    public void Configure(AppLocalizationService localizationService) {
        if (!ReferenceEquals(_localizationService, localizationService)) {
            if (_localizationService is not null) {
                _localizationService.LanguageChanged -= HandleLanguageChanged;
            }

            _localizationService = localizationService;
            _localizationService.LanguageChanged += HandleLanguageChanged;
            Unloaded += DynastyRangePicker_Unloaded;
        }

        ApplyLocalization();
    }

    public async Task LoadDynastiesAsync(string sqlitePath, CancellationToken cancellationToken = default) {
        _dynasties = await _dynastyLookupService.GetDynastiesAsync(sqlitePath, cancellationToken);
        _cmbDynastyFrom.ItemsSource = _dynasties;
        _cmbDynastyTo.ItemsSource = _dynasties;
        _cmbDynastyFrom.SelectedIndex = -1;
        _cmbDynastyTo.SelectedIndex = -1;
    }

    public (DynastyOption? From, DynastyOption? To) GetNormalizedRange() {
        var selectedFrom = SelectedFrom;
        var selectedTo = SelectedTo;

        if (selectedFrom is not null && selectedTo is not null) {
            var fromSort = selectedFrom.StartYear ?? int.MinValue;
            var toSort = selectedTo.StartYear ?? int.MinValue;
            if (fromSort > toSort) {
                (selectedFrom, selectedTo) = (selectedTo, selectedFrom);
            }
        }

        return (selectedFrom, selectedTo);
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls() {
        _chkUseDynasty = this.FindControl<CheckBox>("ChkUseDynasty") ?? throw new InvalidOperationException("ChkUseDynasty not found.");
        _cmbDynastyFrom = this.FindControl<ComboBox>("CmbDynastyFrom") ?? throw new InvalidOperationException("CmbDynastyFrom not found.");
        _lblDynastyTo = this.FindControl<TextBlock>("LblDynastyTo") ?? throw new InvalidOperationException("LblDynastyTo not found.");
        _cmbDynastyTo = this.FindControl<ComboBox>("CmbDynastyTo") ?? throw new InvalidOperationException("CmbDynastyTo not found.");
    }

    private void ApplyLocalization() {
        if (_localizationService is null) {
            return;
        }

        _chkUseDynasty.Content = T("status_query.use_dynasty");
        _lblDynastyTo.Text = T("status_query.to");
        _cmbDynastyFrom.PlaceholderText = T("status_query.select_dynasty");
        _cmbDynastyTo.PlaceholderText = T("status_query.select_dynasty");
    }

    private void HandleLanguageChanged(object? sender, EventArgs e) {
        ApplyLocalization();
    }

    private void DynastyRangePicker_Unloaded(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e) {
        if (_localizationService is not null) {
            _localizationService.LanguageChanged -= HandleLanguageChanged;
        }

        Unloaded -= DynastyRangePicker_Unloaded;
    }

    private string T(string key) => _localizationService?.Get(key) ?? key;
}
