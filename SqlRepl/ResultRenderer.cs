using System.Data;
using Spectre.Console;

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
            table.AddColumn(new TableColumn($"[bold yellow]{Markup.Escape(col.ColumnName.ToLowerInvariant())}[/]"));
        }

        foreach (DataRow row in data.Rows)
        {
            var cells = new string[data.Columns.Count];
            for (var i = 0; i < data.Columns.Count; i++)
            {
                var value = row[i];
                cells[i] = value == DBNull.Value
                    ? "[grey italic]NULL[/]"
                    : Markup.Escape(value?.ToString() ?? string.Empty);
            }
            table.AddRow(cells);
        }

        console.Write(table);
    }
}
