using System.Data;
using Spectre.Console;
using Spectre.Console.Testing;

namespace SqlRepl.Tests;

public class ResultRendererEllipsisTests
{
    [Fact]
    public void Render_LongValues_AreNotWrapped()
    {
        var dt = new DataTable();
        dt.Columns.Add("VAL", typeof(string));
        dt.Rows.Add(new string('x', 200));

        var result = new QueryResult
        {
            Data = dt,
            RowsAffected = 1,
            Elapsed = TimeSpan.FromMilliseconds(1),
            IsQuery = true
        };

        var console = new TestConsole();
        console.Profile.Width = 80;

        ResultRenderer.Render(result, console);

        var output = console.Output;
        // With NoWrap + ellipsis, the table should contain the ellipsis character
        Assert.Contains("…", output);
        // And the full 200-char string should NOT appear
        Assert.DoesNotContain(new string('x', 200), output);
    }
}
