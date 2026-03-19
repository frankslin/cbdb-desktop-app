using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Core;

namespace Cbdb.App.Avalonia.Modules;

public partial class EntryCodePickerWindow : Window {
    private static readonly object UnloadedChildPlaceholder = new();

    private readonly AppLocalizationService _localizationService;
    private readonly EntryPickerData _pickerData;
    private readonly List<EntryCodeOption> _allOptions;
    private readonly Dictionary<string, EntryCodeOption> _optionByCode;
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
    private StackPanel _entryOptionHost = null!;
    private TextBlock _txtSelectionHint = null!;
    private Button _btnClearAll = null!;
    private Button _btnCancel = null!;
    private Button _btnApply = null!;
    private EntryTypeNode _activeTypeNode = new(EntryPickerData.RootCode, null, null, Array.Empty<EntryTypeNode>(), Array.Empty<string>());
    private string? _highlightedEntryCode;
    private readonly QueryPickerSearchState _searchState = new();
    private bool _preserveHighlightOnTreeSelection;

    public EntryCodePickerWindow()
        : this(
            new AppLocalizationService(),
            new EntryPickerData(
                new EntryTypeNode(EntryPickerData.RootCode, null, null, Array.Empty<EntryTypeNode>(), Array.Empty<string>()),
                Array.Empty<EntryCodeOption>(),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            )
        ) {
    }

    public EntryCodePickerWindow(AppLocalizationService localizationService, EntryPickerData pickerData, IReadOnlyCollection<string>? selectedCodes = null) {
        _localizationService = localizationService;
        _pickerData = pickerData;
        _allOptions = pickerData.AllEntryCodes.ToList();
        _optionByCode = _allOptions.ToDictionary(option => option.Code, StringComparer.OrdinalIgnoreCase);
        _selectedCodes = selectedCodes is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(selectedCodes, StringComparer.OrdinalIgnoreCase);
        _activeTypeNode = _pickerData.Root;

        InitializeComponent();
        InitializeControls();

        _localizationService.LanguageChanged += HandleLanguageChanged;
        Closed += (_, _) => _localizationService.LanguageChanged -= HandleLanguageChanged;

        ApplyLocalization();
        RenderOptions();
    }

