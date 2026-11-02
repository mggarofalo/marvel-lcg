using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Cards an ability names by what its own resolution has done to them.</summary>
internal static class AbilityResolutionMemoryCards
{
    internal static Card? Named(AbilityCardBinding name, AbilityQueryContext cast) => name switch
    {
        // Printed 16101 Combat Ready: "Reveal that card, then resolve its 'Special'
        // ability." The card the invocation revealed last, once its reveal resolved.
        // Equivalent mutant: every ability that names it reveals one card, so first and last agree.
        AbilityCardBinding.Revealed => (cast.RevealedThisResolution ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' names the revealed card outside an ability invocation")) is { Count: > 0 } revealed
            ? cast.World.Cards[revealed[^1]] : null,
        // Printed 16094 Cutthroat Ambition: "attach this card to Nebula and resolve its
        // 'Special' ability." Attaching makes the card a new object that `this` no longer
        // names; the same physical card is still "this card".
        AbilityCardBinding.AttachedThis => cast.World.Cards[cast.Source.ObjectId],
        _ => throw new InvalidOperationException("Unknown compiled card binding"),
    };
}
