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
    public void ReplSettings_DefaultPageSize_IsZero_MeansAuto()
    {
        var settings = new ReplSettings();
        Assert.Equal(0, settings.PageSize);
    }

    [Fact]
    public void Render_AutoPageSize_UsesConsoleHeight()
    {
        // PageSize=0 means auto: derive from console height
        // With height=30 and overhead of 8, effective page size = 22
        var settings = new ReplSettings { PageSize = 0 };
        var dt = CreateTable(50);

        var result = new QueryResult
        {
            Data = dt,
            RowsAffected = 50,
            Elapsed = TimeSpan.FromMilliseconds(1),
            IsQuery = true
        };

        var console = new TestConsole();
        console.Profile.Width = 120;
        console.Profile.Height = 30;

        ResultRenderer.Render(result, console, settings);

        var output = console.Output;
        // Should paginate based on height (30 - 8 = 22 rows per page)
        Assert.Contains("row-0", output);
        Assert.Contains("row-21", output);
        Assert.DoesNotContain("row-22", output);
        Assert.Contains("Page 1", output);
    }

    [Fact]
    public void Render_AutoPageSize_SmallTerminal_ClampsToMinimum()
    {
        // Even with a tiny terminal, page size should not go below 10
        var settings = new ReplSettings { PageSize = 0 };
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
        console.Profile.Height = 5; // very small

        ResultRenderer.Render(result, console, settings);

        var output = console.Output;
        // Minimum page size is 10, so first 10 rows should show
        Assert.Contains("row-0", output);
        Assert.Contains("row-9", output);
        Assert.DoesNotContain("row-10", output);
        Assert.Contains("Page 1", output);
    }

    [Fact]
    public void Render_ExplicitPageSize_IgnoresConsoleHeight()
    {
        // When PageSize is explicitly set (>0), use it regardless of console height
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
        console.Profile.Height = 100; // tall terminal, but explicit PageSize should win

        ResultRenderer.Render(result, console, settings);

        var output = console.Output;
        Assert.Contains("row-0", output);
        Assert.Contains("row-4", output);
        Assert.DoesNotContain("row-5", output);
        Assert.Contains("Page 1", output);
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
