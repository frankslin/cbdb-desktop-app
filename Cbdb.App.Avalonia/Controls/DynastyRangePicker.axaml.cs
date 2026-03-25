using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Avalonia.Modules;
using Cbdb.App.Core;
using Cbdb.App.Data;

namespace Cbdb.App.Avalonia.Controls;

public partial class DynastyRangePicker : UserControl {
    private readonly IDynastyLookupService _dynastyLookupService = new SqliteDynastyLookupService();
    private IReadOnlyList<DynastyOption> _dynasties = Array.Empty<DynastyOption>();
    private readonly HashSet<int> _selectedDynastyIds = new();

    private AppLocalizationService? _localizationService;
    private Button _btnSelectDynasties = null!;
    private TextBox _txtSelectedDynasties = null!;
    private Button _btnClearDynasties = null!;

    public DynastyRangePicker() {
        InitializeComponent();
        InitializeControls();
        _btnSelectDynasties.Click += BtnSelectDynasties_Click;
        _btnClearDynasties.Click += BtnClearDynasties_Click;
    }

    public IReadOnlyList<int> SelectedDynastyIds => _selectedDynastyIds.OrderBy(id => id).ToList();

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
        UpdateSummary();
    }

    public async Task LoadDynastiesAsync(string sqlitePath, CancellationToken cancellationToken = default) {
        _dynasties = await _dynastyLookupService.GetDynastiesAsync(sqlitePath, cancellationToken);
        _selectedDynastyIds.RemoveWhere(id => _dynasties.All(option => option.DynastyId != id));
        UpdateSummary();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls() {
        _btnSelectDynasties = this.FindControl<Button>("BtnSelectDynasties") ?? throw new InvalidOperationException("BtnSelectDynasties not found.");
        _txtSelectedDynasties = this.FindControl<TextBox>("TxtSelectedDynasties") ?? throw new InvalidOperationException("TxtSelectedDynasties not found.");
        _btnClearDynasties = this.FindControl<Button>("BtnClearDynasties") ?? throw new InvalidOperationException("BtnClearDynasties not found.");
    }

    private void ApplyLocalization() {
        if (_localizationService is null) {
            return;
        }

        _btnSelectDynasties.Content = T("dynasty_picker.select_dynasties");
        _btnClearDynasties.Content = T("dynasty_picker.clear_dynasties");
    }

    private void HandleLanguageChanged(object? sender, EventArgs e) {
        ApplyLocalization();
        UpdateSummary();
    }

    private async void BtnSelectDynasties_Click(object? sender, RoutedEventArgs e) {
        if (_localizationService is null) {
            return;
        }

        var picker = new DynastyPickerWindow(_localizationService, _dynasties, _selectedDynastyIds);
        var owner = this.GetVisualRoot() as Window;
        bool? result;
        if (owner is not null) {
            result = await picker.ShowDialog<bool?>(owner);
        } else {
            picker.Show();
            result = null;
        }

        if (result == true) {
            _selectedDynastyIds.Clear();
            foreach (var dynastyId in picker.SelectedDynastyIds) {
                _selectedDynastyIds.Add(dynastyId);
            }
            UpdateSummary();
        } else if (owner is null) {
            UpdateSummary();
        }
    }

    private void BtnClearDynasties_Click(object? sender, RoutedEventArgs e) {
        if (_selectedDynastyIds.Count == 0) {
            return;
        }

        _selectedDynastyIds.Clear();
        UpdateSummary();
    }

    private void UpdateSummary() {
        if (_localizationService is null) {
            return;
        }

        if (_selectedDynastyIds.Count == 0) {
            _txtSelectedDynasties.Text = T("dynasty_picker.all_dynasties");
            return;
        }

        var selectedOptions = _dynasties.Where(option => _selectedDynastyIds.Contains(option.DynastyId)).ToList();
        if (selectedOptions.Count == 1) {
            _txtSelectedDynasties.Text = selectedOptions[0].DisplayLabel;
            return;
        }

        if (selectedOptions.Count == _dynasties.Count && _dynasties.Count > 0) {
            _txtSelectedDynasties.Text = T("dynasty_picker.all_dynasties");
            return;
        }

        _txtSelectedDynasties.Text = string.Format(T("dynasty_picker.selected_count"), selectedOptions.Count);
    }

    private void DynastyRangePicker_Unloaded(object? sender, RoutedEventArgs e) {
        if (_localizationService is not null) {
            _localizationService.LanguageChanged -= HandleLanguageChanged;
        }

        Unloaded -= DynastyRangePicker_Unloaded;
    }

    private string T(string key) => _localizationService?.Get(key) ?? key;
}
