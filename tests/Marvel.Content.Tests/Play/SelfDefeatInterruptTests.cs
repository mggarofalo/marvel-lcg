using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic self-defeat abilities isolate the engine's interrupt contract.</summary>
public sealed class SelfDefeatInterruptTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    [Rule("rr:damage.step.7")]
    [Rule("rr:damage.step.8")]
    [Rule("rr:forced.1")]
    public void OwnForcedInterruptResolvesOnceBeforeDiscard()
    {
        // "Forced Interrupt" abilities "must be resolved when their triggering conditions are met."
        // Damage step 7 precedes step 8: "Discarding of a defeated character."
        var (world, ally, scheme) = Board(Ability("ForcedInterrupt", 1));
        var events = new List<GameEvent>();
        DamagePlacement.Resolve(world, Cards, new(ally, ally, 3, "test", "test"), events);
        Sequence.Finish(world, Cards, world.Abilities, events);

        Assert.Equal(1, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        int threat = events.FindIndex(item => item.Verb == "Place_Threat");
        int discard = events.FindIndex(item => item is CardsMoved moved
            && moved.Cards.Any(landing => landing.Card == ally.ObjectId));
        Assert.True(threat >= 0 && discard > threat);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:interrupt.3")]
    [Rule("rr:interrupt.5")]
    [Rule("rr:triggering-condition.1")]
    public void OwnOptionalInterruptCanBeAcceptedOrDeclined(bool accept)
    {
        // "An interrupt ability is resolved ... before that triggering condition resolves."
        // "Each Interrupt and Response ability can only be triggered once per occurrence."
        var (world, ally, scheme) = Board(Ability("Interrupt", 1));
        DamagePlacement.Resolve(world, Cards, new(ally, ally, 3, "test", "test"), []);
        var prompt = Sequence.Work(world, Cards, world.Abilities, [])!;
        Assert.True(prompt.Cancellable);
        Assert.Equal(TimingPriority.Interrupt, prompt.When);
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal(ally.ObjectId, offer.AnchorId);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("k_threat"));
        Sequence.Answer(world, Cards, world.Abilities, prompt,
            accept ? Decision.Take(offer.Id) : Decision.Decline, []);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, []));
        Assert.Equal(accept ? 1 : 0, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
    }

    [Fact]
    [Rule("rr:forced.4")]
    [Rule("rr:when-defeated-abilities.1")]
    [Rule("rr:triggering-condition.1")]
    public void OwnForcedAndWhenDefeatedShareTierBeforeOwnOptionalInterrupt()
    {
        // "Forced interrupts take priority and initiate before non-forced interrupts."
        // "When Defeated" is equivalent to "Forced Interrupt: When this card is defeated..."
        var (world, ally, scheme) = Board(string.Join(",",
            Ability("ForcedInterrupt", 1), Ability("WhenDefeated", 2), Ability("Interrupt", 1)));
        DamagePlacement.Resolve(world, Cards, new(ally, ally, 3, "test", "test"), []);
        var ordering = Sequence.Work(world, Cards, world.Abilities, [])!;
        Assert.False(ordering.Cancellable);
        Assert.Equal(TimingPriority.ForcedInterrupt, ordering.When);
        Assert.Equal(2, ordering.Affordances.Count);
        Sequence.Answer(world, Cards, world.Abilities, ordering,
            Decision.Take(ordering.Affordances[0].Id), []);
        var optional = Sequence.Work(world, Cards, world.Abilities, [])!;
        Assert.True(optional.Cancellable);
        Assert.Equal(3, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Sequence.Answer(world, Cards, world.Abilities, optional,
            Decision.Take(Assert.Single(optional.Affordances).Id), []);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, []));
        Assert.Equal(4, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
    }

    private static string Ability(string timing, int amount) => """
        {"trigger":{"event":"WhenCardDefeated","timing":"TIMING","subject":"this"},
         "effect":{"placeThreat":{"scheme":{"query":"mainScheme"},"amount":AMOUNT}}}
        """.Replace("TIMING", timing, StringComparison.Ordinal)
            .Replace("AMOUNT", amount.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);

    private static (World World, Card Ally, Card Scheme) Board(string abilities)
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("01001a");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(
            """{"cards":[{"card":"01083","abilities":[ABILITIES]}]}"""
                .Replace("ABILITIES", abilities, StringComparison.Ordinal)));
        world.CreateCard("01088", seat.Deck);
        var ally = world.CreateCard("01083",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        return (world, ally, scheme);
    }
}
