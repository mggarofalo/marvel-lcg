using Marvel.Client;

namespace Marvel.Godot;

/// <summary>Releases a setup attempt's connection unless the displayed session adopts it.</summary>
internal sealed class SetupConnectionLease(LocalGameClient client) : IDisposable
{
    internal LocalGameClient Client { get; } = client;
    private bool transferred;

    internal void TransferToSession() => transferred = true;

    public void Dispose()
    {
        if (!transferred) Client.Dispose();
    }
}
