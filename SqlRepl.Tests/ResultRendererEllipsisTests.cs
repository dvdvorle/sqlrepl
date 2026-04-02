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
        var longValue = new string('x', 200);
        dt.Rows.Add(longValue);

        var result = new QueryResult
        {
            Data = dt,
            RowsAffected = 1,
            Elapsed = TimeSpan.FromMilliseconds(1),
            IsQuery = true
        };

        var console = new TestConsole();
        console.Profile.Width = 250;

        ResultRenderer.Render(result, console);

        var output = console.Output;
        // Content should stay on a single line — not wrapped across multiple lines
        var lines = output.Split('\n');
        var dataLines = lines.Where(l => l.Contains("xxxx")).ToList();
        Assert.Single(dataLines);
    }

    [Fact]
    public void Render_CellContentHasNoLineBreaks()
    {
        var dt = new DataTable();
        dt.Columns.Add("VAL", typeof(string));
        dt.Rows.Add("hello world foo bar");

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
        // All cell content words should be on a single line — no wrapping
        var lines = output.Split('\n');
        var dataLines = lines.Where(l => l.Contains("hello")).ToList();
        Assert.Single(dataLines);
        Assert.Contains("foo", dataLines[0]);
        Assert.Contains("bar", dataLines[0]);
    }
}
