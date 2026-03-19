using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Cbdb.App.Avalonia.Modules;

internal sealed class QueryPickerSearchState {
    private List<string> _matches = new();

    public IReadOnlyList<string> Matches => _matches;
    public int Index { get; private set; } = -1;
    public bool HasActiveMatch => _matches.Count > 0 && Index >= 0;

    public void Reset() {
        _matches.Clear();
        Index = -1;
    }

    public void Replace(IEnumerable<string> matches) {
        _matches = matches.ToList();
        Index = -1;
    }

    public string? MoveNext() {
        if (_matches.Count == 0) {
            return null;
        }

        Index = (Index + 1) % _matches.Count;
        return _matches[Index];
    }
}

internal static class QueryPickerTreeHelper {
    public static void BindSelectionCheckBox(
        CheckBox checkBox,
        string code,
        ISet<string> selectedCodes,
        Action onSelectionChanged
    ) {
        checkBox.Tag = code;
        checkBox.IsCheckedChanged += (_, _) => {
            if (checkBox.IsChecked == true) {
                selectedCodes.Add(code);
            } else {
                selectedCodes.Remove(code);
            }

            onSelectionChanged();
        };
    }

    public static void BindWholeRowToggle(Border row, CheckBox checkBox) {
        row.PointerPressed += (_, e) => {
            if (e.Source is CheckBox) {
                return;
            }

            checkBox.IsChecked = checkBox.IsChecked != true;
            e.Handled = true;
        };
    }

    public static IReadOnlyList<TOption> LimitVisibleOptionsPreservingSelected<TOption>(
        IEnumerable<TOption> options,
        Func<TOption, string> getCode,
        IReadOnlySet<string> selectedCodes,
        int maxVisibleCount
    ) {
        var optionList = options.ToList();
        var selected = optionList
            .Where(option => selectedCodes.Contains(getCode(option)))
            .ToList();

        var remainingSlots = Math.Max(0, maxVisibleCount - selected.Count);
        if (remainingSlots == 0) {
            return selected;
        }

        return selected
            .Concat(optionList.Where(option => !selectedCodes.Contains(getCode(option))).Take(remainingSlots))
            .ToList();
    }

    public static void InitializeChildren<TNode>(
        TreeViewItem item,
        IReadOnlyList<TNode> children,
        bool shouldMaterialize,
        object unloadedChildPlaceholder,
        Func<TNode, TreeViewItem> buildChildItem
    ) {
        if (children.Count == 0) {
            return;
        }

        item.ItemsSource = shouldMaterialize
            ? children.Select(buildChildItem).ToList()
            : new[] { unloadedChildPlaceholder };
    }

    public static void ExpandNode<TNode>(
        TreeViewItem item,
        string code,
        IReadOnlyList<TNode> children,
        HashSet<string> expandedCodes,
        Func<TNode, TreeViewItem> buildChildItem
    ) {
        expandedCodes.Add(code);
        if (children.Count > 0) {
            item.ItemsSource = children.Select(buildChildItem).ToList();
        }
    }

    public static void CollapseNode(string code, HashSet<string> expandedCodes) {
        expandedCodes.Remove(code);
    }

    public static bool TryHandleWholeRowToggle<TNode>(
        object? sender,
        PointerReleasedEventArgs e,
        HashSet<string> expandedCodes,
        Func<TNode, string> getCode,
        Func<TNode, IReadOnlyList<TNode>> getChildren,
        Func<TNode, TreeViewItem> buildChildItem
    ) {
        if (sender is not TreeViewItem item || item.Tag is not TNode node) {
            return false;
        }

        var children = getChildren(node);
        if (children.Count == 0) {
            return false;
        }

        if (e.Source is not global::Avalonia.Visual sourceVisual) {
            return false;
        }

        var sourceTreeItem = sourceVisual.FindAncestorOfType<TreeViewItem>() ?? sourceVisual as TreeViewItem;
        if (!ReferenceEquals(sourceTreeItem, item)) {
            return false;
        }

        var sourceToggle = sourceVisual.FindAncestorOfType<ToggleButton>() ?? sourceVisual as ToggleButton;
        if (sourceToggle is not null) {
            return false;
        }

        var scrollViewer = item.FindAncestorOfType<ScrollViewer>();
        var previousOffset = scrollViewer?.Offset;
        var code = getCode(node);
        if (item.IsExpanded) {
            item.IsExpanded = false;
            expandedCodes.Remove(code);
        } else {
            expandedCodes.Add(code);
            item.ItemsSource = children.Select(buildChildItem).ToList();
            item.IsExpanded = true;
        }

        if (scrollViewer is not null && previousOffset.HasValue) {
            Dispatcher.UIThread.Post(() => scrollViewer.Offset = previousOffset.Value);
        }

        e.Handled = true;
        return true;
    }

    public static TNode? FindNode<TNode>(
        TNode root,
        string code,
        Func<TNode, string> getCode,
        Func<TNode, IEnumerable<TNode>> getChildren
    ) where TNode : class {
        if (string.Equals(getCode(root), code, StringComparison.OrdinalIgnoreCase)) {
            return root;
        }

        foreach (var child in getChildren(root)) {
            var found = FindNode(child, code, getCode, getChildren);
            if (found is not null) {
                return found;
            }
        }

        return null;
    }

    public static bool ContainsDescendant<TNode>(
        TNode parent,
        string code,
        Func<TNode, string> getCode,
        Func<TNode, IEnumerable<TNode>> getChildren
    ) {
        foreach (var child in getChildren(parent)) {
            if (string.Equals(getCode(child), code, StringComparison.OrdinalIgnoreCase)
                || ContainsDescendant(child, code, getCode, getChildren)) {
                return true;
            }
        }

        return false;
    }
}
