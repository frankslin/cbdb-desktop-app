using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Core;

namespace Cbdb.App.Avalonia.Modules;

public partial class StatusCodePickerWindow : Window {
    private readonly AppLocalizationService _localizationService;
    private readonly StatusPickerData _pickerData;
    private readonly List<StatusCodeOption> _allOptions;
    private readonly Dictionary<string, StatusCodeOption> _optionByCode;
    private readonly HashSet<string> _selectedCodes;
    private readonly HashSet<string> _expandedTypeCodes = new(StringComparer.OrdinalIgnoreCase);

    private TextBox _txtSearch = null!;
    private Button _btnFind = null!;
    private Button _btnFindNext = null!;
    private TextBlock _txtCategoryHeader = null!;
    private TreeView _typeTreeView = null!;
    private Button _btnSelectVisible = null!;
    private Button _btnClearVisible = null!;
    private TextBlock _txtCurrentType = null!;
    private TextBlock _txtSummary = null!;
    private StackPanel _statusOptionHost = null!;
    private TextBlock _txtSelectionHint = null!;
    private Button _btnClearAll = null!;
    private Button _btnCancel = null!;
    private Button _btnApply = null!;
    private StatusTypeNode _activeTypeNode = new(StatusPickerData.RootCode, null, null, Array.Empty<StatusTypeNode>(), Array.Empty<string>());
    private string? _highlightedStatusCode;
    private List<string> _searchMatches = new();
    private int _searchMatchIndex = -1;
    private bool _preserveHighlightOnTreeSelection;

    public StatusCodePickerWindow()
        : this(
            new AppLocalizationService(),
            new StatusPickerData(
                new StatusTypeNode(StatusPickerData.RootCode, null, null, Array.Empty<StatusTypeNode>(), Array.Empty<string>()),
                Array.Empty<StatusCodeOption>(),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            )
        ) {
    }

    public StatusCodePickerWindow(
        AppLocalizationService localizationService,
        StatusPickerData pickerData,
        IReadOnlyCollection<string>? selectedCodes = null
    ) {
        _localizationService = localizationService;
        _pickerData = pickerData;
        _allOptions = pickerData.AllStatusCodes.ToList();
        _optionByCode = _allOptions.ToDictionary(option => option.Code, StringComparer.OrdinalIgnoreCase);
        _selectedCodes = selectedCodes is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(selectedCodes, StringComparer.OrdinalIgnoreCase);
        _activeTypeNode = _pickerData.Root;
        foreach (var node in _pickerData.Root.Children.Where(node => node.Children.Count > 0)) {
            _expandedTypeCodes.Add(node.Code);
        }

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
        _btnFind = this.FindControl<Button>("BtnFind") ?? throw new InvalidOperationException("BtnFind not found.");
        _btnFindNext = this.FindControl<Button>("BtnFindNext") ?? throw new InvalidOperationException("BtnFindNext not found.");
        _txtCategoryHeader = this.FindControl<TextBlock>("TxtCategoryHeader") ?? throw new InvalidOperationException("TxtCategoryHeader not found.");
        _typeTreeView = this.FindControl<TreeView>("TypeTreeView") ?? throw new InvalidOperationException("TypeTreeView not found.");
        _typeTreeView.SelectionChanged += TypeTreeView_SelectionChanged;
        _btnSelectVisible = this.FindControl<Button>("BtnSelectVisible") ?? throw new InvalidOperationException("BtnSelectVisible not found.");
        _btnClearVisible = this.FindControl<Button>("BtnClearVisible") ?? throw new InvalidOperationException("BtnClearVisible not found.");
        _txtCurrentType = this.FindControl<TextBlock>("TxtCurrentType") ?? throw new InvalidOperationException("TxtCurrentType not found.");
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
        _btnFind.Content = T("status_query.search");
        _btnFindNext.Content = T("status_query.find_next");
        _txtCategoryHeader.Text = T("status_query.category_tree");
        _btnSelectVisible.Content = T("status_query.select_all");
        _btnClearVisible.Content = T("status_query.clear_visible");
        _btnClearAll.Content = T("status_query.clear_all");
        _btnCancel.Content = T("status_query.cancel");
        _btnApply.Content = T("status_query.apply");
        RenderTypeTree();
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
        ResetSearchState();
        RunSearch(moveNext: false);
    }

    private void BtnFind_Click(object? sender, RoutedEventArgs e) {
        RunSearch(moveNext: false);
    }

