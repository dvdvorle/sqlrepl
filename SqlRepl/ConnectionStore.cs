using System.Text.Json;
using System.Text.Json.Serialization;
using FuzzySharp;

namespace SqlRepl;

public record SavedConnection
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("connectionString")]
    public string? ConnectionString { get; init; }

    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("password")]
    public string? Password { get; init; }

    [JsonPropertyName("host")]
    public string? Host { get; init; }

    [JsonIgnore]
    public bool IsComponentBased => Username is not null && Host is not null;
}

public class ConnectionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private Dictionary<string, SavedConnection> _connections = new(StringComparer.OrdinalIgnoreCase);

    public ConnectionStore(string? filePath = null)
    {
        _filePath = filePath ?? GetDefaultPath();
        Load();
    }

    public static string GetDefaultPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".sqlrepl", "connections.json");
    }

    public IReadOnlyList<SavedConnection> GetAll() =>
        _connections.Values.OrderBy(c => c.Name).ToList();

    public SavedConnection? Get(string name) =>
        _connections.GetValueOrDefault(name);

    public IReadOnlyList<SavedConnection> FuzzySearch(string query, int threshold = 70)
    {
        if (string.IsNullOrWhiteSpace(query))
            return GetAll();

        var q = query.ToLowerInvariant();

        return _connections.Values
            .Select(c =>
            {
                var name = c.Name.ToLowerInvariant();
                // Prefer substring containment, then fall back to full-string ratio
                var score = name.Contains(q)
                    ? 100
                    : Fuzz.Ratio(q, name);
                return new { Connection = c, Score = score };
            })
            .Where(x => x.Score >= threshold)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Connection.Name)
            .Select(x => x.Connection)
            .ToList();
    }

    public void Save(SavedConnection connection)
    {
        _connections[connection.Name] = connection;
        Persist();
    }

    public bool Delete(string name)
    {
        var removed = _connections.Remove(name);
        if (removed) Persist();
        return removed;
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
            return;

        var json = File.ReadAllText(_filePath);
        var list = JsonSerializer.Deserialize<List<SavedConnection>>(json, JsonOptions) ?? [];
        _connections = new Dictionary<string, SavedConnection>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in list)
            _connections[c.Name] = c;
    }

    private void Persist()
    {
        var dir = Path.GetDirectoryName(_filePath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(_connections.Values.OrderBy(c => c.Name).ToList(), JsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
