using Godot;

namespace Marvel.Godot;

/// <summary>Starts the packaged multiplayer acceptance driver from an explicit user argument.</summary>
internal static class PackagedHostedSmoke
{
    internal const string Argument = "--marvel-hosted-multiplayer-smoke";
    private const string ScenePath = "res://smoke/HostedMultiplayerSmoke.tscn";
    private static bool started;

    internal static bool IsRequested(IEnumerable<string> arguments) =>
        arguments.Contains(Argument, StringComparer.Ordinal);

    internal static bool TryStart(Control owner)
    {
        if (started || !IsRequested(OS.GetCmdlineUserArgs()))
            return false;

        started = true;
        GD.Print("PACKAGED_HOSTED_SMOKE_ENTRYPOINT");
        PackedScene? scene = GD.Load<PackedScene>(ScenePath);
        if (scene is null)
        {
            GD.PushError("the packaged multiplayer smoke scene is unavailable");
            owner.GetTree().Quit(1);
            return true;
        }

        owner.Hide();
        owner.GetTree().Root.CallDeferred(Node.MethodName.AddChild, scene.Instantiate());
        return true;
    }
}
