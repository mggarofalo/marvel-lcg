const int MaximumComplexity = 10;

string root = RepositoryRoot();
int csharp = RunLizard(root, "csharp", SourceFiles(root, ".cs"));
int gdscript = RunLizard(root, "gdscript", SourceFiles(root, ".gd"));
return csharp != 0 || gdscript != 0 ? 1 : 0;

static int RunLizard(string root, string language, IEnumerable<string> paths)
{
    string[] inputs = [.. paths];
    var failed = false;
    for (int offset = 0; offset < inputs.Length; offset += 50)
    {
        int count = Math.Min(50, inputs.Length - offset);
        if (RunLizardBatch(root, language, inputs[offset..(offset + count)]) != 0)
        {
            failed = true;
        }
    }
    return failed ? 1 : 0;
}

static int RunLizardBatch(string root, string language, IEnumerable<string> inputs)
{
    var start = new System.Diagnostics.ProcessStartInfo("lizard")
    {
        UseShellExecute = false,
        WorkingDirectory = root,
    };
    start.ArgumentList.Add("--CCN");
    start.ArgumentList.Add(MaximumComplexity.ToString());
    start.ArgumentList.Add("--length");
    start.ArgumentList.Add("1000000");
    start.ArgumentList.Add("--warnings_only");
    start.ArgumentList.Add("-l");
    start.ArgumentList.Add(language);
    start.ArgumentList.Add("--");
    foreach (string path in inputs)
    {
        start.ArgumentList.Add(path);
    }

    using var process = System.Diagnostics.Process.Start(start)
        ?? throw new InvalidOperationException("Could not start lizard");
    if (!process.WaitForExit(TimeSpan.FromMinutes(5)))
    {
        process.Kill(true);
        throw new TimeoutException("Lizard did not finish within five minutes");
    }
    return process.ExitCode;
}

static IEnumerable<string> SourceFiles(string root, string extension)
{
    foreach (string path in GitOutput(
                 root, "ls-files", "-z", "--cached", "--others", "--exclude-standard")
             .Split('\0', StringSplitOptions.RemoveEmptyEntries))
    {
        if (Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase)
            && File.Exists(Path.Combine(root, path)))
        {
            yield return path;
        }
    }
}

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
