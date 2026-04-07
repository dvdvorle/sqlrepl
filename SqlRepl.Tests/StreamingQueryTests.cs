using System.Data;
using System.Diagnostics;
using Spectre.Console.Testing;

namespace SqlRepl.Tests;

public class StreamingQueryTests
{
    #region StreamingQueryResult — page reading

    [Fact]
    public async Task ReadPageAsync_ReadsRequestedNumberOfRows()
    {
        var dt = CreateTable(25);
        await using var stream = CreateStream(dt);

        var page = await stream.ReadPageAsync(10);

        Assert.Equal(10, page.Count);
        Assert.True(stream.HasMore);
        Assert.Equal(10, stream.TotalRowsRead);
    }

    [Fact]
    public async Task ReadPageAsync_ReturnsRemainingWhenFewerThanPageSize()
    {
        var dt = CreateTable(5);
        await using var stream = CreateStream(dt);

        var page = await stream.ReadPageAsync(10);

        Assert.Equal(5, page.Count);
        Assert.False(stream.HasMore);
        Assert.Equal(5, stream.TotalRowsRead);
    }

    [Fact]
    public async Task ReadPageAsync_ReturnsEmptyWhenExhausted()
    {
        var dt = CreateTable(3);
        await using var stream = CreateStream(dt);

        await stream.ReadPageAsync(10); // reads all 3
        var page2 = await stream.ReadPageAsync(10);

        Assert.Empty(page2);
        Assert.False(stream.HasMore);
    }

    [Fact]
    public async Task ReadPageAsync_MultiplePagesTrackTotalRows()
    {
        var dt = CreateTable(25);
        await using var stream = CreateStream(dt);

        await stream.ReadPageAsync(10);
        Assert.Equal(10, stream.TotalRowsRead);

        await stream.ReadPageAsync(10);
        Assert.Equal(20, stream.TotalRowsRead);

        await stream.ReadPageAsync(10); // only 5 left
        Assert.Equal(25, stream.TotalRowsRead);
        Assert.False(stream.HasMore);
    }

    [Fact]
    public async Task ReadPageAsync_ValuesAreCorrect()
    {
        var dt = CreateTable(3);
        await using var stream = CreateStream(dt);

        var page = await stream.ReadPageAsync(10);

        Assert.Equal(0, page[0][0]);
        Assert.Equal("row-0", page[0][1]);
        Assert.Equal(2, page[2][0]);
        Assert.Equal("row-2", page[2][1]);
    }

    #endregion

    #region ResultRenderer.RenderStreamingAsync

    [Fact]
    public async Task RenderStreamingAsync_SinglePage_ShowsAllRowsAndTiming()
    {
        var dt = CreateTable(5);
        await using var stream = CreateStream(dt);

        var console = new TestConsole();
        console.Profile.Width = 120;
        console.Profile.Height = 30;

        await ResultRenderer.RenderStreamingAsync(stream, console, new ReplSettings());

        var output = console.Output;
        Assert.Contains("row-0", output);
        Assert.Contains("row-4", output);
        Assert.Contains("5 row(s) returned", output);
        Assert.DoesNotContain("Next page", output);
    }

    [Fact]
    public async Task RenderStreamingAsync_MoreThanOnePage_NonInteractive_ShowsFirstPageOnly()
    {
        var dt = CreateTable(50);
        await using var stream = CreateStream(dt);

        var console = new TestConsole();
        console.Profile.Width = 120;
        console.Profile.Height = 20; // effective page size = 12

        await ResultRenderer.RenderStreamingAsync(stream, console, new ReplSettings());

        var output = console.Output;
        Assert.Contains("row-0", output);
        Assert.Contains("row-11", output);
        Assert.DoesNotContain("row-12", output);
        // Should show the streaming page info
        Assert.Contains("12+ rows", output);
    }

    [Fact]
    public async Task RenderStreamingAsync_EmptyResult_ShowsZeroRows()
    {
        var dt = CreateTable(0);
        await using var stream = CreateStream(dt);

        var console = new TestConsole();
        console.Profile.Width = 120;
        console.Profile.Height = 30;

        await ResultRenderer.RenderStreamingAsync(stream, console, new ReplSettings());

        var output = console.Output;
        Assert.Contains("0 row(s) returned", output);
    }

    [Fact]
    public async Task RenderStreamingAsync_HidesAllNullColumns()
    {
        var dt = new DataTable();
        dt.Columns.Add("ID", typeof(int));
        dt.Columns.Add("ALWAYS_NULL", typeof(string));
        dt.Columns.Add("NAME", typeof(string));
        for (int i = 0; i < 5; i++)
            dt.Rows.Add(i, DBNull.Value, $"row-{i}");

        await using var stream = CreateStream(dt);

        var console = new TestConsole();
        console.Profile.Width = 120;
        console.Profile.Height = 30;

        await ResultRenderer.RenderStreamingAsync(stream, console, new ReplSettings());

        var output = console.Output;
        Assert.Contains("row-0", output);
        Assert.Contains("always_null", output.ToLowerInvariant()); // mentioned in hidden list
        Assert.Contains("Hidden", output);
    }

    #endregion

    #region QueryExecutor.IsQuery

    [Theory]
    [InlineData("SELECT * FROM dual", true)]
    [InlineData("  select 1", true)]
    [InlineData("WITH cte AS (...) SELECT * FROM cte", true)]
    [InlineData("DESCRIBE employees", true)]
    [InlineData("DESC employees", true)]
    [InlineData("SHOW tables", true)]
    [InlineData("INSERT INTO t VALUES (1)", false)]
    [InlineData("UPDATE t SET x=1", false)]
    [InlineData("DELETE FROM t", false)]
    [InlineData("CREATE TABLE t (id int)", false)]
    public void IsQuery_ClassifiesCorrectly(string sql, bool expected)
    {
        Assert.Equal(expected, QueryExecutor.IsQuery(sql));
    }

    #endregion

    #region Helpers

    private static DataTable CreateTable(int rowCount)
    {
        var dt = new DataTable();
        dt.Columns.Add("ID", typeof(int));
        dt.Columns.Add("NAME", typeof(string));
        for (var i = 0; i < rowCount; i++)
            dt.Rows.Add(i, $"row-{i}");
        return dt;
    }

    private static StreamingQueryResult CreateStream(DataTable dt)
    {
        var reader = dt.CreateDataReader();
        var columns = new string[dt.Columns.Count];
        for (int i = 0; i < dt.Columns.Count; i++)
            columns[i] = dt.Columns[i].ColumnName;
        return new StreamingQueryResult(reader, null, columns, Stopwatch.StartNew());
    }

    #endregion
}
