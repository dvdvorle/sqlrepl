using System.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SqlRepl;

public static class ResultRenderer
{
    private const int TableOverheadLines = 8; // borders, header, separator, footer, status line, prompt
    private const int MinPageSize = 10;
    private const int FallbackPageSize = 50;

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
                var pageSize = GetEffectivePageSize(settings, console);

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

    public static async Task RenderStreamingAsync(StreamingQueryResult stream, IAnsiConsole? console = null, ReplSettings? settings = null)
    {
        console ??= AnsiConsole.Console;
        settings ??= new ReplSettings();

        var pageSize = GetEffectivePageSize(settings, console);

        var rows = await stream.ReadPageAsync(pageSize);
        stream.StopTimer();

        if (rows.Count == 0)
        {
            console.MarkupLine($"[grey]0 row(s) returned in {stream.Elapsed.TotalMilliseconds:F0}ms[/]");
            return;
        }

        var (visibleIndices, hiddenNames) = GetVisibleColumnsFromRows(stream.ColumnNames, rows);

        RenderStreamingPage(stream.ColumnNames, visibleIndices, rows, console, settings);

        if (stream.HasMore)
        {
            while (true)
            {
                console.MarkupLine($"[grey]Showing rows 1–{stream.TotalRowsRead} ({stream.TotalRowsRead}+ rows)[/]");

                if (!console.Profile.Capabilities.Interactive)
                    break;

                var choice = console.Prompt(
                    new SelectionPrompt<string>()
                        .AddChoices("Next page", "Quit"));

                if (choice == "Quit")
                    break;

                rows = await stream.ReadPageAsync(pageSize);
                if (rows.Count == 0)
                    break;

                RenderStreamingPage(stream.ColumnNames, visibleIndices, rows, console, settings);

                if (!stream.HasMore)
                    break;
            }
        }

        if (hiddenNames.Count > 0)
        {
            var names = string.Join(", ", hiddenNames);
            console.MarkupLine($"[grey]Hidden (all null in first page): {names}[/]");
        }

        console.MarkupLine($"[grey]{stream.TotalRowsRead} row(s) returned in {stream.Elapsed.TotalMilliseconds:F0}ms[/]");
    }

    private static int GetEffectivePageSize(ReplSettings settings, IAnsiConsole console)
    {
        if (settings.PageSize > 0)
            return settings.PageSize;

        var height = console.Profile.Height;
        if (height > 0)
            return Math.Max(MinPageSize, height - TableOverheadLines);

        return FallbackPageSize;
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

    private static (List<int> visible, List<string> hidden) GetVisibleColumnsFromRows(string[] columnNames, List<object[]> rows)
    {
        var visible = new List<int>();
        var hidden = new List<string>();
        for (var i = 0; i < columnNames.Length; i++)
        {
            var hasValue = rows.Any(row => row[i] != DBNull.Value && row[i] is not null);
            if (hasValue)
                visible.Add(i);
            else
                hidden.Add(columnNames[i].ToLowerInvariant());
        }
        return (visible, hidden);
    }

    private static void RenderStreamingPage(string[] columnNames, List<int> visibleIndices, List<object[]> rows, IAnsiConsole console, ReplSettings settings)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey);

        foreach (var colIdx in visibleIndices)
            table.AddColumn(Markup.Escape(columnNames[colIdx].ToLowerInvariant()), c => c.NoWrap());

        foreach (var row in rows)
        {
            var cells = new IRenderable[visibleIndices.Count];
            for (var i = 0; i < visibleIndices.Count; i++)
            {
                var value = row[visibleIndices[i]];
                if (value == DBNull.Value || value is null)
                    cells[i] = new Markup("[grey]—[/]");
                else
                    cells[i] = new NonBreakingText(FormatValue(value, settings));
            }
            table.AddRow(cells);
        }

        console.Write(table);
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
