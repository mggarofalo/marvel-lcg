const int MaximumLines = 500;

string root = RepositoryRoot();
var failed = false;
var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    ".cs",
    ".csx",
    ".gd",
    ".ps1",
    ".sh",
};
foreach (string path in SourceFiles(root))
{
    if (!extensions.Contains(Path.GetExtension(path))
        || !File.Exists(Path.Combine(root, path)))
    {
        continue;
    }

    var lines = 0;
    using var reader = File.OpenText(Path.Combine(root, path));
    while (lines <= MaximumLines && reader.ReadLine() is not null)
    {
        lines++;
    }

    if (lines <= MaximumLines)
    {
        continue;
    }

    Console.Error.WriteLine(
        $"{path}: file has more than {MaximumLines} lines; split it before committing");
    failed = true;
}

return failed ? 1 : 0;

static IEnumerable<string> SourceFiles(string root) =>
    GitOutput(root, "ls-files", "-z", "--cached", "--others", "--exclude-standard")
        .Split('\0', StringSplitOptions.RemoveEmptyEntries);

static string RepositoryRoot()
{
    string root = GitOutput(Environment.CurrentDirectory, "rev-parse", "--show-toplevel").Trim();
    if (root.Length == 0)
    {
        throw new InvalidOperationException("Git returned an empty repository root");
    }
    return root;
}

static string GitOutput(string workingDirectory, params string[] arguments)
{
    var start = new System.Diagnostics.ProcessStartInfo("git")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        WorkingDirectory = workingDirectory,
    };
    foreach (string argument in arguments)
    {
        start.ArgumentList.Add(argument);
    }

    using var process = System.Diagnostics.Process.Start(start)
        ?? throw new InvalidOperationException("Could not start git");
    var outputTask = process.StandardOutput.ReadToEndAsync();
    using var timeout = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(1));
    try
    {
        process.WaitForExitAsync(timeout.Token).GetAwaiter().GetResult();
    }
    catch (System.OperationCanceledException)
    {
        process.Kill(true);
        throw new TimeoutException("Git did not finish within one minute");
    }
    string output = outputTask.GetAwaiter().GetResult();
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException("Git command failed");
    }
    return output;
}
