using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Core;

namespace Cbdb.App.Avalonia.Modules;

public partial class StatusCodePickerWindow : Window {
    private readonly AppLocalizationService _localizationService;
    private readonly List<StatusCodeOption> _allOptions;
    private readonly HashSet<string> _selectedCodes;

    private TextBox _txtSearch = null!;
    private Button _btnClearSearch = null!;
    private TextBlock _txtSummary = null!;
    private StackPanel _statusOptionHost = null!;
    private TextBlock _txtSelectionHint = null!;
    private Button _btnClearAll = null!;
    private Button _btnCancel = null!;
    private Button _btnApply = null!;

    public StatusCodePickerWindow()
        : this(new AppLocalizationService(), Array.Empty<StatusCodeOption>()) {
    }

    public StatusCodePickerWindow(
        AppLocalizationService localizationService,
        IReadOnlyList<StatusCodeOption> options,
        IReadOnlyCollection<string>? selectedCodes = null
    ) {
        _localizationService = localizationService;
        _allOptions = options.OrderBy(option => option.DisplayLabel, StringComparer.CurrentCultureIgnoreCase).ToList();
        _selectedCodes = selectedCodes is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(selectedCodes, StringComparer.OrdinalIgnoreCase);

        InitializeComponent();
        InitializeControls();

        _localizationService.LanguageChanged += HandleLanguageChanged;
        Closed += (_, _) => _localizationService.LanguageChanged -= HandleLanguageChanged;

        ApplyLocalization();
        RenderOptions();
    }

    public IReadOnlyList<string> SelectedCodes =>
        _selectedCodes.OrderBy(code => code, StringComparer.OrdinalIgnoreCase).ToList();

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls() {
        _txtSearch = this.FindControl<TextBox>("TxtSearch") ?? throw new InvalidOperationException("TxtSearch not found.");
        _btnClearSearch = this.FindControl<Button>("BtnClearSearch") ?? throw new InvalidOperationException("BtnClearSearch not found.");
        _txtSummary = this.FindControl<TextBlock>("TxtSummary") ?? throw new InvalidOperationException("TxtSummary not found.");
        _statusOptionHost = this.FindControl<StackPanel>("StatusOptionHost") ?? throw new InvalidOperationException("StatusOptionHost not found.");
        _txtSelectionHint = this.FindControl<TextBlock>("TxtSelectionHint") ?? throw new InvalidOperationException("TxtSelectionHint not found.");
        _btnClearAll = this.FindControl<Button>("BtnClearAll") ?? throw new InvalidOperationException("BtnClearAll not found.");
        _btnCancel = this.FindControl<Button>("BtnCancel") ?? throw new InvalidOperationException("BtnCancel not found.");
        _btnApply = this.FindControl<Button>("BtnApply") ?? throw new InvalidOperationException("BtnApply not found.");
    }

    private void ApplyLocalization() {
        Title = T("status_query.picker_title");
        _txtSearch.Watermark = T("status_query.search_placeholder");
        _btnClearSearch.Content = T("browser.clear");
        _btnClearAll.Content = T("status_query.clear_all");
        _btnCancel.Content = T("status_query.cancel");
        _btnApply.Content = T("status_query.apply");
        UpdateSummary();
    }

    private void HandleLanguageChanged(object? sender, EventArgs e) {
        ApplyLocalization();
        RenderOptions();
    }

    private void TxtSearch_KeyUp(object? sender, KeyEventArgs e) {
        RenderOptions();
    }

    private void BtnClearSearch_Click(object? sender, RoutedEventArgs e) {
        _txtSearch.Text = string.Empty;
        RenderOptions();
    }

    private void BtnClearAll_Click(object? sender, RoutedEventArgs e) {
        _selectedCodes.Clear();
        RenderOptions();
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e) {
        Close(false);
    }

    private void BtnApply_Click(object? sender, RoutedEventArgs e) {
        Close(true);
    }

    private void RenderOptions() {
        _statusOptionHost.Children.Clear();

        var keyword = _txtSearch.Text?.Trim();
        var filtered = string.IsNullOrWhiteSpace(keyword)
            ? _allOptions
            : _allOptions.Where(option =>
                option.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(option.Description) && option.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(option.DescriptionChn) && option.DescriptionChn.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            ).ToList();

        foreach (var option in filtered) {
            var checkBox = new CheckBox {
                Content = $"{option.DisplayLabel} [{option.UsageCount:N0}]",
                Tag = option.Code,
                IsChecked = _selectedCodes.Contains(option.Code)
            };
            checkBox.IsCheckedChanged += OptionCheckBox_Changed;
            _statusOptionHost.Children.Add(checkBox);
        }

        if (filtered.Count == 0) {
            _statusOptionHost.Children.Add(new TextBlock {
                Text = T("status_query.picker_no_match"),
                Foreground = global::Avalonia.Media.Brushes.DimGray
            });
        }

        UpdateSummary(filtered.Count);
    }

    private void OptionCheckBox_Changed(object? sender, RoutedEventArgs e) {
        if (sender is not CheckBox checkBox || checkBox.Tag is not string code) {
            return;
        }

        if (checkBox.IsChecked == true) {
            _selectedCodes.Add(code);
        } else {
            _selectedCodes.Remove(code);
        }

        UpdateSummary();
    }

    private void UpdateSummary(int? visibleCount = null) {
        var count = _selectedCodes.Count;
        _txtSummary.Text = string.Format(T("status_query.picker_summary"), count, _allOptions.Count);
        _txtSelectionHint.Text = visibleCount.HasValue
            ? string.Format(T("status_query.picker_visible"), visibleCount.Value)
            : string.Empty;
    }

    private string T(string key) => _localizationService.Get(key);
}
