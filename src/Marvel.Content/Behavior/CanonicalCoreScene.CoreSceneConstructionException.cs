using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>A legal Core Set deal from which one behavioral transcript begins.</summary>

/// <summary>A construction failure tied to the exact authority and operation.</summary>
public sealed class CoreSceneConstructionException : InvalidOperationException
{
    /// <summary>Creates a named construction failure.</summary>
    public CoreSceneConstructionException(
        string authority, string operation, string reason, Exception? inner = null)
        : base($"{authority}; {operation}: {reason}", inner)
    {
        Authority = authority;
        Operation = operation;
    }

    /// <summary>The authority obligation whose legal scene was being built.</summary>
    public string Authority { get; }

    /// <summary>The operation rejected by the first invariant it violated.</summary>
    public string Operation { get; }
}
