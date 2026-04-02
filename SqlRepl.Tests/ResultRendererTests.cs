using System.Data;
using Spectre.Console;
using Spectre.Console.Testing;

namespace SqlRepl.Tests;

public class ResultRendererTests
{
    [Fact]
    public void Render_QueryWithData_OutputsTableAndRowCount()
    {
        var dt = new DataTable();
        dt.Columns.Add("ID", typeof(int));
        dt.Columns.Add("NAME", typeof(string));
        dt.Rows.Add(1, "Alice");
        dt.Rows.Add(2, "Bob");

        var result = new QueryResult
        {
            Data = dt,
            RowsAffected = 2,
            Elapsed = TimeSpan.FromMilliseconds(42),
            IsQuery = true
        };

        var console = new TestConsole();
        console.Profile.Width = 120;

        ResultRenderer.Render(result, console);

        var output = console.Output;
        Assert.Contains("id", output);
        Assert.Contains("name", output);
        Assert.Contains("Alice", output);
        Assert.Contains("Bob", output);
        Assert.Contains("2 row(s) returned", output);
    }

    [Fact]
    public void Render_NonQuery_OutputsRowsAffected()
    {
        var result = new QueryResult
        {
            RowsAffected = 5,
            Elapsed = TimeSpan.FromMilliseconds(10),
            IsQuery = false
        };

        var console = new TestConsole();
        console.Profile.Width = 120;

        ResultRenderer.Render(result, console);

        var output = console.Output;
        Assert.Contains("5 row(s) affected", output);
        Assert.DoesNotContain("returned", output);
    }

    [Fact]
    public void Render_QueryWithNullValues_ShowsNULL()
    {
        var dt = new DataTable();
        dt.Columns.Add("VAL", typeof(string));
        dt.Rows.Add(DBNull.Value);

        var result = new QueryResult
        {
            Data = dt,
            RowsAffected = 1,
            Elapsed = TimeSpan.FromMilliseconds(1),
            IsQuery = true
        };

        var console = new TestConsole();
        console.Profile.Width = 120;

        ResultRenderer.Render(result, console);

        var output = console.Output;
        Assert.Contains("NULL", output);
    }
}
