using Oracle.ManagedDataAccess.Client;
using Spectre.Console;

namespace SqlRepl;

/// <summary>
/// Manages Oracle database connections.
/// </summary>
public class ConnectionManager : IDisposable
{
    private OracleConnection? _connection;

    public bool IsConnected => _connection?.State == System.Data.ConnectionState.Open;

    public string? CurrentDataSource => _connection?.DataSource;

    /// <summary>
    /// Connects using an explicit connection string.
    /// </summary>
    public async Task ConnectAsync(string connectionString)
    {
        await DisconnectAsync();
        _connection = new OracleConnection(connectionString);
        await _connection.OpenAsync();
    }

    /// <summary>
    /// Connects using individual components (user, password, host with optional port/service).
    /// Host format: host[:port][/service]
    /// </summary>
    public async Task ConnectAsync(string username, string password, string host)
    {
        var connectionString = BuildConnectionString(username, password, host);
        await ConnectAsync(connectionString);
    }

    public OracleConnection GetConnection()
    {
        if (_connection is null || !IsConnected)
            throw new InvalidOperationException("Not connected. Use 'conn' to connect first.");
        return _connection;
    }

    public async Task DisconnectAsync()
    {
        if (_connection is not null)
        {
            if (_connection.State != System.Data.ConnectionState.Closed)
                await _connection.CloseAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private static string BuildConnectionString(string username, string password, string host)
    {
        // Host can be a TNS alias (e.g. "myalias"), or explicit host[:port][/service].
        // Pass through as-is to let Oracle resolve aliases via tnsnames.ora.
        return $"User Id={username};Password={password};Data Source={host}";
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _connection = null;
    }
}
