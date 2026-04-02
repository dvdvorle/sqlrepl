using Microsoft.Data.Sqlite;

namespace SqlRepl;

public record HistoryEntry
{
    public long Id { get; init; }
    public string Command { get; init; } = "";
    public string Connection { get; init; } = "";
    public DateTime ExecutedAt { get; init; }
}

public class CommandHistory : IDisposable
{
    private readonly SqliteConnection _db;

    public CommandHistory(string? dbPath = null)
    {
        dbPath ??= GetDefaultPath();

        var dir = Path.GetDirectoryName(dbPath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        _db = new SqliteConnection($"Data Source={dbPath}");
        _db.Open();
        Initialize();
    }

    public static string GetDefaultPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".sqlrepl", "history.db");
    }

    public void Add(string command, string? connection = null)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "INSERT INTO history (command, connection, executed_at) VALUES ($command, $connection, $executed_at)";
        cmd.Parameters.AddWithValue("$command", command);
        cmd.Parameters.AddWithValue("$connection", connection ?? "");
        cmd.Parameters.AddWithValue("$executed_at", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();

        using var ftsCmd = _db.CreateCommand();
        ftsCmd.CommandText = "INSERT INTO history_fts (rowid, command) VALUES (last_insert_rowid(), $command)";
        ftsCmd.Parameters.AddWithValue("$command", command);
        ftsCmd.ExecuteNonQuery();
    }

    public IReadOnlyList<HistoryEntry> GetRecent(int limit = 50)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "SELECT id, command, connection, executed_at FROM history ORDER BY id DESC LIMIT $limit";
        cmd.Parameters.AddWithValue("$limit", limit);

        return ReadEntries(cmd);
    }

    public IReadOnlyList<HistoryEntry> Search(string query, int limit = 50)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            SELECT h.id, h.command, h.connection, h.executed_at
            FROM history_fts fts
            JOIN history h ON h.id = fts.rowid
            WHERE history_fts MATCH $query
            ORDER BY h.id DESC
            LIMIT $limit
            """;
        cmd.Parameters.AddWithValue("$query", $"\"{query}\"");
        cmd.Parameters.AddWithValue("$limit", limit);

        return ReadEntries(cmd);
    }

    private static List<HistoryEntry> ReadEntries(SqliteCommand cmd)
    {
        using var reader = cmd.ExecuteReader();
        var entries = new List<HistoryEntry>();
        while (reader.Read())
        {
            entries.Add(new HistoryEntry
            {
                Id = reader.GetInt64(0),
                Command = reader.GetString(1),
                Connection = reader.GetString(2),
                ExecutedAt = DateTime.Parse(reader.GetString(3)).ToUniversalTime()
            });
        }
        return entries;
    }

    private void Initialize()
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                command TEXT NOT NULL,
                connection TEXT NOT NULL DEFAULT '',
                executed_at TEXT NOT NULL
            );
            CREATE VIRTUAL TABLE IF NOT EXISTS history_fts USING fts5(command, content='');
            """;
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
    }
}
