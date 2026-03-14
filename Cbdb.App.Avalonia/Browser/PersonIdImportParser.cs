using System.Text;

namespace Cbdb.App.Avalonia.Browser;

public static class PersonIdImportParser {
    public static IReadOnlyList<int> Parse(string? content) {
        if (string.IsNullOrWhiteSpace(content)) {
            return Array.Empty<int>();
        }

        var lines = content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length == 0) {
            return Array.Empty<int>();
        }

        var firstRow = ParseCsvRow(lines[0]);
        var headerIndex = firstRow.FindIndex(cell => string.Equals(NormalizeHeader(cell), "c_personid", StringComparison.OrdinalIgnoreCase));
        var useHeader = headerIndex >= 0;
        var personIdIndex = useHeader ? headerIndex : 0;
        var startIndex = useHeader ? 1 : 0;

        var results = new List<int>();
        var seen = new HashSet<int>();
        for (var i = startIndex; i < lines.Length; i++) {
            var row = ParseCsvRow(lines[i]);
            if (personIdIndex >= row.Count) {
                if (!useHeader && int.TryParse(lines[i].Trim(), out var linePersonId) && linePersonId > 0 && seen.Add(linePersonId)) {
                    results.Add(linePersonId);
                }

                continue;
            }

            if (TryParsePersonId(row[personIdIndex], out var personId) && seen.Add(personId)) {
                results.Add(personId);
            } else if (!useHeader && row.Count == 1 && int.TryParse(lines[i].Trim(), out var rawLinePersonId) && rawLinePersonId > 0 && seen.Add(rawLinePersonId)) {
                results.Add(rawLinePersonId);
            }
        }

        return results;
    }

    private static bool TryParsePersonId(string? value, out int personId) {
        return int.TryParse(value?.Trim(), out personId) && personId > 0;
    }

    private static string NormalizeHeader(string? value) {
        return (value ?? string.Empty).Trim().Trim('"').Trim();
    }

    private static List<string> ParseCsvRow(string line) {
        var cells = new List<string>();
        var builder = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++) {
            var ch = line[i];
            if (ch == '"') {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') {
                    builder.Append('"');
                    i++;
                } else {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == ',' && !inQuotes) {
                cells.Add(builder.ToString().Trim());
                builder.Clear();
                continue;
            }

            builder.Append(ch);
        }

        cells.Add(builder.ToString().Trim());
        return cells;
    }
}
