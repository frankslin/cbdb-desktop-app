using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Core;

namespace Cbdb.App.Avalonia.Modules;

public partial class PlacePickerWindow : Window {
    private const int MaxVisibleOptionsWithoutSearch = 300;
    private const int MaxVisibleOptionsWithSearch = 500;

    private readonly AppLocalizationService _localizationService;
    private readonly List<PlaceOption> _allOptions;
    private readonly Dictionary<int, PlaceOptionRow> _rowsByAddressId;
    private readonly ObservableCollection<PlaceOptionRow> _visibleRows = new();
    private readonly HashSet<int> _selectedPlaceIds;

    private TextBox _txtSearch = null!;
    private Button _btnRunSearch = null!;
    private Button _btnClearSearch = null!;
    private TextBlock _txtSummary = null!;
    private Button _btnSelectVisible = null!;
    private Button _btnInvertVisible = null!;
    private ItemsControl _placeOptionHost = null!;
    private Button _btnClearAll = null!;
    private Button _btnCancel = null!;
    private Button _btnApply = null!;
    private string? _activeKeyword;
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
        _rowsByAddressId = _allOptions
            .GroupBy(option => option.AddressId)
            .ToDictionary(
                group => group.Key,
                group => new PlaceOptionRow(group.First(), _selectedPlaceIds.Contains(group.Key))
            );

        InitializeComponent();
        InitializeControls();

        _localizationService.LanguageChanged += HandleLanguageChanged;
        Closed += (_, _) => _localizationService.LanguageChanged -= HandleLanguageChanged;

