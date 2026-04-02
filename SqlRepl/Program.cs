using Spectre.Console;

namespace SqlRepl;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var repl = new Repl();
        await repl.RunAsync();
        return 0;
    }
}
