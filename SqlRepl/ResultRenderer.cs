using System.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SqlRepl;

public static class ResultRenderer
{
    public static void Render(QueryResult result, IAnsiConsole? console = null, ReplSettings? settings = null)
    {
        console ??= AnsiConsole.Console;
        settings ??= new ReplSettings();

        if (result.IsQuery && result.Data is not null)
        {
            var hiddenColumns = RenderTable(result.Data, console, settings);
            if (hiddenColumns.Count > 0)
            {
                var names = string.Join(", ", hiddenColumns);
                console.MarkupLine($"[grey]Hidden (all null): {names}[/]");
            }
        }

        var info = result.IsQuery
            ? $"[grey]{result.RowsAffected} row(s) returned[/]"
            : $"[grey]{result.RowsAffected} row(s) affected[/]";

        console.MarkupLine($"{info} [grey]in {result.Elapsed.TotalMilliseconds:F0}ms[/]");
    }

    private static IReadOnlyList<string> RenderTable(DataTable data, IAnsiConsole console, ReplSettings settings)
    {
        if (data.Columns.Count == 0)
            return [];

        // Determine which columns have at least one non-null value
        var visibleColumns = new List<int>();
        var hiddenColumns = new List<string>();
        for (var i = 0; i < data.Columns.Count; i++)
        {
            var hasValue = false;
            foreach (DataRow row in data.Rows)
            {
                if (row[i] != DBNull.Value)
                {
                    hasValue = true;
                    break;
                }
            }

            if (hasValue)
                visibleColumns.Add(i);
            else
                hiddenColumns.Add(data.Columns[i].ColumnName.ToLowerInvariant());
        }

        if (visibleColumns.Count == 0)
            return hiddenColumns;

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey);

        foreach (var colIdx in visibleColumns)
        {
            table.AddColumn(Markup.Escape(data.Columns[colIdx].ColumnName.ToLowerInvariant()), c => c.NoWrap());
        }

        foreach (DataRow row in data.Rows)
        {
            var cells = new IRenderable[visibleColumns.Count];
            for (var i = 0; i < visibleColumns.Count; i++)
            {
                var value = row[visibleColumns[i]];
                if (value == DBNull.Value)
                {
                    cells[i] = new Markup("[grey]—[/]");
                }
                else
                {
                    cells[i] = new NonBreakingText(FormatValue(value, settings));
                }
            }
            table.AddRow(cells);
        }

        console.Write(table);

        return hiddenColumns;
    }

    private static string FormatValue(object value, ReplSettings settings)
    {
        if (value is DateTime dt)
        {
            var format = dt.TimeOfDay == TimeSpan.Zero
                ? settings.DateOnlyFormat
                : settings.DateFormat;
            return dt.ToString(format);
        }

        return value.ToString() ?? string.Empty;
    }
}

/// <summary>
/// A single-line text renderable that never wraps or breaks at whitespace.
/// Inspired by Spectre.Console's Paragraph but skips line splitting entirely.
/// </summary>
public sealed class NonBreakingText : IRenderable
{
    private readonly string _text;
    private readonly Style _style;

    public NonBreakingText(string text, Style? style = null)
    {
        _text = text;
        _style = style ?? Style.Plain;
    }

    public Measurement Measure(RenderOptions options, int maxWidth)
    {
        var cellCount = _text.Length;
        return new Measurement(Math.Min(cellCount, maxWidth), Math.Min(cellCount, maxWidth));
    }

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        string text;
        if (maxWidth <= 0)
            text = string.Empty;
        else if (_text.Length <= maxWidth)
            text = _text;
        else if (maxWidth == 1)
            text = "…";
        else
            text = _text[..(maxWidth - 1)] + "…";

        yield return new Segment(text, _style);
        yield return Segment.LineBreak;
    }
}
