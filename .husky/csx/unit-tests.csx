return RunTests();

static int RunTests()
{
    string root = RepositoryRoot();
    var start = new System.Diagnostics.ProcessStartInfo("dotnet")
    {
        UseShellExecute = false,
        WorkingDirectory = root,
    };
    start.ArgumentList.Add("test");
    start.ArgumentList.Add("tests/Marvel.UnitTests.slnx");
    start.ArgumentList.Add("--configuration");
    start.ArgumentList.Add("Release");
    start.ArgumentList.Add("--nologo");

    using var process = System.Diagnostics.Process.Start(start)
        ?? throw new InvalidOperationException("Could not start dotnet");
    if (!process.WaitForExit(TimeSpan.FromMinutes(15)))
    {
        process.Kill(true);
        throw new TimeoutException("Unit tests did not finish within fifteen minutes");
    }
    return process.ExitCode;
}

static string RepositoryRoot()
{
    var start = new System.Diagnostics.ProcessStartInfo("git")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
    };
    start.ArgumentList.Add("rev-parse");
    start.ArgumentList.Add("--show-toplevel");
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
    string root = outputTask.GetAwaiter().GetResult().Trim();
    if (process.ExitCode != 0 || root.Length == 0)
    {
        throw new InvalidOperationException("Could not locate the repository root");
    }
    return root;
}
