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

public partial class DynastyPickerWindow : Window {
    private readonly AppLocalizationService _localizationService;
    private readonly ObservableCollection<DynastyOptionRow> _rows = new();
    private readonly HashSet<int> _selectedDynastyIds;

    private TextBlock _txtSummary = null!;
    private ItemsControl _dynastyOptionHost = null!;
    private Button _btnSelectAll = null!;
    private Button _btnClearAll = null!;
    private Button _btnCancel = null!;
    private Button _btnApply = null!;

    public DynastyPickerWindow() : this(new AppLocalizationService(), Array.Empty<DynastyOption>()) {
    }

    public DynastyPickerWindow(
        AppLocalizationService localizationService,
        IReadOnlyList<DynastyOption> options,
        IReadOnlyCollection<int>? selectedDynastyIds = null
    ) {
        _localizationService = localizationService;
        _selectedDynastyIds = selectedDynastyIds is null
            ? new HashSet<int>()
            : new HashSet<int>(selectedDynastyIds);

        foreach (var option in options) {
            _rows.Add(new DynastyOptionRow(option, _selectedDynastyIds.Contains(option.DynastyId)));
        }

        InitializeComponent();
        InitializeControls();
        _localizationService.LanguageChanged += HandleLanguageChanged;
        Closed += (_, _) => _localizationService.LanguageChanged -= HandleLanguageChanged;

        ApplyLocalization();
        UpdateSummary();
    }

    public IReadOnlyList<int> SelectedDynastyIds => _rows.Where(row => row.IsSelected).Select(row => row.DynastyId).OrderBy(id => id).ToList();

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls() {
        _txtSummary = this.FindControl<TextBlock>("TxtSummary") ?? throw new InvalidOperationException("TxtSummary not found.");
        _dynastyOptionHost = this.FindControl<ItemsControl>("DynastyOptionHost") ?? throw new InvalidOperationException("DynastyOptionHost not found.");
        _btnSelectAll = this.FindControl<Button>("BtnSelectAll") ?? throw new InvalidOperationException("BtnSelectAll not found.");
        _btnClearAll = this.FindControl<Button>("BtnClearAll") ?? throw new InvalidOperationException("BtnClearAll not found.");
        _btnCancel = this.FindControl<Button>("BtnCancel") ?? throw new InvalidOperationException("BtnCancel not found.");
        _btnApply = this.FindControl<Button>("BtnApply") ?? throw new InvalidOperationException("BtnApply not found.");
        _dynastyOptionHost.ItemsSource = _rows;
    }

    private void ApplyLocalization() {
        Title = T("dynasty_picker.title");
        _btnSelectAll.Content = T("dynasty_picker.select_all");
        _btnClearAll.Content = T("dynasty_picker.clear_all");
        _btnCancel.Content = T("dynasty_picker.cancel");
        _btnApply.Content = T("dynasty_picker.apply");
    }

    private void HandleLanguageChanged(object? sender, EventArgs e) {
        ApplyLocalization();
        UpdateSummary();
    }

    private void DynastyOptionRow_PointerReleased(object? sender, PointerReleasedEventArgs e) {
        if (sender is not Control control || control.DataContext is not DynastyOptionRow row) {
            return;
        }

        row.IsSelected = !row.IsSelected;
        UpdateSummary();
        e.Handled = true;
    }

    private void BtnSelectAll_Click(object? sender, RoutedEventArgs e) {
        foreach (var row in _rows) {
            row.IsSelected = true;
        }

        UpdateSummary();
    }

    private void BtnClearAll_Click(object? sender, RoutedEventArgs e) {
        foreach (var row in _rows) {
            row.IsSelected = false;
        }

        UpdateSummary();
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e) {
        Close(false);
    }

    private void BtnApply_Click(object? sender, RoutedEventArgs e) {
        Close(true);
    }

    private void UpdateSummary() {
        _txtSummary.Text = string.Format(
            T("dynasty_picker.summary"),
            _rows.Count(row => row.IsSelected),
            _rows.Count
        );
    }

    private string T(string key) => _localizationService.Get(key);

    private sealed class DynastyOptionRow : INotifyPropertyChanged {
        private bool _isSelected;

        public DynastyOptionRow(DynastyOption option, bool isSelected) {
            DynastyId = option.DynastyId;
            DisplayLabel = option.DisplayLabel;
            _isSelected = isSelected;
        }

        public int DynastyId { get; }
        public string DisplayLabel { get; }

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
