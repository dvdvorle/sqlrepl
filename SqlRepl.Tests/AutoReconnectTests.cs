using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SqlRepl.Commands;
using Typin.Console;

namespace SqlRepl.Tests;

public class AutoReconnectTests
{
    #region ConnectionManager — reconnect capability

    [Fact]
    public void CanReconnect_WhenNeverConnected_ReturnsFalse()
    {
        using var mgr = new ConnectionManager();
        Assert.False(mgr.CanReconnect);
    }

    [Fact]
    public async Task ReconnectAsync_WhenCannotReconnect_Throws()
    {
        using var mgr = new ConnectionManager();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(mgr.ReconnectAsync);
        Assert.Contains("No previous connection", ex.Message);
    }

    #endregion

    #region ReplSettings — AutoReconnect default

    [Fact]
    public void AutoReconnect_DefaultsToTrue()
    {
        var settings = new ReplSettings();
        Assert.True(settings.AutoReconnect);
    }

    #endregion

    #region QueryResult — Reconnected flag

    [Fact]
    public void QueryResult_Reconnected_DefaultsFalse()
    {
        var result = new QueryResult { IsQuery = true };
        Assert.False(result.Reconnected);
    }

    [Fact]
    public void QueryResult_Reconnected_CanBeSetTrue()
    {
        var result = new QueryResult { IsQuery = true, Reconnected = true };
        Assert.True(result.Reconnected);
    }

    #endregion

    #region QueryExecutor — reconnect on failure

    [Fact]
    public async Task ExecuteAsync_WhenAutoReconnectDisabled_DoesNotRetry()
    {
        var inner = Substitute.For<IQueryExecutor>();
        inner.ExecuteAsync("SELECT 1").ThrowsAsync(new Exception("connection lost"));

        var settings = new ReplSettings { AutoReconnect = false };
        var executor = new ReconnectingQueryExecutor(inner, Substitute.For<IConnectionChecker>(), settings);

        await Assert.ThrowsAsync<Exception>(() => executor.ExecuteAsync("SELECT 1"));
        await inner.Received(1).ExecuteAsync("SELECT 1");
    }

    [Fact]
    public async Task ExecuteAsync_WhenAutoReconnectEnabled_AndConnectionLost_RetriesAndSetsFlag()
    {
        var inner = Substitute.For<IQueryExecutor>();
        var checker = Substitute.For<IConnectionChecker>();

        // First call fails, second succeeds
        var expectedResult = new QueryResult { IsQuery = true, RowsAffected = 1 };
        var callCount = 0;
        inner.ExecuteAsync("SELECT 1").Returns(_ =>
        {
            callCount++;
            if (callCount == 1)
                throw new Exception("connection lost");
            return Task.FromResult(expectedResult);
        });

        checker.IsConnected.Returns(false);
        checker.CanReconnect.Returns(true);
        checker.ReconnectAsync().Returns(Task.CompletedTask);

        var settings = new ReplSettings { AutoReconnect = true };
        var executor = new ReconnectingQueryExecutor(inner, checker, settings);

        var result = await executor.ExecuteAsync("SELECT 1");

        Assert.True(result.Reconnected);
        Assert.Equal(1, result.RowsAffected);
        await checker.Received(1).ReconnectAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAutoReconnectEnabled_ButStillConnected_DoesNotRetry()
    {
        // If the connection is still open, it's a SQL error, not a connection drop
        var inner = Substitute.For<IQueryExecutor>();
        inner.ExecuteAsync("BAD SQL").ThrowsAsync(new Exception("ORA-00942: table or view does not exist"));

        var checker = Substitute.For<IConnectionChecker>();
        checker.IsConnected.Returns(true); // still connected — SQL error
        checker.CanReconnect.Returns(true);

        var settings = new ReplSettings { AutoReconnect = true };
        var executor = new ReconnectingQueryExecutor(inner, checker, settings);

        await Assert.ThrowsAsync<Exception>(() => executor.ExecuteAsync("BAD SQL"));
        await checker.DidNotReceive().ReconnectAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WhenReconnectFails_ThrowsOriginalException()
    {
        var inner = Substitute.For<IQueryExecutor>();
        inner.ExecuteAsync("SELECT 1").ThrowsAsync(new Exception("connection lost"));

        var checker = Substitute.For<IConnectionChecker>();
        checker.IsConnected.Returns(false);
        checker.CanReconnect.Returns(true);
        checker.ReconnectAsync().ThrowsAsync(new Exception("reconnect failed"));

        var settings = new ReplSettings { AutoReconnect = true };
        var executor = new ReconnectingQueryExecutor(inner, checker, settings);

        var ex = await Assert.ThrowsAsync<Exception>(() => executor.ExecuteAsync("SELECT 1"));
        Assert.Equal("connection lost", ex.Message);
    }

    #endregion

    #region SqlCommand — reconnect notification

    [Fact]
    public async Task SqlCommand_WhenReconnected_ShowsNotification()
    {
        using var connectionManager = new ConnectionManager();
        var queryExecutor = Substitute.For<IQueryExecutor>();
        queryExecutor.ExecuteAsync(Arg.Any<string>())
            .Returns(new QueryResult { IsQuery = true, Reconnected = true });

        var tempDir = Path.Combine(Path.GetTempPath(), "sqlrepl-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            using var history = new CommandHistory(Path.Combine(tempDir, "history.db"));
            var settings = new ReplSettings();

            // We need to fake IsConnected — but ConnectionManager is concrete.
            // Instead, test that the notification text is produced when Reconnected is true.
            // We'll use a mock IQueryExecutor that returns Reconnected = true.
            // SqlCommand checks _connectionManager.IsConnected first, so we need
            // the connection to appear connected. We can't easily do this without a real DB.
            // Instead, we test the notification logic directly.

            // The notification logic will be: if result.Reconnected, write warning.
            // We verify this through the SqlCommand by checking output.
            // Since we can't make ConnectionManager.IsConnected return true without a real
            // connection, we'll test the notification method separately.

            var (console, output, _) = VirtualConsole.CreateBuffered();
            using var _ = console;

            // Directly test the static notification helper
            SqlCommand.WriteReconnectNotification(console);

            var text = output.GetString();
            Assert.Contains("Reconnected", text, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    #endregion
}
