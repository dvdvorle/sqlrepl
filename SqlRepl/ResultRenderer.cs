using System.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SqlRepl;

public static class ResultRenderer
{
    public static void Render(QueryResult result, IAnsiConsole? console = null)
    {
        console ??= AnsiConsole.Console;

        if (result.IsQuery && result.Data is not null)
        {
            RenderTable(result.Data, console);
        }

        var info = result.IsQuery
            ? $"[grey]{result.RowsAffected} row(s) returned[/]"
            : $"[grey]{result.RowsAffected} row(s) affected[/]";

        console.MarkupLine($"{info} [grey]in {result.Elapsed.TotalMilliseconds:F0}ms[/]");
    }

    private static void RenderTable(DataTable data, IAnsiConsole console)
    {
        if (data.Columns.Count == 0)
            return;

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey);

        foreach (DataColumn col in data.Columns)
        {
            table.AddColumn(Markup.Escape(col.ColumnName.ToLowerInvariant()), c => c.NoWrap());
        }

        foreach (DataRow row in data.Rows)
        {
            var cells = new IRenderable[data.Columns.Count];
            for (var i = 0; i < data.Columns.Count; i++)
            {
                var value = row[i];
                if (value == DBNull.Value)
                {
                    cells[i] = new Markup("[grey italic]NULL[/]");
                }
                else
                {
                    cells[i] = new NonBreakingText(value?.ToString() ?? string.Empty);
                }
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
        var text = _text.Length <= maxWidth
            ? _text
            : _text[..(maxWidth - 1)] + "…";
        yield return new Segment(text, _style);
        yield return Segment.LineBreak;
    }
}
