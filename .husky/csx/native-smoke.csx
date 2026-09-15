return RunNativeSmoke();

static int RunNativeSmoke()
{
    string root = RepositoryRoot();
    string godot = ResolveGodot();
    var start = new System.Diagnostics.ProcessStartInfo("pwsh")
    {
        UseShellExecute = false,
        WorkingDirectory = root,
    };
    start.ArgumentList.Add("-NoProfile");
    start.ArgumentList.Add("-File");
    start.ArgumentList.Add(System.IO.Path.Combine(root, "tools", "godot-smoke.ps1"));
    start.ArgumentList.Add("-GodotBin");
    start.ArgumentList.Add(godot);

    using var process = System.Diagnostics.Process.Start(start)
        ?? throw new InvalidOperationException("Could not start native Godot smoke");
    if (!process.WaitForExit(TimeSpan.FromMinutes(10)))
    {
        process.Kill(true);
        throw new TimeoutException("Native Godot smoke did not finish within ten minutes");
    }
    return process.ExitCode;
}

static string ResolveGodot()
{
    var configured = Environment.GetEnvironmentVariable("GODOT_BIN");
    if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
    {
        return configured;
    }
    const string windowsDefault =
        @"C:\Tools\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe";
    if (OperatingSystem.IsWindows() && File.Exists(windowsDefault))
    {
        return windowsDefault;
    }
    throw new InvalidOperationException(
        "Set GODOT_BIN to the Godot 4.7 .NET executable before committing.");
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
