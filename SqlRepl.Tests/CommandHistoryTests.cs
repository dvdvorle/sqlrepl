namespace SqlRepl.Tests;

public class CommandHistoryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _dbPath;

    public CommandHistoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sqlrepl-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _dbPath = Path.Combine(_tempDir, "history.db");
    }

    [Fact]
    public void Add_StoresCommand()
    {
        using var history = new CommandHistory(_dbPath);
        history.Add("SELECT 1 FROM dual", "devdb");

        var results = history.GetRecent(10);
        Assert.Single(results);
        Assert.Equal("SELECT 1 FROM dual", results[0].Command);
        Assert.Equal("devdb", results[0].Connection);
    }

    [Fact]
    public void GetRecent_ReturnsNewestFirst()
    {
        using var history = new CommandHistory(_dbPath);
        history.Add("SELECT 1", "db");
        history.Add("SELECT 2", "db");
        history.Add("SELECT 3", "db");

        var results = history.GetRecent(10);
        Assert.Equal(3, results.Count);
        Assert.Equal("SELECT 3", results[0].Command);
        Assert.Equal("SELECT 1", results[2].Command);
    }

    [Fact]
    public void GetRecent_RespectsLimit()
    {
        using var history = new CommandHistory(_dbPath);
        for (var i = 0; i < 20; i++)
            history.Add($"SELECT {i}", "db");

        var results = history.GetRecent(5);
        Assert.Equal(5, results.Count);
        Assert.Equal("SELECT 19", results[0].Command);
    }

    [Fact]
    public void Search_FullTextSearch_FindsMatches()
    {
        using var history = new CommandHistory(_dbPath);
        history.Add("SELECT * FROM employees WHERE dept = 'IT'", "db");
        history.Add("INSERT INTO logs (msg) VALUES ('hello')", "db");
        history.Add("SELECT name FROM employees", "db");

        var results = history.Search("employees");
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Contains("employees", r.Command));
    }

    [Fact]
    public void Search_NoMatch_ReturnsEmpty()
    {
        using var history = new CommandHistory(_dbPath);
        history.Add("SELECT 1 FROM dual", "db");

        var results = history.Search("nonexistent");
        Assert.Empty(results);
    }

    [Fact]
    public void Search_CaseInsensitive()
    {
        using var history = new CommandHistory(_dbPath);
        history.Add("SELECT * FROM Employees", "db");

        var results = history.Search("employees");
        Assert.Single(results);
    }

    [Fact]
    public void PersistsBetweenInstances()
    {
        var history1 = new CommandHistory(_dbPath);
        history1.Add("SELECT 1", "db");
        history1.Dispose();

        var history2 = new CommandHistory(_dbPath);
        var results = history2.GetRecent(10);
        Assert.Single(results);
        history2.Dispose();
    }

    [Fact]
    public void Add_RecordsTimestamp()
    {
        using var history = new CommandHistory(_dbPath);
        var before = DateTime.UtcNow;
        history.Add("SELECT 1", "db");
        var after = DateTime.UtcNow;

        var results = history.GetRecent(1);
        Assert.InRange(results[0].ExecutedAt, before.AddSeconds(-1), after.AddSeconds(1));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
