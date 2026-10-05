using Marvel.Core.Digest;

namespace Marvel.Rules.State;

/// <summary>Maps one physical card and its active copy to the internal digest contract.</summary>
internal static class CardDigestRecord
{
    internal static CardRecord For(World world, Card card, string zone, int index) => new(
        card.ObjectId, card.FaceId, zone, card.Owner, index, card.Area.Host, card.FaceUp,
        StateFields.For(card, world.Facts, world.Players, DeckTypes.IsInPlay(card.Area.Type),
            card.HasRegisteredTokens,
            card.Owner == world.FirstPlayer && card.Area.Type == DeckType.HeroArea, world))
    {
        Profile = card.InstanceState.Profile is { } profile
            ? new EffectiveProfileRecord(profile.Id, profile.Title, profile.Kind.ToString(),
                profile.Traits, profile.BaseValues)
            : null,
    };
}
