namespace SqlRepl;

public record BufferResult(bool IsComplete, string? Sql);

public class SqlBuffer
{
    private readonly List<string> _lines = [];

    public bool IsBuffering => _lines.Count > 0;

    public BufferResult Append(string line)
    {
        var trimmed = line.TrimEnd();
        if (trimmed.EndsWith(';'))
        {
            // Terminate: strip the semicolon, join with buffered lines, return
            var currentLine = trimmed[..^1].Trim();
            string sql;
            if (_lines.Count > 0)
            {
                if (currentLine.Length > 0)
                    _lines.Add(currentLine);
                sql = string.Join("\n", _lines);
                _lines.Clear();
            }
            else
            {
                sql = currentLine;
            }
            return new BufferResult(true, sql);
        }

        // No semicolon — buffer this line
        _lines.Add(line);
        return new BufferResult(false, null);
    }

    public void Clear()
    {
        _lines.Clear();
    }
}
