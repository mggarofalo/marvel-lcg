using System.ComponentModel;
using System.Diagnostics;

namespace Marvel.MarvelCdb.Harvest;

public readonly record struct CommandResult(int ExitCode, string Output, string Error);

public interface ICommandRunner
{
    CommandResult Run(IReadOnlyList<string> arguments);
}

public sealed class MarvelCdbCommand : ICommandRunner
{
    public CommandResult Run(IReadOnlyList<string> arguments)
    {
        var start = new ProcessStartInfo("marvelcdb")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        try
        {
            using Process process = Process.Start(start)
                ?? throw new InvalidOperationException("could not start marvelcdb");
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> error = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            Task.WaitAll(output, error);
            return new CommandResult(process.ExitCode, output.Result, error.Result);
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException(
                "marvelcdb is not on PATH. Install it with `go install "
                + "github.com/mggarofalo/marvelcdb-cli/cmd/marvelcdb@latest`. "
                + "It is an acquisition tool, not a build dependency.",
                exception);
        }
    }
}
