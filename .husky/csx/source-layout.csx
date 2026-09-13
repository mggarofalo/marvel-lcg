return RunPolicy();

static int RunPolicy()
{
    string root = RepositoryRoot();
    var start = new System.Diagnostics.ProcessStartInfo("dotnet")
    {
        UseShellExecute = false,
        WorkingDirectory = root,
    };
    start.ArgumentList.Add("test");
    start.ArgumentList.Add("tests/Marvel.Architecture.Tests/Marvel.Architecture.Tests.csproj");
    start.ArgumentList.Add("--configuration");
    start.ArgumentList.Add("Release");
    start.ArgumentList.Add("--nologo");
    start.ArgumentList.Add("--filter");
    start.ArgumentList.Add("FullyQualifiedName~SourceLayoutPolicyTests");

    using var process = System.Diagnostics.Process.Start(start)
        ?? throw new InvalidOperationException("Could not start dotnet");
    if (!process.WaitForExit(TimeSpan.FromMinutes(10)))
    {
        process.Kill(true);
        throw new TimeoutException("Source layout policy did not finish within ten minutes");
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
    string root = process.StandardOutput.ReadToEnd().Trim();
    if (!process.WaitForExit(TimeSpan.FromMinutes(1))
        || process.ExitCode != 0
        || root.Length == 0)
    {
        throw new InvalidOperationException("Could not locate the repository root");
    }
    return root;
}
