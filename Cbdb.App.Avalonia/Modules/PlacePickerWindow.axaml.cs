using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Core;

namespace Cbdb.App.Avalonia.Modules;

public partial class PlacePickerWindow : Window {
    private const int MaxVisibleOptionsWithoutSearch = 300;
    private const int MaxVisibleOptionsWithSearch = 500;

    private readonly AppLocalizationService _localizationService;
    private readonly List<PlaceOption> _allOptions;
    private readonly HashSet<int> _selectedPlaceIds;

    private TextBox _txtSearch = null!;
    private Button _btnRunSearch = null!;
    private Button _btnClearSearch = null!;
    private TextBlock _txtSummary = null!;
    private StackPanel _placeOptionHost = null!;
    private TextBlock _txtSelectionHint = null!;
    private Button _btnClearAll = null!;
    private Button _btnCancel = null!;
    private Button _btnApply = null!;
    private string? _activeKeyword;

    public PlacePickerWindow()
        : this(new AppLocalizationService(), Array.Empty<PlaceOption>()) {
    }

    public PlacePickerWindow(
        AppLocalizationService localizationService,
        IReadOnlyList<PlaceOption> options,
        IReadOnlyCollection<int>? selectedPlaceIds = null
    ) {
        _localizationService = localizationService;
        _allOptions = options.ToList();
        _selectedPlaceIds = selectedPlaceIds is null
            ? new HashSet<int>()
            : new HashSet<int>(selectedPlaceIds);

        InitializeComponent();
        InitializeControls();

        _localizationService.LanguageChanged += HandleLanguageChanged;
        Closed += (_, _) => _localizationService.LanguageChanged -= HandleLanguageChanged;

        ApplyLocalization();
        RenderOptions();
    }

    public IReadOnlyList<int> SelectedPlaceIds => _selectedPlaceIds.OrderBy(id => id).ToList();

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls() {
        _txtSearch = this.FindControl<TextBox>("TxtSearch") ?? throw new InvalidOperationException("TxtSearch not found.");
        _btnRunSearch = this.FindControl<Button>("BtnRunSearch") ?? throw new InvalidOperationException("BtnRunSearch not found.");
        _btnClearSearch = this.FindControl<Button>("BtnClearSearch") ?? throw new InvalidOperationException("BtnClearSearch not found.");
        _txtSummary = this.FindControl<TextBlock>("TxtSummary") ?? throw new InvalidOperationException("TxtSummary not found.");
        _placeOptionHost = this.FindControl<StackPanel>("PlaceOptionHost") ?? throw new InvalidOperationException("PlaceOptionHost not found.");
        _txtSelectionHint = this.FindControl<TextBlock>("TxtSelectionHint") ?? throw new InvalidOperationException("TxtSelectionHint not found.");
        _btnClearAll = this.FindControl<Button>("BtnClearAll") ?? throw new InvalidOperationException("BtnClearAll not found.");
        _btnCancel = this.FindControl<Button>("BtnCancel") ?? throw new InvalidOperationException("BtnCancel not found.");
        _btnApply = this.FindControl<Button>("BtnApply") ?? throw new InvalidOperationException("BtnApply not found.");
    }

    private void ApplyLocalization() {
        Title = T("status_query.place_picker_title");
        _txtSearch.Watermark = T("status_query.place_search_placeholder");
        _btnRunSearch.Content = T("status_query.search");
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

    private void TxtSearch_KeyDown(object? sender, KeyEventArgs e) {
        if (e.Key != Key.Enter) {
            return;
        }

        e.Handled = true;
        ApplySearch();
    }

    private void BtnRunSearch_Click(object? sender, RoutedEventArgs e) {
        ApplySearch();
    }

    private void BtnClearSearch_Click(object? sender, RoutedEventArgs e) {
        _txtSearch.Text = string.Empty;
        _activeKeyword = null;
        RenderOptions();
    }

    private void BtnClearAll_Click(object? sender, RoutedEventArgs e) {
        _selectedPlaceIds.Clear();
        RenderOptions();
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e) {
        Close(false);
    }

    private void BtnApply_Click(object? sender, RoutedEventArgs e) {
        Close(true);
    }

    private void RenderOptions() {
        _placeOptionHost.Children.Clear();

        var keyword = _activeKeyword;
        var filtered = string.IsNullOrWhiteSpace(keyword)
            ? BuildDefaultVisibleOptions()
            : _allOptions.Where(option =>
                option.AddressId.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(option.Name) && option.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(option.NameChn) && option.NameChn.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            )
            .Take(MaxVisibleOptionsWithSearch)
            .ToList();

        foreach (var option in filtered) {
            var checkBox = new CheckBox {
                Tag = option.AddressId,
                IsChecked = _selectedPlaceIds.Contains(option.AddressId)
            };
            checkBox.Content = BuildOptionContent(option);
            checkBox.IsCheckedChanged += OptionCheckBox_Changed;
            _placeOptionHost.Children.Add(checkBox);
        }

        if (filtered.Count == 0) {
            _placeOptionHost.Children.Add(new TextBlock {
                Text = T("status_query.place_picker_no_match"),
                Foreground = Brushes.DimGray
            });
        }

        UpdateSummary(filtered.Count);
    }

    private void OptionCheckBox_Changed(object? sender, RoutedEventArgs e) {
        if (sender is not CheckBox checkBox || checkBox.Tag is not int addressId) {
            return;
        }

        if (checkBox.IsChecked == true) {
            _selectedPlaceIds.Add(addressId);
        } else {
            _selectedPlaceIds.Remove(addressId);
        }

        UpdateSummary(GetVisibleCount());
    }

    private int GetVisibleCount() {
        var keyword = _activeKeyword;
        return string.IsNullOrWhiteSpace(keyword)
            ? BuildDefaultVisibleOptions().Count
            : _allOptions.Count(option =>
                option.AddressId.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(option.Name) && option.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(option.NameChn) && option.NameChn.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
    }

    private void ApplySearch() {
        _activeKeyword = NormalizeKeyword(_txtSearch.Text);
        RenderOptions();
    }

    private List<PlaceOption> BuildDefaultVisibleOptions() {
        var selected = _allOptions
            .Where(option => _selectedPlaceIds.Contains(option.AddressId))
            .ToList();

        var remainingSlots = Math.Max(0, MaxVisibleOptionsWithoutSearch - selected.Count);
        if (remainingSlots == 0) {
            return selected;
        }

        return selected
            .Concat(_allOptions.Where(option => !_selectedPlaceIds.Contains(option.AddressId)).Take(remainingSlots))
            .ToList();
    }

    private static string? NormalizeKeyword(string? keyword) {
        return string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
    }

    private static Control BuildOptionContent(PlaceOption option) {
        var stack = new StackPanel {
            Spacing = 2
        };

        stack.Children.Add(new TextBlock {
            Text = option.DisplayLabel
        });

        if (!string.IsNullOrWhiteSpace(option.DetailLabel)) {
            stack.Children.Add(new TextBlock {
                Text = option.DetailLabel,
                Foreground = Brushes.DimGray,
                FontSize = 11
            });
        }

        return stack;
    }

    private void UpdateSummary(int? visibleCount = null) {
        _txtSummary.Text = string.Format(T("status_query.place_picker_summary"), _selectedPlaceIds.Count, _allOptions.Count);
        _txtSelectionHint.Text = visibleCount.HasValue
            ? string.Format(T("status_query.picker_visible"), visibleCount.Value)
            : string.Empty;
    }

    private string T(string key) => _localizationService.Get(key);
}
