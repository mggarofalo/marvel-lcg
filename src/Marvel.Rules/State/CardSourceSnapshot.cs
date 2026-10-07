namespace Marvel.Rules.State;

/// <summary>The source identity that actually created an effect, before authorization.</summary>
public sealed record CardSourceSnapshot(
    int CardId,
    int Incarnation,
    string FaceId,
    string Title,
    CardSourceExposure Exposure)
{
    internal static CardSourceSnapshot Capture(Card card, ICardFacts facts) => new(
        card.ObjectId, card.Incarnation, EffectiveCards.FaceId(card), EffectiveCards.Title(card, facts),
        new(card.Area.Type, card.Area.PlayArea.Player, card.FaceUp, EffectiveCards.HasProfile(card)));
}
