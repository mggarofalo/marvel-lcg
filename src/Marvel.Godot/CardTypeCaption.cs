namespace Marvel.Godot;

/// <summary>Names the printed type independently of an encounter/player classification prefix.</summary>
internal static class CardTypeCaption
{
    internal static string From(string kind) => kind switch
    {
        "ENCOUNTER VILLAIN" => "VILLAIN",
        "ENCOUNTER SIDE SCHEME" or "PLAYER SIDE SCHEME" => "SIDE SCHEME",
        _ => kind,
    };
}
