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
            var (visibleColumns, hiddenColumns) = GetVisibleColumns(result.Data);

            if (visibleColumns.Count == 0 && hiddenColumns.Count > 0)
            {
                var names = string.Join(", ", hiddenColumns);
                console.MarkupLine($"[grey]Hidden (all null): {names}[/]");
            }
            else if (visibleColumns.Count > 0)
            {
                var totalRows = result.Data.Rows.Count;
                var pageSize = settings.PageSize;

                if (totalRows <= pageSize)
                {
                    RenderPage(result.Data, visibleColumns, 0, totalRows, console, settings);
                }
                else
                {
                    RenderPaginated(result.Data, visibleColumns, pageSize, totalRows, console, settings);
                }

                if (hiddenColumns.Count > 0)
                {
                    var names = string.Join(", ", hiddenColumns);
                    console.MarkupLine($"[grey]Hidden (all null): {names}[/]");
                }
            }
        }

        var info = result.IsQuery
            ? $"[grey]{result.RowsAffected} row(s) returned[/]"
            : $"[grey]{result.RowsAffected} row(s) affected[/]";

        console.MarkupLine($"{info} [grey]in {result.Elapsed.TotalMilliseconds:F0}ms[/]");
    }

    private static void RenderPaginated(DataTable data, List<int> visibleColumns, int pageSize, int totalRows, IAnsiConsole console, ReplSettings settings)
    {
        var totalPages = (int)Math.Ceiling((double)totalRows / pageSize);
        var page = 0;

        while (true)
        {
            var start = page * pageSize;
            var end = Math.Min(start + pageSize, totalRows);
            RenderPage(data, visibleColumns, start, end, console, settings);
            console.MarkupLine($"[grey]Page {page + 1}/{totalPages} ({start + 1}–{end} of {totalRows} rows)[/]");

            if (end >= totalRows)
                break;

            if (!console.Profile.Capabilities.Interactive)
                break;

            var choice = console.Prompt(
                new SelectionPrompt<string>()
                    .AddChoices("Next page", "Quit"));

            if (choice == "Quit")
                break;

            page++;
        }
    }

    private static (List<int> visible, List<string> hidden) GetVisibleColumns(DataTable data)
    {
        var visible = new List<int>();
        var hidden = new List<string>();
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
                visible.Add(i);
            else
                hidden.Add(data.Columns[i].ColumnName.ToLowerInvariant());
        }
        return (visible, hidden);
    }

    private static void RenderPage(DataTable data, List<int> visibleColumns, int startRow, int endRow, IAnsiConsole console, ReplSettings settings)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey);

        foreach (var colIdx in visibleColumns)
        {
            table.AddColumn(Markup.Escape(data.Columns[colIdx].ColumnName.ToLowerInvariant()), c => c.NoWrap());
        }

        for (var r = startRow; r < endRow; r++)
        {
            var row = data.Rows[r];
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
