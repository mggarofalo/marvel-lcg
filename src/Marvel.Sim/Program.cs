using Marvel.Session;

namespace Marvel.Sim;

/// <summary>The headless whole-game simulation entry point.</summary>
public static class Program
{
    /// <summary>Runs the requested simulation or replay.</summary>
    public static int Main(string[] args)
    {
        try
        {
            return CommandLine.Run(args, Console.Out, Console.Error);
        }
        catch (SimulationUsageException error)
        {
            Console.Error.WriteLine(error.Message);
            Console.Error.WriteLine(CommandLine.Usage);
            return 2;
        }
        catch (ReplayDivergenceException error)
        {
            Console.Error.WriteLine(error.Message);
            return 3;
        }
        catch (Exception error) when (error is IOException
            or UnauthorizedAccessException
            or System.Text.Json.JsonException)
        {
            Console.Error.WriteLine(error.Message);
            return 2;
        }
    }
}
