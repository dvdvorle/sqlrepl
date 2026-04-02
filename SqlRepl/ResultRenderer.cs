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
                var text = value == DBNull.Value
                    ? "[grey italic]NULL[/]"
                    : Markup.Escape(value?.ToString() ?? string.Empty);
                cells[i] = new Markup(text).Overflow(Overflow.Ellipsis);
            }
            table.AddRow(cells);
        }

        console.Write(table);
    }
}