    public IReadOnlyList<string> SelectedCodes => _selectedCodes.OrderBy(code => code, StringComparer.OrdinalIgnoreCase).ToList();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

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
        _entryOptionHost = this.FindControl<StackPanel>("EntryOptionHost") ?? throw new InvalidOperationException("EntryOptionHost not found.");
        _txtSelectionHint = this.FindControl<TextBlock>("TxtSelectionHint") ?? throw new InvalidOperationException("TxtSelectionHint not found.");
        _btnClearAll = this.FindControl<Button>("BtnClearAll") ?? throw new InvalidOperationException("BtnClearAll not found.");
        _btnCancel = this.FindControl<Button>("BtnCancel") ?? throw new InvalidOperationException("BtnCancel not found.");
        _btnApply = this.FindControl<Button>("BtnApply") ?? throw new InvalidOperationException("BtnApply not found.");
    }

    private void ApplyLocalization() {
        Title = T("entry_query.picker_title");
        _txtSearch.Watermark = T("entry_query.search_placeholder");
        _btnFind.Content = T("entry_query.search");
        _btnFindNext.Content = T("entry_query.find_next");
        _txtCategoryHeader.Text = T("entry_query.category_tree");
        _btnClearVisible.Content = T("entry_query.clear_visible");
        _btnClearAll.Content = T("entry_query.clear_all");
        _btnCancel.Content = T("entry_query.cancel");
        _btnApply.Content = T("entry_query.apply");
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

    private void BtnFind_Click(object? sender, RoutedEventArgs e) => RunSearch(moveNext: false);

    private void BtnFindNext_Click(object? sender, RoutedEventArgs e) => RunSearch(moveNext: true);

    private void BtnClearAll_Click(object? sender, RoutedEventArgs e) {
        _selectedCodes.Clear();
        RenderOptions();
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e) => Close(false);

    private void BtnApply_Click(object? sender, RoutedEventArgs e) => Close(true);

    private void BtnSelectVisible_Click(object? sender, RoutedEventArgs e) {
        var visibleOptions = GetVisibleOptions();
        if (visibleOptions.Count == 0) {
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

    private TreeViewItem BuildTreeItem(EntryTypeNode node) {
        var item = new TreeViewItem {
            Header = GetTypeNodeLabel(node),
            Tag = node,
            IsExpanded = _expandedTypeCodes.Contains(node.Code) || IsDescendantOfActiveNode(node),
            IsSelected = string.Equals(_activeTypeNode.Code, node.Code, StringComparison.OrdinalIgnoreCase)
        };

        item.Expanded += TreeItem_Expanded;
        item.Collapsed += TreeItem_Collapsed;
        item.PointerReleased += TreeItem_PointerReleased;

        QueryPickerTreeHelper.InitializeChildren(
            item,
            node.Children,
            _expandedTypeCodes.Contains(node.Code) || IsDescendantOfActiveNode(node),
            UnloadedChildPlaceholder,
            BuildTreeItem
        );

        return item;
    }

    private void TypeTreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e) {
        if (_typeTreeView.SelectedItem is not TreeViewItem { Tag: EntryTypeNode node }) {
            return;
        }

        _activeTypeNode = node;
        if (!_preserveHighlightOnTreeSelection) {
            _highlightedEntryCode = null;
        }
        if (node.Children.Count > 0) {
            _expandedTypeCodes.Add(node.Code);
        }

        RenderOptions();
    }

    private void TreeItem_Expanded(object? sender, RoutedEventArgs e) {
        if (sender is TreeViewItem item && item.Tag is EntryTypeNode node) {
            QueryPickerTreeHelper.ExpandNode(item, node.Code, node.Children, _expandedTypeCodes, BuildTreeItem);
        }
    }

    private void TreeItem_Collapsed(object? sender, RoutedEventArgs e) {
        if (sender is TreeViewItem { Tag: EntryTypeNode node }) {
            QueryPickerTreeHelper.CollapseNode(node.Code, _expandedTypeCodes);
        }
    }

    private void TreeItem_PointerReleased(object? sender, PointerReleasedEventArgs e) {
        QueryPickerTreeHelper.TryHandleWholeRowToggle<EntryTypeNode>(
            sender,
            e,
            _expandedTypeCodes,
            node => node.Code,
            node => node.Children,
            BuildTreeItem
        );
    }

    private void RenderOptions() {
        _entryOptionHost.Children.Clear();
        _txtCurrentType.Text = GetTypeNodeLabel(_activeTypeNode);
        var visibleOptions = GetVisibleOptions();
        Control? highlightedRow = null;
        _btnSelectVisible.Content = visibleOptions.Count > 0 && visibleOptions.All(option => _selectedCodes.Contains(option.Code))
            ? T("entry_query.deselect_all")
            : T("entry_query.select_all");

        foreach (var option in visibleOptions) {
            var isSelected = _selectedCodes.Contains(option.Code);
            var row = new Border {
                Background = string.Equals(option.Code, _highlightedEntryCode, StringComparison.OrdinalIgnoreCase)
                    ? new SolidColorBrush(Color.Parse("#FFF3BF"))
                    : Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.Parse("#E0E0E0")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(4, 3),
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            var grid = new Grid {
                ColumnDefinitions = new ColumnDefinitions("Auto,*")
            };

            var checkBox = new CheckBox {
                IsChecked = isSelected,
                VerticalAlignment = VerticalAlignment.Top
            };
            checkBox.IsCheckedChanged += (_, _) => {
                if (checkBox.IsChecked == true) {
                    _selectedCodes.Add(option.Code);
                } else {
                    _selectedCodes.Remove(option.Code);
                }

                UpdateSummary();
            };
            row.PointerPressed += (_, e) => {
                if (e.Source is CheckBox) {
                    return;
                }

                checkBox.IsChecked = checkBox.IsChecked != true;
                e.Handled = true;
            };

            var textPanel = new StackPanel {
                Margin = new Thickness(8, 0, 0, 0),
                Spacing = 2
            };
            Grid.SetColumn(textPanel, 1);
            textPanel.Children.Add(new TextBlock {
                Text = option.DisplayLabel,
                FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });
            textPanel.Children.Add(new TextBlock {
                Text = $"{T("entry_query.entry_code")}: {option.Code}    {T("browser.association_count")}: {option.UsageCount:N0}",
                Foreground = new SolidColorBrush(Color.Parse("#666666"))
            });

            grid.Children.Add(checkBox);
            grid.Children.Add(textPanel);
            row.Child = grid;
            _entryOptionHost.Children.Add(row);
            if (string.Equals(option.Code, _highlightedEntryCode, StringComparison.OrdinalIgnoreCase)) {
                highlightedRow = row;
            }
        }

        if (_entryOptionHost.Children.Count == 0) {
            _entryOptionHost.Children.Add(new TextBlock {
                Text = T("entry_query.picker_no_match"),
                Foreground = new SolidColorBrush(Color.Parse("#666666"))
            });
        }

        UpdateSummary();
        if (highlightedRow is not null) {
            Dispatcher.UIThread.Post(() => highlightedRow.BringIntoView());
        }
    }

    private IReadOnlyList<EntryCodeOption> GetVisibleOptions() {
        if (_activeTypeNode.IsRoot) {
            return _allOptions;
        }

        return _activeTypeNode.EntryCodes
            .Where(_optionByCode.ContainsKey)
            .Select(code => _optionByCode[code])
            .ToList();
    }

    private void RunSearch(bool moveNext) {
        var keyword = _txtSearch.Text?.Trim();
        if (string.IsNullOrWhiteSpace(keyword)) {
            ResetSearchState();
            _highlightedEntryCode = null;
            RenderOptions();
            return;
        }

        if (!moveNext || _searchState.Matches.Count == 0) {
            _searchState.Replace(_allOptions
                .Where(option =>
                    option.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    || (option.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (option.DescriptionChn?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
                .Select(option => option.Code)
            );
        }

        if (_searchState.Matches.Count == 0) {
            _txtSelectionHint.Text = T("entry_query.search_no_match");
            return;
        }

        var matchCode = _searchState.MoveNext();
        if (matchCode is null) {
            _txtSelectionHint.Text = T("entry_query.search_no_match");
            return;
        }
        _highlightedEntryCode = matchCode;

        if (_pickerData.EntryCodeToTypeCode.TryGetValue(matchCode, out var typeCode)) {
            var node = QueryPickerTreeHelper.FindNode(_pickerData.Root, typeCode, item => item.Code, item => item.Children);
            if (node is not null) {
                _activeTypeNode = node;
            }
        } else {
            _activeTypeNode = _pickerData.Root;
        }

        _preserveHighlightOnTreeSelection = true;
        RenderTypeTree();
        _preserveHighlightOnTreeSelection = false;
        RenderOptions();
        _txtSelectionHint.Text = string.Format(T("entry_query.search_result"), _searchState.Index + 1, _searchState.Matches.Count);
    }

    private void UpdateSummary() {
        _txtSummary.Text = string.Format(T("entry_query.picker_summary"), _selectedCodes.Count, _allOptions.Count);
        if (_searchState.HasActiveMatch) {
            _txtSelectionHint.Text = string.Format(T("entry_query.search_result"), _searchState.Index + 1, _searchState.Matches.Count);
            return;
        }

        _txtSelectionHint.Text = string.Format(T("entry_query.picker_visible"), GetVisibleOptions().Count);
    }

    private void ResetSearchState() {
        _searchState.Reset();
        _txtSelectionHint.Text = string.Empty;
    }

    private bool IsDescendantOfActiveNode(EntryTypeNode node) {
        return QueryPickerTreeHelper.ContainsDescendant(node, _activeTypeNode.Code, item => item.Code, item => item.Children);
    }

    private string GetTypeNodeLabel(EntryTypeNode node) {
        if (node.IsRoot) {
            return T("entry_query.category_root");
        }

        return string.IsNullOrWhiteSpace(node.DescriptionChn)
            ? node.Description ?? node.Code
            : string.IsNullOrWhiteSpace(node.Description)
                ? node.DescriptionChn
                : $"{node.DescriptionChn} / {node.Description}";
    }

    private string T(string key) => _localizationService.Get(key);
}
