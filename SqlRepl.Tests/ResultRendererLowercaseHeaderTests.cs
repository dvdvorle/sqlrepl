using System.Data;
using Spectre.Console;
using Spectre.Console.Testing;

namespace SqlRepl.Tests;

public class ResultRendererLowercaseHeaderTests
{
    [Fact]
    public void Render_ColumnsAreRenderedLowercase()
    {
        var dt = new DataTable();
        dt.Columns.Add("EMPLOYEE_ID", typeof(int));
        dt.Columns.Add("FULL_NAME", typeof(string));
        dt.Rows.Add(1, "Alice");

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
        Assert.Contains("employee_id", output);
        Assert.Contains("full_name", output);
        Assert.DoesNotContain("EMPLOYEE_ID", output);
        Assert.DoesNotContain("FULL_NAME", output);
    }
}
