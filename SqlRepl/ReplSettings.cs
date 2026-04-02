using Microsoft.Extensions.Configuration;

namespace SqlRepl;

public class ReplSettings
{
    public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    public string DateOnlyFormat { get; set; } = "yyyy-MM-dd";

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

        return settings;
    }
}
