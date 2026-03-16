using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
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
    private Button _btnSelectVisible = null!;
    private Button _btnInvertVisible = null!;
    private StackPanel _placeOptionHost = null!;
    private TextBlock _txtSelectionHint = null!;
    private Button _btnClearAll = null!;
    private Button _btnCancel = null!;
    private Button _btnApply = null!;
    private string? _activeKeyword;
    private List<PlaceOption> _visibleOptions = new();
    private int? _lastToggledAddressId;
    private List<int>? _stickyVisibleAddressIds;

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
        _btnSelectVisible = this.FindControl<Button>("BtnSelectVisible") ?? throw new InvalidOperationException("BtnSelectVisible not found.");
        _btnInvertVisible = this.FindControl<Button>("BtnInvertVisible") ?? throw new InvalidOperationException("BtnInvertVisible not found.");
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
        _btnSelectVisible.Content = T("status_query.select_visible");
        _btnInvertVisible.Content = T("status_query.invert_visible");
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
        _stickyVisibleAddressIds = null;
        RenderOptions();
    }

    private void BtnClearAll_Click(object? sender, RoutedEventArgs e) {
        _selectedPlaceIds.Clear();
        _stickyVisibleAddressIds = null;
        RenderOptions();
    }

    private void BtnSelectVisible_Click(object? sender, RoutedEventArgs e) {
        if (_visibleOptions.Count == 0) {
            return;
        }

        _stickyVisibleAddressIds = _visibleOptions.Select(option => option.AddressId).ToList();
        foreach (var addressId in _visibleOptions.Select(option => option.AddressId).Distinct()) {
            _selectedPlaceIds.Add(addressId);
        }

        RenderOptions();
    }

    private void BtnInvertVisible_Click(object? sender, RoutedEventArgs e) {
        if (_visibleOptions.Count == 0) {
            return;
        }

        _stickyVisibleAddressIds = _visibleOptions.Select(option => option.AddressId).ToList();
        var visibleAddressIds = _visibleOptions
            .Select(option => option.AddressId)
            .Distinct()
            .ToList();
        var selectedBeforeInvert = new HashSet<int>(_selectedPlaceIds);

        foreach (var addressId in visibleAddressIds) {
            if (selectedBeforeInvert.Contains(addressId)) {
                _selectedPlaceIds.Remove(addressId);
            } else {
                _selectedPlaceIds.Add(addressId);
            }
        }

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
        _visibleOptions = (string.IsNullOrWhiteSpace(keyword)
            ? BuildVisibleOptionsWithoutSearch()
            : _allOptions.Where(option =>
                option.AddressId.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(option.Name) && option.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(option.NameChn) && option.NameChn.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            )
            .Take(MaxVisibleOptionsWithSearch)
            .ToList());

        foreach (var option in _visibleOptions) {
            var row = new Border {
                Background = Brushes.Transparent,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(4, 3),
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            var grid = new Grid {
                ColumnDefinitions = new ColumnDefinitions("Auto,*")
            };

            var checkBox = new CheckBox {
                Tag = option.AddressId,
                IsChecked = _selectedPlaceIds.Contains(option.AddressId),
                VerticalAlignment = VerticalAlignment.Top,
                IsHitTestVisible = false,
                Focusable = false
            };

            var content = BuildOptionContent(option);
            Grid.SetColumn(content, 1);

            grid.Children.Add(checkBox);
            grid.Children.Add(content);
            row.Child = grid;
            row.PointerReleased += (_, e) => HandleOptionPointerReleased(option, e);
            _placeOptionHost.Children.Add(row);
        }

        if (_visibleOptions.Count == 0) {
            _placeOptionHost.Children.Add(new TextBlock {
                Text = T("status_query.place_picker_no_match"),
                Foreground = Brushes.DimGray
            });
        }

        UpdateSummary(_visibleOptions.Count);
    }

    private void HandleOptionPointerReleased(PlaceOption option, PointerReleasedEventArgs e) {
        var newState = !_selectedPlaceIds.Contains(option.AddressId);
        _stickyVisibleAddressIds = _visibleOptions.Select(item => item.AddressId).ToList();
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && _lastToggledAddressId.HasValue) {
            ApplyShiftRangeSelection(option.AddressId, newState);
        } else {
            SetPlaceSelection(option.AddressId, newState);
        }

        _lastToggledAddressId = option.AddressId;
        RenderOptions();
        e.Handled = true;
    }

    private void ApplyShiftRangeSelection(int targetAddressId, bool isSelected) {
        var targetIndex = _visibleOptions.FindIndex(option => option.AddressId == targetAddressId);
        var anchorIndex = _visibleOptions.FindIndex(option => option.AddressId == _lastToggledAddressId);
        if (targetIndex < 0 || anchorIndex < 0) {
            SetPlaceSelection(targetAddressId, isSelected);
            return;
        }

        var start = Math.Min(anchorIndex, targetIndex);
        var end = Math.Max(anchorIndex, targetIndex);
        for (var i = start; i <= end; i++) {
            SetPlaceSelection(_visibleOptions[i].AddressId, isSelected);
        }
    }

    private void SetPlaceSelection(int addressId, bool isSelected) {
        if (isSelected) {
            _selectedPlaceIds.Add(addressId);
        } else {
            _selectedPlaceIds.Remove(addressId);
        }
    }

    private int GetVisibleCount() {
        var keyword = _activeKeyword;
        return string.IsNullOrWhiteSpace(keyword)
            ? BuildVisibleOptionsWithoutSearch().Count
            : _allOptions.Count(option =>
                option.AddressId.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(option.Name) && option.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(option.NameChn) && option.NameChn.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
    }

    private void ApplySearch() {
        _activeKeyword = NormalizeKeyword(_txtSearch.Text);
        _stickyVisibleAddressIds = null;
        RenderOptions();
    }

    private List<PlaceOption> BuildVisibleOptionsWithoutSearch() {
        if (_stickyVisibleAddressIds is { Count: > 0 }) {
            return _stickyVisibleAddressIds
                .Select(addressId => _allOptions.FirstOrDefault(option => option.AddressId == addressId))
                .Where(option => option is not null)
                .Cast<PlaceOption>()
                .ToList();
        }

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
        var smallFontSize = Application.Current?.Resources.TryGetValue("AppSmallFontSize", out var resource) == true &&
            resource is double value
            ? value
            : 11d;

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
                FontSize = smallFontSize
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
