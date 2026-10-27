using System.Collections.Immutable;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Rules.Setup;

/// <summary>Validates and binds the typed Invocation setup before the game begins.</summary>
internal static class InvocationSetup
{
    private static readonly ImmutableArray<string> IdentityFaces = ["09001a", "09001b"];
    internal static void Validate(IReadOnlyList<CardBlueprint> blueprints, int players)
    {
        foreach (var card in blueprints)
        {
            bool invocation = card.Spec.Split(',').Any(InvocationDeckRecipe.IsInvocation);
            if (invocation != (card.Slot == SetupSlot.InvocationDeck))
                throw new ArgumentException("Invocation cards require their separate typed setup slot", nameof(blueprints));
            if (card.Slot == SetupSlot.InvocationDeck && (card.Seat < 0 || card.Seat >= players))
                throw new ArgumentException("Invocation cards require a selected owner", nameof(blueprints));
        }
        for (int seat = 0; seat < players; seat++) ValidateSeat(blueprints, seat);
    }

    private static void ValidateSeat(IReadOnlyList<CardBlueprint> blueprints, int seat)
    {
        var identities = blueprints.Where(card => card.Slot == SetupSlot.Identity && card.Seat == seat).ToArray();
        bool strange = identities.Any(card => card.Spec.Split(',').Any(IdentityFaces.Contains));
        var cards = blueprints.Where(card => card.Slot == SetupSlot.InvocationDeck && card.Seat == seat).ToArray();
        if (!strange && cards.Length == 0) return;
        // The insert places this deck "next to your identity card"; physical ownership is explicit.
        if (identities.Length != 1 || !identities[0].Spec.Split(',').Order(StringComparer.Ordinal)
                .SequenceEqual(IdentityFaces))
            throw new ArgumentException("an Invocation deck requires its owner's Doctor Strange identity", nameof(blueprints));
        _ = new InvocationDeckRecipe(cards.Select(card => card.Spec).ToArray());
    }

    internal static void Bind(World world, IReadOnlyList<CardBlueprint> blueprints, List<GameEvent> events)
    {
        for (int seat = 0; seat < world.Seats.Count; seat++)
        {
            var cards = blueprints.Select((blueprint, index) => (blueprint, index))
                .Where(item => item.blueprint.Slot == SetupSlot.InvocationDeck && item.blueprint.Seat == seat)
                .Select(item => world.Cards[item.index]).ToArray();
            if (cards.Length > 0) InvocationDeck.Initialize(world, seat, cards, "Setup", events);
        }
    }
}
