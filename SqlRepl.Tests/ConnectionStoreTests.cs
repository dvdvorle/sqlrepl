namespace SqlRepl.Tests;

public class ConnectionStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _filePath;

    public ConnectionStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sqlrepl-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _filePath = Path.Combine(_tempDir, "connections.json");
    }

    [Fact]
    public void GetAll_WhenEmpty_ReturnsEmpty()
    {
        var store = new ConnectionStore(_filePath);
        Assert.Empty(store.GetAll());
    }

    [Fact]
    public void Save_AndGet_RoundTrips()
    {
        var store = new ConnectionStore(_filePath);
        var conn = new SavedConnection
        {
            Name = "dev",
            Username = "scott",
            Password = "tiger",
            Host = "devdb"
        };

        store.Save(conn);

        var loaded = store.Get("dev");
        Assert.NotNull(loaded);
        Assert.Equal("scott", loaded.Username);
        Assert.Equal("tiger", loaded.Password);
        Assert.Equal("devdb", loaded.Host);
        Assert.True(loaded.IsComponentBased);
    }

    [Fact]
    public void Save_ConnectionString_RoundTrips()
    {
        var store = new ConnectionStore(_filePath);
        var conn = new SavedConnection
        {
            Name = "prod",
            ConnectionString = "User Id=admin;Password=secret;Data Source=proddb:1521/ORCL"
        };

        store.Save(conn);

        var loaded = store.Get("prod");
        Assert.NotNull(loaded);
        Assert.Equal(conn.ConnectionString, loaded.ConnectionString);
        Assert.False(loaded.IsComponentBased);
    }

    [Fact]
    public void Save_PersistsToDisk_LoadableByNewInstance()
    {
        var store = new ConnectionStore(_filePath);
        store.Save(new SavedConnection { Name = "test", Username = "u", Password = "p", Host = "h" });

        var store2 = new ConnectionStore(_filePath);
        var loaded = store2.Get("test");
        Assert.NotNull(loaded);
        Assert.Equal("u", loaded.Username);
    }

    [Fact]
    public void Get_CaseInsensitive()
    {
        var store = new ConnectionStore(_filePath);
        store.Save(new SavedConnection { Name = "MyDb", Username = "u", Password = "p", Host = "h" });

        Assert.NotNull(store.Get("mydb"));
        Assert.NotNull(store.Get("MYDB"));
    }

    [Fact]
    public void Delete_RemovesConnection()
    {
        var store = new ConnectionStore(_filePath);
        store.Save(new SavedConnection { Name = "temp", Username = "u", Password = "p", Host = "h" });

        Assert.True(store.Delete("temp"));
        Assert.Null(store.Get("temp"));
    }

    [Fact]
    public void Delete_NonExistent_ReturnsFalse()
    {
        var store = new ConnectionStore(_filePath);
        Assert.False(store.Delete("nope"));
    }

    [Fact]
    public void Save_Overwrites_ExistingByName()
    {
        var store = new ConnectionStore(_filePath);
        store.Save(new SavedConnection { Name = "dev", Username = "old", Password = "p", Host = "h" });
        store.Save(new SavedConnection { Name = "dev", Username = "new", Password = "p", Host = "h" });

        var loaded = store.Get("dev");
        Assert.Equal("new", loaded!.Username);
        Assert.Single(store.GetAll());
    }

    [Fact]
    public void GetAll_ReturnsSortedByName()
    {
        var store = new ConnectionStore(_filePath);
        store.Save(new SavedConnection { Name = "zebra", Host = "h", Username = "u", Password = "p" });
        store.Save(new SavedConnection { Name = "alpha", Host = "h", Username = "u", Password = "p" });

        var all = store.GetAll();
        Assert.Equal("alpha", all[0].Name);
        Assert.Equal("zebra", all[1].Name);
    }

    [Fact]
    public void FuzzySearch_ExactMatch_ReturnsSingle()
    {
        var store = new ConnectionStore(_filePath);
        store.Save(new SavedConnection { Name = "dev", Host = "h", Username = "u", Password = "p" });
        store.Save(new SavedConnection { Name = "prod", Host = "h", Username = "u", Password = "p" });

        var results = store.FuzzySearch("dev");
        Assert.Single(results);
        Assert.Equal("dev", results[0].Name);
    }

    [Fact]
    public void FuzzySearch_PartialMatch_ReturnsMatches()
    {
        var store = new ConnectionStore(_filePath);
        store.Save(new SavedConnection { Name = "dev-oracle", Host = "h", Username = "u", Password = "p" });
        store.Save(new SavedConnection { Name = "dev-postgres", Host = "h", Username = "u", Password = "p" });
        store.Save(new SavedConnection { Name = "prod-oracle", Host = "h", Username = "u", Password = "p" });

        var results = store.FuzzySearch("dev");
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Contains("dev", r.Name));
    }

    [Fact]
    public void FuzzySearch_FuzzyMatch_ReturnsBestMatches()
    {
        var store = new ConnectionStore(_filePath);
        store.Save(new SavedConnection { Name = "sc344so1", Host = "h", Username = "u", Password = "p" });
        store.Save(new SavedConnection { Name = "sc344ow1", Host = "h", Username = "u", Password = "p" });
        store.Save(new SavedConnection { Name = "unrelated", Host = "h", Username = "u", Password = "p" });

        var results = store.FuzzySearch("sc344");
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.StartsWith("sc344", r.Name));
    }

    [Fact]
    public void FuzzySearch_NoMatch_ReturnsEmpty()
    {
        var store = new ConnectionStore(_filePath);
        store.Save(new SavedConnection { Name = "dev", Host = "h", Username = "u", Password = "p" });

        var results = store.FuzzySearch("zzz");
        Assert.Empty(results);
    }

    [Fact]
    public void FuzzySearch_CaseInsensitive()
    {
        var store = new ConnectionStore(_filePath);
        store.Save(new SavedConnection { Name = "DevOracle", Host = "h", Username = "u", Password = "p" });

        var results = store.FuzzySearch("devoracle");
        Assert.Single(results);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
