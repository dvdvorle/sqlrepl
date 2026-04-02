using System.Data;
using Spectre.Console.Testing;

namespace SqlRepl.Tests;

public class ResultRendererPaginationTests
{
    [Fact]
    public void Render_UnderPageSize_ShowsAllRows()
    {
        var settings = new ReplSettings { PageSize = 50 };
        var dt = CreateTable(10);

        var result = new QueryResult
        {
            Data = dt,
            RowsAffected = 10,
            Elapsed = TimeSpan.FromMilliseconds(1),
            IsQuery = true
        };

        var console = new TestConsole();
        console.Profile.Width = 120;

        ResultRenderer.Render(result, console, settings);

        var output = console.Output;
        Assert.Contains("row-0", output);
        Assert.Contains("row-9", output);
        // No pagination prompt for small results
        Assert.DoesNotContain("Page", output);
    }

    [Fact]
    public void Render_OverPageSize_ShowsFirstPageOnly()
    {
        var settings = new ReplSettings { PageSize = 5 };
        var dt = CreateTable(20);

        var result = new QueryResult
        {
            Data = dt,
            RowsAffected = 20,
            Elapsed = TimeSpan.FromMilliseconds(1),
            IsQuery = true
        };

        var console = new TestConsole();
        console.Profile.Width = 120;
        // TestConsole is non-interactive, so pagination should render first page and stop
        ResultRenderer.Render(result, console, settings);

        var output = console.Output;
        Assert.Contains("row-0", output);
        Assert.Contains("row-4", output);
        Assert.Contains("Page 1", output);
    }

    [Fact]
    public void ReplSettings_DefaultPageSize_Is50()
    {
        var settings = new ReplSettings();
        Assert.Equal(50, settings.PageSize);
    }

    private static DataTable CreateTable(int rowCount)
    {
        var dt = new DataTable();
        dt.Columns.Add("ID", typeof(int));
        dt.Columns.Add("NAME", typeof(string));
        for (var i = 0; i < rowCount; i++)
            dt.Rows.Add(i, $"row-{i}");
        return dt;
    }
}
