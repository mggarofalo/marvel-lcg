namespace Marvel.Godot;

/// <summary>The forced-ability receipt's words beside the engine's summary.</summary>
/// <remarks>The words are our choices.</remarks>
internal static class ForcedAbilityWording
{
    /// <summary>A receipt whose resolution has not finished.</summary>
    internal static readonly Wording InProgress = new("", "In progress", "Resolution is still in progress.");

    /// <summary>The decision that follows the receipt, as one short part.</summary>
    internal static string Next(string heading) => $"Next: {heading}";
}
