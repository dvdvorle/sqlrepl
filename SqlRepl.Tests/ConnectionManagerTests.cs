namespace SqlRepl.Tests;

public class ConnectionManagerTests
{
    [Fact]
    public void IsConnected_WhenNew_ReturnsFalse()
    {
        using var mgr = new ConnectionManager();
        Assert.False(mgr.IsConnected);
    }

    [Fact]
    public void CurrentDataSource_WhenNew_ReturnsNull()
    {
        using var mgr = new ConnectionManager();
        Assert.Null(mgr.CurrentDataSource);
    }

    [Fact]
    public void GetConnection_WhenNotConnected_Throws()
    {
        using var mgr = new ConnectionManager();
        var ex = Assert.Throws<InvalidOperationException>(() => mgr.GetConnection());
        Assert.Contains("Not connected", ex.Message);
    }

    [Fact]
    public async Task ConnectAsync_WithInvalidHost_ThrowsOracleException()
    {
        using var mgr = new ConnectionManager();
        // Should fail to connect to a nonexistent host
        await Assert.ThrowsAnyAsync<Exception>(
            () => mgr.ConnectAsync("user", "pass", "nonexistent_host:9999/BADDB"));
    }

    [Fact]
    public async Task DisconnectAsync_WhenNotConnected_DoesNotThrow()
    {
        using var mgr = new ConnectionManager();
        await mgr.DisconnectAsync(); // should be a no-op
        Assert.False(mgr.IsConnected);
    }
}
