using Microsoft.Data.Sqlite;

namespace SqlRepl;

public record HistoryEntry
{
    public long Id { get; init; }
    public string Command { get; init; } = "";
    public string Connection { get; init; } = "";
    public DateTime LastExecutedAt { get; init; }
    public int ExecutionCount { get; init; }
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
        connection ??= "";

        // Check if command already exists
        using var check = _db.CreateCommand();
        check.CommandText = "SELECT id FROM commands WHERE command = $command";
        check.Parameters.AddWithValue("$command", command);
        var existing = check.ExecuteScalar();

        long commandId;
        if (existing is not null)
        {
            commandId = (long)existing;
        }
        else
        {
            // Insert new command
            using var insert = _db.CreateCommand();
            insert.CommandText = "INSERT INTO commands (command) VALUES ($command)";
            insert.Parameters.AddWithValue("$command", command);
            insert.ExecuteNonQuery();

            using var getId = _db.CreateCommand();
            getId.CommandText = "SELECT last_insert_rowid()";
            commandId = (long)getId.ExecuteScalar()!;

            // Index in FTS
            using var fts = _db.CreateCommand();
            fts.CommandText = "INSERT INTO commands_fts (rowid, command) VALUES ($id, $command)";
            fts.Parameters.AddWithValue("$id", commandId);
            fts.Parameters.AddWithValue("$command", command);
            fts.ExecuteNonQuery();
        }

        // Record execution
        using var exec = _db.CreateCommand();
        exec.CommandText = "INSERT INTO executions (command_id, connection, executed_at) VALUES ($cid, $conn, $ts)";
        exec.Parameters.AddWithValue("$cid", commandId);
        exec.Parameters.AddWithValue("$conn", connection);
        exec.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("o"));
        exec.ExecuteNonQuery();
    }

    public IReadOnlyList<HistoryEntry> GetRecent(int limit = 50)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            SELECT c.id, c.command, e.connection, e.executed_at, e.cnt
            FROM commands c
            JOIN (
                SELECT command_id, connection, MAX(executed_at) AS executed_at, COUNT(*) AS cnt
                FROM executions
                GROUP BY command_id
            ) e ON e.command_id = c.id
            ORDER BY e.executed_at DESC
            LIMIT $limit
            """;
        cmd.Parameters.AddWithValue("$limit", limit);

        return ReadEntries(cmd);
    }

    public IReadOnlyList<HistoryEntry> Search(string query, int limit = 50)
    {
        if (query.Length < 3)
        {
            // Trigram needs at least 3 chars; fall back to LIKE
            using var likeCmd = _db.CreateCommand();
            likeCmd.CommandText = """
                SELECT c.id, c.command, e.connection, e.executed_at, e.cnt
                FROM commands c
                JOIN (
                    SELECT command_id, connection, MAX(executed_at) AS executed_at, COUNT(*) AS cnt
                    FROM executions
                    GROUP BY command_id
                ) e ON e.command_id = c.id
                WHERE c.command LIKE $pattern
                ORDER BY e.executed_at DESC
                LIMIT $limit
                """;
            likeCmd.Parameters.AddWithValue("$pattern", $"%{query}%");
            likeCmd.Parameters.AddWithValue("$limit", limit);
            return ReadEntries(likeCmd);
        }

        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            SELECT c.id, c.command, e.connection, e.executed_at, e.cnt
            FROM commands_fts fts
            JOIN commands c ON c.id = fts.rowid
            JOIN (
                SELECT command_id, connection, MAX(executed_at) AS executed_at, COUNT(*) AS cnt
                FROM executions
                GROUP BY command_id
            ) e ON e.command_id = c.id
            WHERE commands_fts MATCH $query
            ORDER BY e.executed_at DESC
            LIMIT $limit
            """;
        cmd.Parameters.AddWithValue("$query", query);
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
                LastExecutedAt = DateTime.Parse(reader.GetString(3)).ToUniversalTime(),
                ExecutionCount = reader.GetInt32(4)
            });
        }
        return entries;
    }

    private void Initialize()
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS commands (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                command TEXT NOT NULL UNIQUE
            );
            CREATE TABLE IF NOT EXISTS executions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                command_id INTEGER NOT NULL REFERENCES commands(id),
                connection TEXT NOT NULL DEFAULT '',
                executed_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_executions_command_id ON executions(command_id);
            CREATE VIRTUAL TABLE IF NOT EXISTS commands_fts USING fts5(
                command,
                content='',
                tokenize='trigram'
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
    }
}
