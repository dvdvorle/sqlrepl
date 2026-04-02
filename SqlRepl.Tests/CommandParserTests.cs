namespace SqlRepl.Tests;

public class CommandParserTests
{
    // --- Empty input ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Parse_EmptyOrWhitespace_ReturnsEmpty(string? input)
    {
        var result = CommandParser.Parse(input ?? "");
        Assert.Equal(CommandType.Empty, result.Type);
    }

    // --- Exit ---

    [Theory]
    [InlineData("exit")]
    [InlineData("EXIT")]
    [InlineData("quit")]
    [InlineData("QUIT")]
    [InlineData("q")]
    [InlineData("Q")]
    public void Parse_ExitCommands_ReturnsExit(string input)
    {
        var result = CommandParser.Parse(input);
        Assert.Equal(CommandType.Exit, result.Type);
    }

    // --- Help ---

    [Theory]
    [InlineData("help")]
    [InlineData("HELP")]
    [InlineData("?")]
    public void Parse_HelpCommands_ReturnsHelp(string input)
    {
        var result = CommandParser.Parse(input);
        Assert.Equal(CommandType.Help, result.Type);
    }

    // --- Connect with user/pass@host ---

    [Fact]
    public void Parse_ConnectComponents_ParsesUserPassHost()
    {
        var result = CommandParser.Parse("conn scott/tiger@dbhost");

        Assert.Equal(CommandType.Connect, result.Type);
        Assert.Equal("scott", result.Username);
        Assert.Equal("tiger", result.Password);
        Assert.Equal("dbhost", result.Host);
        Assert.Null(result.ConnectionString);
    }

    [Fact]
    public void Parse_ConnectComponents_WithPortAndService()
    {
        var result = CommandParser.Parse("connect admin/secret@myhost:1522/MYDB");

        Assert.Equal(CommandType.Connect, result.Type);
        Assert.Equal("admin", result.Username);
        Assert.Equal("secret", result.Password);
        Assert.Equal("myhost:1522/MYDB", result.Host);
    }

    [Fact]
    public void Parse_ConnectComponents_CaseInsensitive()
    {
        var result = CommandParser.Parse("CONN user/pass@host");
        Assert.Equal(CommandType.Connect, result.Type);
        Assert.Equal("user", result.Username);
    }

    [Fact]
    public void Parse_ConnectLongForm_Works()
    {
        var result = CommandParser.Parse("connect user/pass@host");
        Assert.Equal(CommandType.Connect, result.Type);
        Assert.Equal("user", result.Username);
    }

    // --- Connect with quoted connection string ---

    [Fact]
    public void Parse_ConnectQuotedString_ParsesConnectionString()
    {
        var connStr = "User Id=scott;Password=tiger;Data Source=dbhost:1521/ORCL";
        var result = CommandParser.Parse($"conn \"{connStr}\"");

        Assert.Equal(CommandType.Connect, result.Type);
        Assert.Equal(connStr, result.ConnectionString);
        Assert.Null(result.Username);
    }

    // --- Connect with raw connection string (Key=Value) ---

    [Fact]
    public void Parse_ConnectRawString_ParsesConnectionString()
    {
        var connStr = "User Id=scott;Password=tiger;Data Source=dbhost:1521/ORCL";
        var result = CommandParser.Parse($"conn {connStr}");

        Assert.Equal(CommandType.Connect, result.Type);
        Assert.Equal(connStr, result.ConnectionString);
    }

    // --- SQL queries ---

    [Theory]
    [InlineData("SELECT * FROM dual")]
    [InlineData("select 1 from dual")]
    [InlineData("WITH cte AS (SELECT 1 FROM dual) SELECT * FROM cte")]
    [InlineData("INSERT INTO t VALUES (1)")]
    [InlineData("UPDATE t SET x = 1")]
    [InlineData("DELETE FROM t WHERE id = 1")]
    [InlineData("CREATE TABLE t (id NUMBER)")]
    [InlineData("DROP TABLE t")]
    [InlineData("ALTER TABLE t ADD col VARCHAR2(10)")]
    [InlineData("DESCRIBE my_table")]
    public void Parse_SqlStatements_ReturnsQuery(string sql)
    {
        var result = CommandParser.Parse(sql);

        Assert.Equal(CommandType.Query, result.Type);
        Assert.Equal(sql, result.RawInput);
    }

    [Fact]
    public void Parse_SqlWithLeadingWhitespace_TrimmedInRawInput()
    {
        var result = CommandParser.Parse("  SELECT 1 FROM dual  ");

        Assert.Equal(CommandType.Query, result.Type);
        Assert.Equal("SELECT 1 FROM dual", result.RawInput);
    }

    // --- Edge cases ---

    [Fact]
    public void Parse_ConnWithoutArgs_TreatsAsQuery()
    {
        // "conn" alone with no arguments doesn't match any connect regex
        var result = CommandParser.Parse("conn");
        Assert.Equal(CommandType.Query, result.Type);
    }

    [Fact]
    public void Parse_UnknownCommand_TreatsAsQuery()
    {
        var result = CommandParser.Parse("something random");
        Assert.Equal(CommandType.Query, result.Type);
    }
}
