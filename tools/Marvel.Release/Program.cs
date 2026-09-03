namespace Marvel.Release;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            return args.FirstOrDefault() switch
            {
                "manifest" => Manifest(args[1..]),
                "msix-version" => MsixVersion(args[1..]),
                _ => throw new ArgumentException(
                    "usage: Marvel.Release manifest --version V --commit SHA --data-root DIR --output FILE | msix-version V"),
            };
        }
        catch (Exception failure) when (failure is ArgumentException
            or IOException
            or InvalidOperationException)
        {
            Console.Error.WriteLine($"release input rejected: {failure.Message}");
            return 2;
        }
    }

    private static int Manifest(string[] args)
    {
        Dictionary<string, string> options = Options(args);
        string output = Required(options, "--output");
        string? directory = Path.GetDirectoryName(Path.GetFullPath(output));
        if (directory is null || !Directory.Exists(directory))
        {
            throw new ArgumentException("manifest output directory does not exist");
        }

        if (File.Exists(output))
        {
            throw new ArgumentException("manifest output already exists");
        }

        ReleaseManifest manifest = ReleaseManifest.Create(
            ReleaseVersion.Parse(Required(options, "--version")),
            Required(options, "--commit"),
            Required(options, "--data-root"));
        File.WriteAllText(output, manifest.Json());
        return 0;
    }

    private static int MsixVersion(string[] args)
    {
        if (args.Length != 1)
        {
            throw new ArgumentException("msix-version requires exactly one version");
        }

        Console.WriteLine(ReleaseVersion.Parse(args[0]).MsixVersion);
        return 0;
    }

    private static Dictionary<string, string> Options(string[] args)
    {
        if (args.Length == 0 || args.Length % 2 != 0)
        {
            throw new ArgumentException("manifest options must be name/value pairs");
        }

        var parsed = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int index = 0; index < args.Length; index += 2)
        {
            if (!parsed.TryAdd(args[index], args[index + 1]))
            {
                throw new ArgumentException($"duplicate option {args[index]}");
            }
        }

        string[] allowed = ["--version", "--commit", "--data-root", "--output"];
        string? unknown = parsed.Keys.FirstOrDefault(key => !allowed.Contains(key, StringComparer.Ordinal));
        if (unknown is not null)
        {
            throw new ArgumentException($"unknown option {unknown}");
        }

        return parsed;
    }

    private static string Required(Dictionary<string, string> options, string name) =>
        options.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"{name} is required");
}