    private void BtnFindNext_Click(object? sender, RoutedEventArgs e) {
        RunSearch(moveNext: true);
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

    private void BtnSelectVisible_Click(object? sender, RoutedEventArgs e) {
        var visibleOptions = GetVisibleOptions();
        if (_activeTypeNode.IsRoot || visibleOptions.Count == 0) {
            return;
        }

        var shouldSelectAll = visibleOptions.Any(option => !_selectedCodes.Contains(option.Code));
        foreach (var option in visibleOptions) {
            if (shouldSelectAll) {
                _selectedCodes.Add(option.Code);
            } else {
                _selectedCodes.Remove(option.Code);
            }
        }

        RenderOptions();
    }

    private void BtnClearVisible_Click(object? sender, RoutedEventArgs e) {
        if (_activeTypeNode.IsRoot) {
            return;
        }

        foreach (var option in GetVisibleOptions()) {
            _selectedCodes.Remove(option.Code);
        }

        RenderOptions();
    }

    private void RenderTypeTree() {
        _typeTreeView.ItemsSource = _pickerData.Root.Children.Select(BuildTreeItem).ToList();
    }

    private TreeViewItem BuildTreeItem(StatusTypeNode node) {
        var item = new TreeViewItem {
            Header = GetTypeNodeLabel(node),
            Tag = node,
            IsExpanded = _expandedTypeCodes.Contains(node.Code) || IsDescendantOfActiveNode(node),
            IsSelected = string.Equals(_activeTypeNode.Code, node.Code, StringComparison.OrdinalIgnoreCase)
        };

        item.Expanded += TreeItem_Expanded;
        item.Collapsed += TreeItem_Collapsed;

        if (node.Children.Count > 0) {
            item.ItemsSource = node.Children.Select(BuildTreeItem).ToList();
        }

        return item;
    }

    private void TypeTreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e) {
        if (_typeTreeView.SelectedItem is not TreeViewItem { Tag: StatusTypeNode node }) {
            return;
        }

        _activeTypeNode = node;
        if (!_preserveHighlightOnTreeSelection) {
            _highlightedStatusCode = null;
        }
        if (node.Children.Count > 0) {
            _expandedTypeCodes.Add(node.Code);
        }

        RenderOptions();
    }

    private void TreeItem_Expanded(object? sender, RoutedEventArgs e) {
        if (sender is TreeViewItem { Tag: StatusTypeNode node }) {
            _expandedTypeCodes.Add(node.Code);
        }
    }

    private void TreeItem_Collapsed(object? sender, RoutedEventArgs e) {
        if (sender is TreeViewItem { Tag: StatusTypeNode node }) {
            _expandedTypeCodes.Remove(node.Code);
        }
    }

