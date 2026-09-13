namespace Marvel.Sim;

internal sealed class SimulationUsageException : Exception
{
    public SimulationUsageException(string message) : base(message)
    {
    }

    public SimulationUsageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
