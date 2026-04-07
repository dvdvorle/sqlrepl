using Microsoft.Extensions.Configuration;

namespace SqlRepl;

public class ReplSettings
{
    public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    public string DateOnlyFormat { get; set; } = "yyyy-MM-dd";
    public int PageSize { get; set; } = 50;
    public bool AutoReconnect { get; set; } = true;

    public static ReplSettings Load()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables("SQLREPL_")
            .Build();

        var settings = new ReplSettings();

        var dateFormat = config["DateFormat"];
        if (!string.IsNullOrEmpty(dateFormat))
            settings.DateFormat = dateFormat;

        var dateOnlyFormat = config["DateOnlyFormat"];
        if (!string.IsNullOrEmpty(dateOnlyFormat))
            settings.DateOnlyFormat = dateOnlyFormat;

        var pageSize = config["PageSize"];
        if (!string.IsNullOrEmpty(pageSize) && int.TryParse(pageSize, out var ps) && ps > 0)
            settings.PageSize = ps;

        var autoReconnect = config["AutoReconnect"];
        if (!string.IsNullOrEmpty(autoReconnect) && bool.TryParse(autoReconnect, out var ar))
            settings.AutoReconnect = ar;

        return settings;
    }
}