    private void RenderOptions() {
        _statusOptionHost.Children.Clear();
        _txtCurrentType.Text = GetTypeNodeLabel(_activeTypeNode);

        var visibleOptions = GetVisibleOptions();
        Control? highlightedRow = null;
        _btnSelectVisible.Content = visibleOptions.Count > 0 && visibleOptions.All(option => _selectedCodes.Contains(option.Code))
            ? T("entry_query.deselect_all")
            : T("status_query.select_all");

        foreach (var option in visibleOptions) {
            var row = new Border {
                Background = string.Equals(option.Code, _highlightedStatusCode, StringComparison.OrdinalIgnoreCase)
                    ? new SolidColorBrush(Color.Parse("#FFF3BF"))
                    : Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.Parse("#E0E0E0")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(4, 3),
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            var grid = new Grid {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,*,Auto")
            };

            var checkBox = new CheckBox {
                Tag = option.Code,
                IsChecked = _selectedCodes.Contains(option.Code),
                VerticalAlignment = VerticalAlignment.Center
            };
            checkBox.IsCheckedChanged += OptionCheckBox_Changed;
            row.PointerPressed += (_, e) => {
                if (e.Source is CheckBox) {
                    return;
                }

                checkBox.IsChecked = checkBox.IsChecked != true;
                e.Handled = true;
            };

            var descText = new TextBlock {
                Text = option.Description ?? option.Code,
                Margin = new Thickness(8, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var descChnText = new TextBlock {
                Text = option.DescriptionChn ?? string.Empty,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var usageText = new TextBlock {
                Text = $"[{option.UsageCount:N0}]",
                Foreground = Brushes.DimGray,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(checkBox, 0);
            Grid.SetColumn(descText, 1);
            Grid.SetColumn(descChnText, 2);
            Grid.SetColumn(usageText, 3);
            grid.Children.Add(checkBox);
            grid.Children.Add(descText);
            grid.Children.Add(descChnText);
            grid.Children.Add(usageText);
            row.Child = grid;
            _statusOptionHost.Children.Add(row);
            if (string.Equals(option.Code, _highlightedStatusCode, StringComparison.OrdinalIgnoreCase)) {
                highlightedRow = row;
            }
        }

        if (visibleOptions.Count == 0) {
            _statusOptionHost.Children.Add(new TextBlock {
                Text = T("status_query.picker_no_match"),
                Foreground = Brushes.DimGray
            });
        }

        _btnSelectVisible.IsEnabled = !_activeTypeNode.IsRoot && visibleOptions.Count > 0;
        _btnClearVisible.IsEnabled = !_activeTypeNode.IsRoot && visibleOptions.Count > 0;
        _btnFindNext.IsEnabled = _searchMatches.Count > 1;

        UpdateSummary(visibleOptions.Count);
        if (highlightedRow is not null) {
            Dispatcher.UIThread.Post(() => highlightedRow.BringIntoView());
        }
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

        UpdateSummary(GetVisibleOptions().Count);
    }

    private List<StatusCodeOption> GetVisibleOptions() =>
        _activeTypeNode.StatusCodes
            .Select(code => _optionByCode.TryGetValue(code, out var option) ? option : null)
            .Where(option => option is not null)
            .Cast<StatusCodeOption>()
            .ToList();

    private void RunSearch(bool moveNext) {
        var keyword = _txtSearch.Text?.Trim();
        if (string.IsNullOrWhiteSpace(keyword)) {
            _highlightedStatusCode = null;
            RenderOptions();
            return;
        }

        if (!moveNext || _searchMatches.Count == 0) {
            _searchMatches = _allOptions
                .Where(option =>
                    option.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(option.Description) && option.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(option.DescriptionChn) && option.DescriptionChn.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                )
                .Select(option => option.Code)
                .ToList();
            _searchMatchIndex = -1;
        }

        if (_searchMatches.Count == 0) {
            _highlightedStatusCode = null;
            RenderOptions();
            _txtSelectionHint.Text = T("status_query.search_no_match");
            return;
        }

        _searchMatchIndex = (_searchMatchIndex + 1) % _searchMatches.Count;
        ActivateSearchMatch(_searchMatches[_searchMatchIndex]);
    }

    private void ActivateSearchMatch(string statusCode) {
        _highlightedStatusCode = statusCode;

        if (_pickerData.StatusCodeToTypeCode.TryGetValue(statusCode, out var typeCode)) {
            var matchedNode = FindTypeNode(_pickerData.Root, typeCode);
            if (matchedNode is not null) {
                _activeTypeNode = matchedNode;
                if (typeCode.Length > 2) {
                    _expandedTypeCodes.Add(typeCode[..2]);
                }
            }
        }

        _preserveHighlightOnTreeSelection = true;
        RenderTypeTree();
        _preserveHighlightOnTreeSelection = false;
        RenderOptions();
        _txtSelectionHint.Text = string.Format(
            T("status_query.search_result"),
            _searchMatchIndex + 1,
            _searchMatches.Count
        );
    }

    private void ResetSearchState() {
        _searchMatches.Clear();
        _searchMatchIndex = -1;
        _highlightedStatusCode = null;
        _btnFindNext.IsEnabled = false;
        _txtSelectionHint.Text = string.Empty;
    }

    private bool IsDescendantOfActiveNode(StatusTypeNode topNode) =>
        string.Equals(_activeTypeNode.Code, topNode.Code, StringComparison.OrdinalIgnoreCase)
        || topNode.Children.Any(child => string.Equals(child.Code, _activeTypeNode.Code, StringComparison.OrdinalIgnoreCase));

    private static StatusTypeNode? FindTypeNode(StatusTypeNode currentNode, string typeCode) {
        if (string.Equals(currentNode.Code, typeCode, StringComparison.OrdinalIgnoreCase)) {
            return currentNode;
        }

        foreach (var child in currentNode.Children) {
            var match = FindTypeNode(child, typeCode);
            if (match is not null) {
                return match;
            }
        }

        return null;
    }

    private string GetTypeNodeLabel(StatusTypeNode node) {
        if (node.IsRoot) {
            return T("status_query.category_root");
        }

        return string.IsNullOrWhiteSpace(node.DescriptionChn)
            ? node.Description ?? node.Code
            : string.IsNullOrWhiteSpace(node.Description)
                ? node.DescriptionChn
                : $"{node.DescriptionChn} / {node.Description}";
    }

    private void UpdateSummary(int? visibleCount = null) {
        var count = _selectedCodes.Count;
        _txtSummary.Text = string.Format(T("status_query.picker_summary"), count, _allOptions.Count);
        if (_searchMatches.Count > 0 && _searchMatchIndex >= 0) {
            _txtSelectionHint.Text = string.Format(
                T("status_query.search_result"),
                _searchMatchIndex + 1,
                _searchMatches.Count
            );
            return;
        }

        _txtSelectionHint.Text = visibleCount.HasValue
            ? string.Format(T("status_query.picker_visible"), visibleCount.Value)
            : string.Empty;
    }

    private string T(string key) => _localizationService.Get(key);
}
