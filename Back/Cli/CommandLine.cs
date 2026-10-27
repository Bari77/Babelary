namespace Cli;

/// <summary>
/// Local command line. Network commands require <c>BABELARY_TOKEN</c> and never print it.
/// </summary>
public static class CommandLine
{
    /// <summary>
    /// Runs one invocation and returns the process exit code.
    /// </summary>
    public static int Run(string[] args, string? token, TextWriter stdout, TextWriter stderr)
    {
        if (args.Length == 0 || args is ["--help"] or ["-h"] or ["help"])
        {
            WriteHelp(stdout);
            return 0;
        }

        if (args is ["concat", ..])
        {
            stderr.WriteLine("concat is not available yet.");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            stderr.WriteLine("BABELARY_TOKEN is missing. Stopped before any network call.");
            return 1;
        }

        stderr.WriteLine("This command is provided by the babelary npm package.");
        return 1;
    }

    private static void WriteHelp(TextWriter stdout)
    {
        stdout.WriteLine("babelary — syncs a repository with a Babelary instance.");
        stdout.WriteLine();
        stdout.WriteLine("The token is read from BABELARY_TOKEN. It does not belong in babelary.yml.");
        stdout.WriteLine();
        stdout.WriteLine("Commands:");
        stdout.WriteLine("  push sources");
        stdout.WriteLine("  push translations");
        stdout.WriteLine("  pull");
        stdout.WriteLine("  status");
        stdout.WriteLine("  concat");
    }
}