        ApplyLocalization();
        RefreshVisibleRows();
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
        _placeOptionHost = this.FindControl<ItemsControl>("PlaceOptionHost") ?? throw new InvalidOperationException("PlaceOptionHost not found.");
        _btnClearAll = this.FindControl<Button>("BtnClearAll") ?? throw new InvalidOperationException("BtnClearAll not found.");
        _btnCancel = this.FindControl<Button>("BtnCancel") ?? throw new InvalidOperationException("BtnCancel not found.");
        _btnApply = this.FindControl<Button>("BtnApply") ?? throw new InvalidOperationException("BtnApply not found.");
        _placeOptionHost.ItemsSource = _visibleRows;
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
        RefreshVisibleRows();
    }

    private void BtnClearAll_Click(object? sender, RoutedEventArgs e) {
        _selectedPlaceIds.Clear();
        _stickyVisibleAddressIds = null;
        SetRowsSelectionState(_rowsByAddressId.Values, false);
        RefreshVisibleRows();
    }

    private void BtnSelectVisible_Click(object? sender, RoutedEventArgs e) {
        if (_visibleRows.Count == 0) {
            return;
        }

        _stickyVisibleAddressIds = _visibleRows.Select(row => row.AddressId).ToList();
        foreach (var row in _visibleRows) {
            _selectedPlaceIds.Add(row.AddressId);
            row.IsSelected = true;
        }

        UpdateSummary(_visibleRows.Count);
    }

    private void BtnInvertVisible_Click(object? sender, RoutedEventArgs e) {
        if (_visibleRows.Count == 0) {
            return;
        }

        _stickyVisibleAddressIds = _visibleRows.Select(row => row.AddressId).ToList();
        foreach (var row in _visibleRows) {
            var isSelected = !_selectedPlaceIds.Contains(row.AddressId);
            SetPlaceSelection(row.AddressId, isSelected);
        }

        UpdateSummary(_visibleRows.Count);
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e) {
        Close(false);
    }

    private void BtnApply_Click(object? sender, RoutedEventArgs e) {
        Close(true);
    }

    private void PlaceOptionRow_PointerReleased(object? sender, PointerReleasedEventArgs e) {
        if (sender is not Control control || control.DataContext is not PlaceOptionRow row) {
            return;
        }

        var newState = !_selectedPlaceIds.Contains(row.AddressId);
        _stickyVisibleAddressIds = _visibleRows.Select(item => item.AddressId).ToList();

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && _lastToggledAddressId.HasValue) {
            ApplyShiftRangeSelection(row.AddressId, newState);
        } else {
            SetPlaceSelection(row.AddressId, newState);
        }

        _lastToggledAddressId = row.AddressId;
        UpdateSummary(_visibleRows.Count);
        e.Handled = true;
    }

    private void ApplyShiftRangeSelection(int targetAddressId, bool isSelected) {
        var targetIndex = FindVisibleIndex(targetAddressId);
        var anchorIndex = _lastToggledAddressId.HasValue ? FindVisibleIndex(_lastToggledAddressId.Value) : -1;
        if (targetIndex < 0 || anchorIndex < 0) {
            SetPlaceSelection(targetAddressId, isSelected);
            return;
        }

        var start = Math.Min(anchorIndex, targetIndex);
        var end = Math.Max(anchorIndex, targetIndex);
        for (var i = start; i <= end; i++) {
            SetPlaceSelection(_visibleRows[i].AddressId, isSelected);
        }
    }

    private int FindVisibleIndex(int addressId) {
        for (var i = 0; i < _visibleRows.Count; i++) {
            if (_visibleRows[i].AddressId == addressId) {
                return i;
            }
        }

        return -1;
    }

    private void SetPlaceSelection(int addressId, bool isSelected) {
        if (isSelected) {
            _selectedPlaceIds.Add(addressId);
        } else {
            _selectedPlaceIds.Remove(addressId);
        }

        if (_rowsByAddressId.TryGetValue(addressId, out var row)) {
            row.IsSelected = isSelected;
        }
    }

    private void SetRowsSelectionState(IEnumerable<PlaceOptionRow> rows, bool isSelected) {
        foreach (var row in rows) {
            row.IsSelected = isSelected;
        }
    }

    private void ApplySearch() {
        _activeKeyword = NormalizeKeyword(_txtSearch.Text);
        _stickyVisibleAddressIds = null;
        RefreshVisibleRows();
    }

    private void RefreshVisibleRows() {
        var visibleRows = BuildVisibleRows().ToList();

        _visibleRows.Clear();
        foreach (var row in visibleRows) {
            _visibleRows.Add(row);
        }

        UpdateSummary(_visibleRows.Count);
    }

    private IEnumerable<PlaceOptionRow> BuildVisibleRows() {
        var keyword = _activeKeyword;
        if (string.IsNullOrWhiteSpace(keyword)) {
            return BuildVisibleRowsWithoutSearch();
        }

        return _allOptions
            .Where(option =>
                option.AddressId.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(option.Name) && option.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(option.NameChn) && option.NameChn.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            )
            .Take(MaxVisibleOptionsWithSearch)
            .Select(option => _rowsByAddressId[option.AddressId]);
    }

    private IEnumerable<PlaceOptionRow> BuildVisibleRowsWithoutSearch() {
        if (_stickyVisibleAddressIds is { Count: > 0 }) {
            return _stickyVisibleAddressIds
                .Distinct()
                .Select(addressId => _rowsByAddressId.GetValueOrDefault(addressId))
                .Where(row => row is not null)
                .Cast<PlaceOptionRow>();
        }

        var selectedRows = _allOptions
            .Select(option => _rowsByAddressId[option.AddressId])
            .Where(row => row.IsSelected)
            .Distinct()
            .ToList();

        var remainingSlots = Math.Max(0, MaxVisibleOptionsWithoutSearch - selectedRows.Count);
        if (remainingSlots == 0) {
            return selectedRows;
        }

        return selectedRows
            .Concat(_allOptions
                .Select(option => _rowsByAddressId[option.AddressId])
                .Where(row => !row.IsSelected)
                .Distinct()
                .Take(remainingSlots));
    }

    private static string? NormalizeKeyword(string? keyword) {
        return string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
    }

    private void UpdateSummary(int? visibleCount = null) {
        _txtSummary.Text = string.Format(
            T("status_query.place_picker_summary"),
            _selectedPlaceIds.Count,
            visibleCount ?? _visibleRows.Count,
            _allOptions.Count
        );
    }

    private string T(string key) => _localizationService.Get(key);

    public sealed class PlaceOptionRow : INotifyPropertyChanged {
        private bool _isSelected;

        public PlaceOptionRow(PlaceOption option, bool isSelected) {
            AddressId = option.AddressId;
            DisplayLabel = option.DisplayLabel;
            DetailLabel = option.DetailLabel;
            HasDetailLabel = !string.IsNullOrWhiteSpace(DetailLabel);
            _isSelected = isSelected;
        }

        public int AddressId { get; }
        public string DisplayLabel { get; }
        public string DetailLabel { get; }
        public bool HasDetailLabel { get; }

        public bool IsSelected {
            get => _isSelected;
            set {
                if (_isSelected == value) {
                    return;
                }

                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
