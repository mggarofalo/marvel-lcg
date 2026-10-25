using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingWaitingTests : CoreCardTestBase
{
    private static readonly string[] Reprints = ["07014", "07029", "07041"];

    [Rule("rr:move.1")]
    [Theory]
    [InlineData("07014")]
    [InlineData("07029")]
    [InlineData("07041")]
    public void TiedCurrentHolderCannotCompeteWithTheOnlyDifferentDestination(string face)
    {
        // rr:move.1: "it cannot move to its same (current) placement",
        // even when tied for the printed least-threat condition.
        var (world, groups, source) = Setup(face);
        int original = Array.IndexOf(Reprints, face);
        for (int index = 0; index < groups.Length; index++)
            groups[index].Scheme.PlaceTokens("k_threat", index == original || index == 3 ? 0 : 5);
        var runner = AuthoredCards.Runner();

        runner.ExecuteBoost(world, source, 0);
        Finish(world, runner, []);

        Assert.Same(groups[3], world.EncounterTopology!.Active);
        Assert.Equal(1, groups[3].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, groups[original].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Single(groups[3].Deck.Cards);
        Assert.Equal(2, groups[original].Deck.Cards.Count);
    }

    [Theory]
    [InlineData("07014", true)]
    [InlineData("07029", true)]
    [InlineData("07041", true)]
    [InlineData("07014", false)]
    [InlineData("07029", false)]
    [InlineData("07041", false)]
    public void BoostFinishesOriginalAttackBeforeResultingActiveVillainSchemes(string face, bool move)
    {
        // Printed Boost: "Move the active counter to the villain with the least
        // threat on his side scheme. That villain schemes."
        // ruling:d502526070e1f010: "That villain" is the active-counter holder,
        // regardless of whether the counter moved.
        // ruling:23fba0d298997c93: finish the activation and its responses before
        // resolving a new activation generated during it.
        var (world, groups, source) = Setup(face);
        int original = Array.IndexOf(Reprints, face);
        int destination = move ? 3 : original;
        for (int index = 0; index < groups.Length; index++)
            groups[index].Scheme.PlaceTokens("k_threat", index == destination ? 0 : 5);
        World.MoveToTop(source, groups[original].Deck);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: groups[original].Villain.ObjectId, Seat: 0));
        List<GameEvent> events = [];

        Finish(world, runner, events);

        Assert.Same(groups[destination], world.EncounterTopology!.Active);
        Assert.Equal(original == 0 ? 4 : 2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(move ? 1 : original == 1 ? 3 : 2,
            groups[destination].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Same(groups[original].Discard, source.Area);
        AssertActivationOrder(events, groups[original].Villain, groups[destination].Scheme,
            world.Seats[0].IdentityCard, original == 1);
    }

    private static void AssertActivationOrder(List<GameEvent> events, Card attacker, Card destination,
        Card identity, bool thunderball)
    {
        var completed = events.FindIndex(item => item is AttackBoundary { Started: false } boundary
            && boundary.Source == attacker.ObjectId);
        var scheme = events.FindIndex(item => item is FieldSet field
            && field.Card == destination.ObjectId && field.Field == "k_threat");
        Assert.True(completed >= 0);
        Assert.True(scheme > completed);
        // Thunderball's forced response deals its additional damage before the scheme.
        if (thunderball)
        {
            var health = events.Select((item, index) => (item, index)).Where(pair =>
                pair.item is FieldSet field && field.Card == identity.ObjectId
                    && field.Field == "health").ToArray();
            Assert.Equal(2, health.Length);
            Assert.True(health[^1].index < scheme);
        }
    }

    [Theory]
    [InlineData("07014", 0)]
    [InlineData("07029", 2)]
    [InlineData("07041", 5)]
    public void RevealHealsUpToThreeAndGivesToughToTheActiveVillain(string face, int damage)
    {
        // "The active villain heals 3 hit points. Give that villain a tough status card."
        var (world, groups, source) = Setup(face);
        var runner = AuthoredCards.Runner();
        world.EncounterTopology!.SelectActive(groups[3].Villain, "setup", []);
        groups[3].Villain.TakeDamage(damage);
        groups[0].Villain.TakeDamage(4);

        runner.ExecuteReveal(world, source, 0);
        Finish(world, runner, []);

        Assert.Equal(Math.Max(0, damage - 3), groups[3].Villain.Damage);
        Assert.True(Statuses.Has(world, groups[3].Villain, Statuses.Tough));
        Assert.Equal(4, groups[0].Villain.Damage);
        Assert.False(Statuses.Has(world, groups[0].Villain, Statuses.Tough));
    }

    private static (World World, EncounterDeckGroup[] Groups, Card Source) Setup(string face)
    {
        var world = Hero("01001a");
        world.CreateCard("01088", world.Seats[0].Deck);
        world.CreateCard("07001b", world.AreaOf(DeckType.MainSchemesArea));
        var source = world.CreateCard(face, world.AreaOf(DeckType.RevealingArea));
        string[] villains = ["07002", "07017", "07032", "07046"];
        string[] schemes = ["07004", "07019", "07034", "07048"];
        var groups = Enumerable.Range(0, 4).Select(index =>
        {
            var deck = world.CreateArea(DeckType.EncounterDeck);
            Card[] boosts = [world.CreateCard("07037", deck), world.CreateCard("07037", deck)];
            return new EncounterDeckGroup(
                world.CreateCard(villains[index], world.AreaOf(DeckType.VillainArea)),
                world.CreateCard(schemes[index], world.AreaOf(DeckType.SideSchemesArea)),
                deck, world.CreateArea(DeckType.EncounterDiscardPile),
                index == Array.IndexOf(Reprints, face) ? [.. boosts, source] : boosts);
        }).ToArray();
        world.BindEncounterTopology(new EncounterTopology(world, groups,
            groups[Array.IndexOf(Reprints, face)].Villain));
        return (world, groups, source);
    }

    private static void Finish(World world, AbilityRunner runner, List<GameEvent> events)
    {
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) return;
            Assert.True(step < 15);
            Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        }
    }
}
