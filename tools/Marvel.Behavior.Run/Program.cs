using Marvel.Behavior.Run;

if (args.Length != 1 || !string.Equals(args[0], "check", StringComparison.Ordinal))
{
    Console.Error.WriteLine("usage: Marvel.Behavior.Run check");
    return 2;
}

try
{
    string root = RepositoryRoot.Find(Environment.CurrentDirectory);
    var suite = new CoreTranscriptSuite(root);
    IReadOnlyList<TranscriptResult> results = suite.RunPassingCorpus();
    TranscriptException quarantine = suite.RunQuarantine();
    foreach (TranscriptResult result in results)
    {
        Console.WriteLine($"PASS {result.Obligation} {result.Digest}");
    }

    Console.WriteLine(
        $"PASS quarantine rejected: {quarantine.Message.Split(Environment.NewLine)[0]}");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}

internal static class RepositoryRoot
{
    public static string Find(string start)
    {
        for (var directory = new DirectoryInfo(Path.GetFullPath(start));
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Marvel.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            $"cannot find Marvel.slnx above '{Path.GetFullPath(start)}'");
    }
}
