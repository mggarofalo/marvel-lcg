namespace Marvel.Server.Host;

/// <summary>Read-only incident export paths and destination.</summary>
internal sealed record IncidentExportOptions(
    string Output,
    string DataRoot,
    string SaveRoot,
    string DiagnosticsRoot)
{
    public static IncidentExportOptions Parse(string[] args)
    {
        string? output = null;
        string dataRoot = Environment.CurrentDirectory;
        string? saveRoot = null;
        string? diagnosticsRoot = null;
        for (int index = 0; index < args.Length; index++)
        {
            string option = args[index];
            string value = NextValue(args, ref index, option);
            switch (option)
            {
                case "--export-incident":
                    output = value;
                    break;
                case "--data-root":
                    dataRoot = value;
                    break;
                case "--save-root":
                    saveRoot = value;
                    break;
                case "--diagnostics-root":
                    diagnosticsRoot = value;
                    break;
                default:
                    throw new ArgumentException("unsupported incident export option");
            }
        }

        return RequiredOptions(output, dataRoot, saveRoot, diagnosticsRoot);
    }

    private static IncidentExportOptions RequiredOptions(
        string? output, string dataRoot, string? saveRoot, string? diagnosticsRoot) =>
        new(output ?? throw new ArgumentException("--export-incident is required"),
            dataRoot, saveRoot ?? throw new ArgumentException("--save-root is required"),
            diagnosticsRoot ?? throw new ArgumentException("--diagnostics-root is required"));

    private static string NextValue(string[] args, ref int index, string option)
    {
        if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
            throw new ArgumentException($"{option} requires a value");
        return args[index];
    }
}
