using System.Data;
using NSubstitute;
using SqlRepl;
using SqlRepl.Commands;
using Typin.Console;

namespace SqlRepl.Tests;

public class InfoCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _dbPath;

    public InfoCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sqlrepl-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _dbPath = Path.Combine(_tempDir, "history.db");
    }

    private static DataTable MakeColumnsTable(params (int id, string name, string type, int length, string nullable, string? comments)[] rows)
    {
        var dt = new DataTable();
        dt.Columns.Add("COLUMN_ID", typeof(int));
        dt.Columns.Add("COLUMN_NAME", typeof(string));
        dt.Columns.Add("DATA_TYPE", typeof(string));
        dt.Columns.Add("DATA_LENGTH", typeof(int));
        dt.Columns.Add("NULLABLE", typeof(string));
        dt.Columns.Add("COMMENTS", typeof(string));
        foreach (var r in rows)
        {
            dt.Rows.Add(r.id, r.name, r.type, r.length, r.nullable,
                r.comments is null ? DBNull.Value : r.comments);
        }
        return dt;
    }

    private static DataTable MakeForeignKeysTable(params (string column, string constraint, string rTable, string rPk)[] rows)
    {
        var dt = new DataTable();
        dt.Columns.Add("COLUMN_NAME", typeof(string));
        dt.Columns.Add("CONSTRAINT_NAME", typeof(string));
        dt.Columns.Add("R_TABLE_NAME", typeof(string));
        dt.Columns.Add("R_PK", typeof(string));
        foreach (var r in rows)
        {
            dt.Rows.Add(r.column, r.constraint, r.rTable, r.rPk);
        }
        return dt;
    }

    private static DataTable EmptyForeignKeysTable() => MakeForeignKeysTable();

    [Fact]
    public async Task Info_WhenNotConnected_ShowsError()
    {
        using var connectionManager = new ConnectionManager();
        var executor = Substitute.For<IQueryExecutor>();

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        var command = new InfoCommand(connectionManager, executor, new ReplSettings())
        {
            TableName = "COMPONENT"
        };

        await command.ExecuteAsync(console);

        Assert.Contains("Not connected", output.GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Info_WithNoTableName_ShowsUsage()
    {
        using var connectionManager = new ConnectionManager();
        var executor = Substitute.For<IQueryExecutor>();

        var (console, output, _) = VirtualConsole.CreateBuffered();
        using var _ = console;

        var command = new InfoCommand(connectionManager, executor, new ReplSettings())
        {
            TableName = ""
        };

        await command.ExecuteAsync(console);

        Assert.Contains("Usage", output.GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Info_ShowsColumnDetails()
    {
        using var connectionManager = new ConnectionManager();
        var executor = Substitute.For<IQueryExecutor>();

        var columnsResult = new QueryResult
        {
            Data = MakeColumnsTable(
                (1, "ID", "NUMBER", 22, "N", null),
                (2, "NAME", "VARCHAR2", 100, "Y", "The display name")),
            RowsAffected = 2,
            Elapsed = TimeSpan.Zero,
            IsQuery = true
        };
        var fkResult = new QueryResult
        {
            Data = EmptyForeignKeysTable(),
            RowsAffected = 0,
            Elapsed = TimeSpan.Zero,
            IsQuery = true
        };

        SetupExecutorForTable(executor, "COMPONENT", columnsResult, fkResult);

        var command = new InfoCommand(connectionManager, executor, new ReplSettings())
        {
            TableName = "COMPONENT"
        };

        // We can't connect to a real DB, so test the rendering logic directly
        var rendered = InfoCommand.RenderInfo("COMPONENT", columnsResult.Data!, fkResult.Data!);

        Assert.Contains("COMPONENT", rendered);
        Assert.Contains("ID", rendered);
        Assert.Contains("NUMBER", rendered);
        Assert.Contains("NAME", rendered);
        Assert.Contains("VARCHAR2", rendered);
        Assert.Contains("The display name", rendered);
    }

    [Fact]
    public async Task Info_ShowsForeignKeys()
    {
        var columnsResult = new QueryResult
        {
            Data = MakeColumnsTable((1, "PARENT_ID", "NUMBER", 22, "Y", null)),
            RowsAffected = 1,
            Elapsed = TimeSpan.Zero,
            IsQuery = true
        };
        var fkResult = new QueryResult
        {
            Data = MakeForeignKeysTable(("PARENT_ID", "FK_PARENT", "PARENT_TABLE", "PK_PARENT")),
            RowsAffected = 1,
            Elapsed = TimeSpan.Zero,
            IsQuery = true
        };

        var rendered = InfoCommand.RenderInfo("CHILD", columnsResult.Data!, fkResult.Data!);

        Assert.Contains("Foreign Keys", rendered);
        Assert.Contains("PARENT_ID", rendered);
        Assert.Contains("PARENT_TABLE", rendered);
        Assert.Contains("FK_PARENT", rendered);
    }

    [Fact]
    public async Task Info_NoForeignKeys_HidesSection()
    {
        var columnsResult = new QueryResult
        {
            Data = MakeColumnsTable((1, "ID", "NUMBER", 22, "N", null)),
            RowsAffected = 1,
            Elapsed = TimeSpan.Zero,
            IsQuery = true
        };
        var fkResult = new QueryResult
        {
            Data = EmptyForeignKeysTable(),
            RowsAffected = 0,
            Elapsed = TimeSpan.Zero,
            IsQuery = true
        };

        var rendered = InfoCommand.RenderInfo("TEST", columnsResult.Data!, fkResult.Data!);

        Assert.DoesNotContain("Foreign Keys", rendered);
    }

    [Fact]
    public async Task Info_MultilineComment_IsShown()
    {
        var columnsResult = new QueryResult
        {
            Data = MakeColumnsTable(
                (1, "STATUS", "VARCHAR2", 50, "N", "Active status.\nLine two of comment.")),
            RowsAffected = 1,
            Elapsed = TimeSpan.Zero,
            IsQuery = true
        };
        var fkResult = new QueryResult
        {
            Data = EmptyForeignKeysTable(),
            RowsAffected = 0,
            Elapsed = TimeSpan.Zero,
            IsQuery = true
        };

        var rendered = InfoCommand.RenderInfo("MY_TABLE", columnsResult.Data!, fkResult.Data!);

        Assert.Contains("Active status.", rendered);
        Assert.Contains("Line two of comment.", rendered);
    }

    [Fact]
    public async Task Info_TableNameIsCaseInsensitive()
    {
        using var connectionManager = new ConnectionManager();
        var executor = Substitute.For<IQueryExecutor>();

        var command = new InfoCommand(connectionManager, executor, new ReplSettings())
        {
            TableName = "component"
        };

        // Verify the SQL uses upper-case table name
        var sql = InfoCommand.BuildColumnsSql("component");
        Assert.Contains("'COMPONENT'", sql);
    }

    private static void SetupExecutorForTable(IQueryExecutor executor, string tableName, QueryResult columnsResult, QueryResult fkResult)
    {
        executor.ExecuteAsync(Arg.Is<string>(s => s.Contains("all_tab_columns")))
            .Returns(Task.FromResult(columnsResult));
        executor.ExecuteAsync(Arg.Is<string>(s => s.Contains("all_cons_columns")))
            .Returns(Task.FromResult(fkResult));
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
