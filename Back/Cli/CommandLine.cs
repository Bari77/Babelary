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
            stderr.WriteLine("concat n'est pas encore disponible.");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            stderr.WriteLine("BABELARY_TOKEN est absent. Arrêt avant tout appel réseau.");
            return 1;
        }

        stderr.WriteLine("Cette commande n'est pas encore disponible.");
        return 1;
    }

    private static void WriteHelp(TextWriter stdout)
    {
        stdout.WriteLine("babelary — synchronise un dépôt avec une instance Babelary.");
        stdout.WriteLine();
        stdout.WriteLine("Le jeton se lit dans BABELARY_TOKEN. Il n'a pas sa place dans babelary.yml.");
        stdout.WriteLine();
        stdout.WriteLine("Commandes :");
        stdout.WriteLine("  push sources");
        stdout.WriteLine("  push translations");
        stdout.WriteLine("  pull");
        stdout.WriteLine("  status");
        stdout.WriteLine("  concat");
    }
}
