using System.Reflection;

namespace SqlRepl.Tests;

public class ConnectionManagerBuildConnectionStringTests
{
    // Use reflection to test the private static method
    private static string BuildConnectionString(string username, string password, string host)
    {
        var method = typeof(ConnectionManager).GetMethod(
            "BuildConnectionString",
            BindingFlags.NonPublic | BindingFlags.Static);

        return (string)method!.Invoke(null, [username, password, host])!;
    }

    [Fact]
    public void TnsAlias_ShouldNotAppendPortOrService()
    {
        // A plain alias like "sc344so1" should be used as-is for Data Source
        var result = BuildConnectionString("scott", "tiger", "sc344so1");

        Assert.Equal("User Id=scott;Password=tiger;Data Source=sc344so1", result);
    }

    [Fact]
    public void HostWithPortAndService_ShouldUseExplicitValues()
    {
        var result = BuildConnectionString("scott", "tiger", "myhost:1522/MYDB");

        Assert.Equal("User Id=scott;Password=tiger;Data Source=myhost:1522/MYDB", result);
    }

    [Fact]
    public void HostWithServiceOnly_ShouldUseHostAndService()
    {
        var result = BuildConnectionString("scott", "tiger", "myhost/MYDB");

        Assert.Equal("User Id=scott;Password=tiger;Data Source=myhost/MYDB", result);
    }

    [Fact]
    public void HostWithPortOnly_ShouldUseHostAndPort()
    {
        var result = BuildConnectionString("scott", "tiger", "myhost:1522");

        Assert.Equal("User Id=scott;Password=tiger;Data Source=myhost:1522", result);
    }
}
