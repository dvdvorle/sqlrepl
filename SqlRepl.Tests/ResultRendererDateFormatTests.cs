using System.Data;
using Spectre.Console.Testing;

namespace SqlRepl.Tests;

public class ResultRendererDateFormatTests
{
    [Fact]
    public void Render_DateTime_UsesConfiguredFormat()
    {
        var settings = new ReplSettings
        {
            DateFormat = "yyyy-MM-dd HH:mm:ss"
        };

        var dt = new DataTable();
        dt.Columns.Add("CREATED", typeof(DateTime));
        dt.Rows.Add(new DateTime(2024, 3, 15, 14, 30, 45));

        var result = new QueryResult
        {
            Data = dt,
            RowsAffected = 1,
            Elapsed = TimeSpan.FromMilliseconds(1),
            IsQuery = true
        };

        var console = new TestConsole();
        console.Profile.Width = 120;

        ResultRenderer.Render(result, console, settings);

        var output = console.Output;
        Assert.Contains("2024-03-15 14:30:45", output);
    }

    [Fact]
    public void Render_DateOnly_UsesDateOnlyFormat()
    {
        var settings = new ReplSettings
        {
            DateFormat = "yyyy-MM-dd HH:mm:ss",
            DateOnlyFormat = "yyyy-MM-dd"
        };

        var dt = new DataTable();
        dt.Columns.Add("CREATED", typeof(DateTime));
        // Midnight = date-only
        dt.Rows.Add(new DateTime(2024, 3, 15, 0, 0, 0));

        var result = new QueryResult
        {
            Data = dt,
            RowsAffected = 1,
            Elapsed = TimeSpan.FromMilliseconds(1),
            IsQuery = true
        };

        var console = new TestConsole();
        console.Profile.Width = 120;

        ResultRenderer.Render(result, console, settings);

        var output = console.Output;
        Assert.Contains("2024-03-15", output);
        Assert.DoesNotContain("00:00:00", output);
    }

    [Fact]
    public void Render_CustomDateFormat_IsRespected()
    {
        var settings = new ReplSettings
        {
            DateFormat = "dd/MM/yyyy HH:mm"
        };

        var dt = new DataTable();
        dt.Columns.Add("CREATED", typeof(DateTime));
        dt.Rows.Add(new DateTime(2024, 3, 15, 14, 30, 0));

        var result = new QueryResult
        {
            Data = dt,
            RowsAffected = 1,
            Elapsed = TimeSpan.FromMilliseconds(1),
            IsQuery = true
        };

        var console = new TestConsole();
        console.Profile.Width = 120;

        ResultRenderer.Render(result, console, settings);

        var output = console.Output;
        Assert.Contains("15/03/2024 14:30", output);
    }

    [Fact]
    public void ReplSettings_DefaultValues_AreCorrect()
    {
        var settings = new ReplSettings();
        Assert.Equal("yyyy-MM-dd HH:mm:ss", settings.DateFormat);
        Assert.Equal("yyyy-MM-dd", settings.DateOnlyFormat);
    }
}
