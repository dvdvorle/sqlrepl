using System.Data;
using System.Text;
using Spectre.Console;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace SqlRepl.Commands;

[Command("info", Description = "Show column details and foreign keys for a table.")]
public class InfoCommand : ICommand
{
    private readonly ConnectionManager _connectionManager;
    private readonly IQueryExecutor _queryExecutor;
    private readonly ReplSettings _settings;

    [CommandParameter(0, Name = "table", Description = "Table name to inspect")]
    public string TableName { get; init; } = "";

    public InfoCommand(ConnectionManager connectionManager, IQueryExecutor queryExecutor, ReplSettings settings)
    {
        _connectionManager = connectionManager;
        _queryExecutor = queryExecutor;
        _settings = settings;
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (string.IsNullOrWhiteSpace(TableName))
        {
            console.Output.WriteLine("Usage: info <table_name>");
            return;
        }

        if (!_connectionManager.IsConnected)
        {
            console.Output.WithForegroundColor(ConsoleColor.Red,
                o => o.WriteLine("Not connected. Use 'conn' to connect first."));
            return;
        }

        var upperTable = TableName.Trim().ToUpperInvariant();

        try
        {
            var columnsSql = BuildColumnsSql(upperTable);
            var fkSql = BuildForeignKeysSql(upperTable);

            var columnsResult = await _queryExecutor.ExecuteAsync(columnsSql);
            var fkResult = await _queryExecutor.ExecuteAsync(fkSql);

            if (columnsResult.Data is null || columnsResult.Data.Rows.Count == 0)
            {
                console.Output.WithForegroundColor(ConsoleColor.Yellow,
                    o => o.WriteLine($"Table '{upperTable}' not found."));
                return;
            }

            var output = RenderInfo(upperTable, columnsResult.Data, fkResult.Data!);
            AnsiConsole.Write(new Markup(output));
            AnsiConsole.WriteLine();
        }
        catch (Exception ex)
        {
            console.Output.WithForegroundColor(ConsoleColor.Red,
                o => o.WriteLine($"Error: {ex.Message}"));
        }
    }

    public static string BuildColumnsSql(string tableName)
    {
        var upper = tableName.Trim().ToUpperInvariant();
        return $@"SELECT t.column_id, t.column_name, t.data_type, t.data_length, t.nullable, c.comments
FROM all_tab_columns t
INNER JOIN all_col_comments c ON t.column_name = c.column_name
  AND t.table_name = c.table_name
  AND t.owner = c.owner
WHERE t.table_name = '{upper}'
  AND t.owner = (
    SELECT MIN(owner) FROM all_tab_columns WHERE table_name = '{upper}'
  )
ORDER BY t.column_id";
    }

    public static string BuildForeignKeysSql(string tableName)
    {
        var upper = tableName.Trim().ToUpperInvariant();
        return $@"SELECT a.column_name, a.constraint_name,
       c_pk.table_name r_table_name, c_pk.constraint_name r_pk
FROM all_cons_columns a
JOIN all_constraints c ON a.owner = c.owner
  AND a.constraint_name = c.constraint_name
JOIN all_constraints c_pk ON c.r_owner = c_pk.owner
  AND c.r_constraint_name = c_pk.constraint_name
WHERE c.constraint_type = 'R'
  AND a.table_name = '{upper}'
  AND a.owner = (
    SELECT MIN(owner) FROM all_tab_columns WHERE table_name = '{upper}'
  )";
    }

    /// <summary>
    /// Renders column and FK info as Spectre markup string. 
    /// Made public/static for testability without a live DB connection.
    /// </summary>
    public static string RenderInfo(string tableName, DataTable columns, DataTable foreignKeys)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[bold yellow]{Markup.Escape(tableName)}[/]");
        sb.AppendLine();

        // Columns table
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey);

        table.AddColumn(new TableColumn("[bold]#[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Column[/]"));
        table.AddColumn(new TableColumn("[bold]Type[/]"));
        table.AddColumn(new TableColumn("[bold]Null[/]").Centered());
        table.AddColumn(new TableColumn("[bold]Comment[/]"));

        foreach (DataRow row in columns.Rows)
        {
            var id = row["COLUMN_ID"]?.ToString() ?? "";
            var name = row["COLUMN_NAME"]?.ToString() ?? "";
            var dataType = row["DATA_TYPE"]?.ToString() ?? "";
            var dataLength = row["DATA_LENGTH"]?.ToString() ?? "";
            var nullable = row["NULLABLE"]?.ToString() ?? "";
            var comment = row["COMMENTS"] == DBNull.Value ? "" : row["COMMENTS"]?.ToString() ?? "";

            var typeDisplay = FormatType(dataType, dataLength);
            var nullDisplay = nullable == "Y" ? "[grey]Y[/]" : "[bold red]N[/]";
            var commentDisplay = string.IsNullOrEmpty(comment) ? "[grey]—[/]" : Markup.Escape(comment);

            table.AddRow(
                new Markup($"[grey]{Markup.Escape(id)}[/]"),
                new Markup($"[bold]{Markup.Escape(name)}[/]"),
                new Markup($"[cyan]{Markup.Escape(typeDisplay)}[/]"),
                new Markup(nullDisplay),
                new Markup(commentDisplay));
        }

        // Capture table render to string via StringWriter-backed console
        var writer = new StringWriter();
        var testConsole = Spectre.Console.AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(writer),
            Ansi = AnsiSupport.No,
            Interactive = InteractionSupport.No
        });
        testConsole.Write(table);
        sb.Append(Markup.Escape(writer.ToString()));

        // Foreign keys
        if (foreignKeys.Rows.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("[bold yellow]Foreign Keys[/]");

            var fkTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey);

            fkTable.AddColumn(new TableColumn("[bold]Column[/]"));
            fkTable.AddColumn(new TableColumn("[bold]Constraint[/]"));
            fkTable.AddColumn(new TableColumn("[bold]References[/]"));

            foreach (DataRow row in foreignKeys.Rows)
            {
                var column = row["COLUMN_NAME"]?.ToString() ?? "";
                var constraint = row["CONSTRAINT_NAME"]?.ToString() ?? "";
                var refTable = row["R_TABLE_NAME"]?.ToString() ?? "";

                fkTable.AddRow(
                    new Markup($"[bold]{Markup.Escape(column)}[/]"),
                    new Markup($"[grey]{Markup.Escape(constraint)}[/]"),
                    new Markup($"[cyan]{Markup.Escape(refTable)}[/]"));
            }

            var fkWriter = new StringWriter();
            var fkConsole = Spectre.Console.AnsiConsole.Create(new AnsiConsoleSettings
            {
                Out = new AnsiConsoleOutput(fkWriter),
                Ansi = AnsiSupport.No,
                Interactive = InteractionSupport.No
            });
            fkConsole.Write(fkTable);
            sb.Append(Markup.Escape(fkWriter.ToString()));
        }

        return sb.ToString();
    }

    private static string FormatType(string dataType, string dataLength)
    {
        return dataType.ToUpperInvariant() switch
        {
            "NUMBER" => "NUMBER",
            "DATE" => "DATE",
            "CLOB" => "CLOB",
            "BLOB" => "BLOB",
            "TIMESTAMP" or "TIMESTAMP(6)" => "TIMESTAMP",
            _ => $"{dataType}({dataLength})"
        };
    }
}
