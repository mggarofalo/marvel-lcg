namespace Marvel.View;

/// <summary>Authorized forced-ability receipt, acknowledged locally without another engine answer.</summary>
public sealed record ForcedAbilityNotification(int SourceId, string Title, string Summary,
    int Player, int Occurrence, bool Completed)
{
    /// <summary>The exact printed face granted by the authorized ability occurrence, without live state.</summary>
    public BoardCardPresentation? SourceCard { get; init; }
}
