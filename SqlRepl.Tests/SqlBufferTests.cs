namespace SqlRepl.Tests;

public class SqlBufferTests
{
    [Fact]
    public void Append_WithoutSemicolon_Buffers()
    {
        var buffer = new SqlBuffer();
        var result = buffer.Append("SELECT *");
        Assert.False(result.IsComplete);
        Assert.Null(result.Sql);
        Assert.True(buffer.IsBuffering);
    }

    [Fact]
    public void Append_WithSemicolon_ReturnsComplete()
    {
        var buffer = new SqlBuffer();
        var result = buffer.Append("SELECT 1 FROM dual;");
        Assert.True(result.IsComplete);
        Assert.Equal("SELECT 1 FROM dual", result.Sql);
        Assert.False(buffer.IsBuffering);
    }

    [Fact]
    public void Append_MultipleLines_JoinsWithNewline()
    {
        var buffer = new SqlBuffer();

        var r1 = buffer.Append("SELECT *");
        Assert.False(r1.IsComplete);

        var r2 = buffer.Append("FROM employees");
        Assert.False(r2.IsComplete);

        var r3 = buffer.Append("WHERE dept = 'IT';");
        Assert.True(r3.IsComplete);
        Assert.Equal("SELECT *\nFROM employees\nWHERE dept = 'IT'", r3.Sql);
    }

    [Fact]
    public void Append_SingleLineWithSemicolon_NoBuffering()
    {
        var buffer = new SqlBuffer();
        var result = buffer.Append("SELECT 1;");
        Assert.True(result.IsComplete);
        Assert.Equal("SELECT 1", result.Sql);
    }

    [Fact]
    public void Append_AfterComplete_StartsNewStatement()
    {
        var buffer = new SqlBuffer();
        buffer.Append("SELECT 1;");

        var result = buffer.Append("SELECT 2;");
        Assert.True(result.IsComplete);
        Assert.Equal("SELECT 2", result.Sql);
    }

    [Fact]
    public void Clear_ResetsBuffer()
    {
        var buffer = new SqlBuffer();
        buffer.Append("SELECT *");
        Assert.True(buffer.IsBuffering);

        buffer.Clear();
        Assert.False(buffer.IsBuffering);
    }

    [Fact]
    public void Append_SemicolonOnly_ReturnsEmptyComplete()
    {
        var buffer = new SqlBuffer();
        var result = buffer.Append(";");
        Assert.True(result.IsComplete);
        Assert.Equal("", result.Sql);
    }

    [Fact]
    public void Append_TrailingWhitespaceAfterSemicolon_StillTerminates()
    {
        var buffer = new SqlBuffer();
        var result = buffer.Append("SELECT 1;  ");
        Assert.True(result.IsComplete);
        Assert.Equal("SELECT 1", result.Sql);
    }
}
