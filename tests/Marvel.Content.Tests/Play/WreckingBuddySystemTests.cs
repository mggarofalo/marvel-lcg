using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingBuddySystemTests : CoreCardTestBase
{
    private static readonly string[] Reprints = ["07010", "07025", "07039", "07054"];

    [Theory]
    [InlineData("07010", false)]
    [InlineData("07025", false)]
    [InlineData("07039", false)]
    [InlineData("07054", false)]
    [InlineData("07010", true)]
    [InlineData("07025", true)]
    [InlineData("07039", true)]
    [InlineData("07054", true)]
    public void RevealsTheSelectedDeckForTheResolvingPlayer(string face, bool onlyVillain)
    {
        // "Reveal the top card of his deck (top 2 cards instead if he is the only villain in play)."
        // Energy Projectiles: "Deal 1 damage to each friendly character you control."
        var (world, groups, source) = Setup(face);
        int selected = onlyVillain ? Array.IndexOf(Reprints, face) : 1;
        if (onlyVillain)
        {
            world.EncounterTopology!.SelectActive(groups[selected].Villain, "setup", []);
            foreach (var group in groups.Where(group => group != groups[selected]))
                World.MoveToTop(group.Villain, world.AreaOf(DeckType.RemovedArea));
        }
        for (int index = 0; index < groups.Length; index++)
            groups[index].Scheme.PlaceTokens("k_threat", index == selected ? 0 : 4);
        var active = world.EncounterTopology!.Active;
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: source.ObjectId, Seat: 1));
        List<GameEvent> events = [];

        Finish(world, runner, events);

        int count = onlyVillain ? 2 : 1;
        Assert.Equal(count, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Same(active, world.EncounterTopology.Active);
        Assert.Equal(3 - count, groups[selected].Deck.Cards.Count);
        Assert.Equal(count, groups[selected].Discard.Cards.Count(card => card.FaceId == "07027"));
        Assert.Same(groups[Array.IndexOf(Reprints, face)].Discard, source.Area);
        Assert.Equal(count, events.OfType<FieldSet>().Count(item =>
            item.Card == world.Seats[1].IdentityCard.ObjectId && item.Field == "health"));
    }

    [Rule("rr:encounter-deck.1")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResetPrecedesChildResolutionAndParentDiscardFollowsEveryChild(bool onlyVillain)
    {
        // "the encounter discard pile is immediately shuffled to create a new encounter deck".
        // ruling:23fba0d298997c93 confirms Buddy System remains unresolved until its children finish.
        var (world, groups, source) = Setup("07025");
        var selected = groups[1];
        if (onlyVillain)
        {
            world.EncounterTopology!.SelectActive(selected.Villain, "setup", []);
            foreach (var group in groups.Where(group => group != selected))
                World.MoveToTop(group.Villain, world.AreaOf(DeckType.RemovedArea));
        }
        foreach (var group in groups.Where(group => group != selected)) group.Scheme.PlaceTokens("k_threat", 4);
        foreach (var card in selected.Deck.Cards.Take(2).ToArray()) World.MoveToTop(card, selected.Discard);
        var child = Assert.Single(selected.Deck.Cards);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: source.ObjectId, Seat: 1));
        List<GameEvent> events = [];

        Finish(world, runner, events);

        int resetIndex = events.FindIndex(item => item is CardsMoved { Verb: "Reset" });
        int firstDamage = events.FindIndex(item => item is FieldSet { Field: "health" });
        int lastDamage = events.FindLastIndex(item => item is FieldSet { Field: "health" });
        int parentDiscard = events.FindIndex(item => item is CardsMoved moved
            && moved.To == Places.Reference(selected.Discard)
            && moved.Cards.Any(landing => landing.Card == source.ObjectId));
        Assert.True(resetIndex >= 0 && resetIndex < firstDamage);
        Assert.True(parentDiscard > lastDamage);
        var reset = Assert.IsType<CardsMoved>(events[resetIndex]);
        Assert.Equal(Places.Reference(selected.Deck), reset.To);
        Assert.Equal(2, reset.Cards.Count);
        Assert.DoesNotContain(reset.Cards, landing => landing.Card == child.ObjectId || landing.Card == source.ObjectId);
        Assert.Equal(onlyVillain ? 2 : 1, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(onlyVillain ? 1 : 2, selected.Deck.Cards.Count);
    }

    [Fact]
    public void SecondRevealWaitsForTheFirstCardsAttackChoiceAndRestoresItsPlayer()
    {
        // Buddy System's two reveals resolve sequentially. Get Wrecked! attacks
        // the resolving player; Energy Projectiles then damages that same player's characters.
        var (world, groups, source) = Setup("07025", ["07037", "07037", "07027", "07037", "07028"]);
        var selected = groups[1];
        world.EncounterTopology!.SelectActive(selected.Villain, "setup", []);
        foreach (var group in groups.Where(group => group != selected))
            World.MoveToTop(group.Villain, world.AreaOf(DeckType.RemovedArea));
        var first = selected.Deck.Cards[^1];
        var second = selected.Deck.Cards[2];
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: source.ObjectId, Seat: 1));
        List<GameEvent> events = [];

        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));

        Assert.Equal(1, prompt.Player);
        Assert.True(prompt.Cancellable);
        Assert.Equal(DeckType.RevealingArea, source.Area.Type);
        Assert.Equal(DeckType.RevealingArea, first.Area.Type);
        Assert.Same(selected.Deck, second.Area);
        Assert.Single(events.OfType<CardsMoved>(), item => item.Verb == "Reveal"
            && item.From == Places.Reference(selected.Deck));
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        Finish(world, runner, events);

        Assert.Equal(3, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        var discards = events.OfType<CardsMoved>().Where(item => item.To == Places.Reference(selected.Discard))
            .SelectMany(item => item.Cards.Select(landing => landing.Card)).ToList();
        Assert.True(discards.IndexOf(first.ObjectId) < discards.IndexOf(second.ObjectId));
        Assert.True(discards.IndexOf(second.ObjectId) < discards.IndexOf(source.ObjectId));
        Assert.Same(selected.Discard, first.Area);
        Assert.Same(selected.Discard, second.Area);
        Assert.Same(selected.Discard, source.Area);
    }

    [Fact]
    public void TiedChoiceIsPublicPureAndBelongsToTheFirstPlayer()
    {
        Assert.Equal(ResolveTie(false), ResolveTie(true));
    }

    private static (string Digest, long Words) ResolveTie(bool probe)
    {
        var (world, groups, source) = Setup("07039");
        groups[0].Scheme.PlaceTokens("k_threat", 4);
        groups[3].Scheme.PlaceTokens("k_threat", 4);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: source.ObjectId, Seat: 1));
        List<GameEvent> events = [];
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(0, prompt.Player);
        Assert.Equal(new[] { groups[1].Villain.ObjectId, groups[2].Villain.ObjectId },
            prompt.Affordances.Select(option => option.Id));
        if (probe)
        {
            var continuation = AbilityContinuationFixtures.Pending(world, source);
            var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed,
                world.Agenda.Outstanding.ToArray());
            var actual = runner.Choosing(world, continuation)!;
            Assert.Equal(prompt.Affordances.Select(option => option.Id), actual.Affordances.Select(option => option.Id));
            Assert.Equal(before.Item1, world.Digest().Canonical());
            Assert.Equal(before.Item2, world.Random.Generator.WordsConsumed);
            Assert.Equal(before.Item3, world.Agenda.Outstanding);
        }
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(groups[1].Villain.ObjectId), events);
        Finish(world, runner, events);
        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Same(groups[0], world.EncounterTopology!.Active);
        return (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);
    }

    [Rule("rr:surge.1")]
    [Fact]
    public void SurgeFromSelectedDeckDealsFromTheActiveDeckToTheOriginalPlayer()
    {
        // "When Revealed: Deal yourself 1 facedown encounter card."
        // pack:mc03:new-rules.the-active-villain: dealt encounter cards use the active villain's deck.
        var (world, groups, source) = Setup("07039", ["07037", "07037", "01121"]);
        for (int index = 0; index < groups.Length; index++)
            groups[index].Scheme.PlaceTokens("k_threat", index == 1 ? 0 : 4);
        var activeTop = groups[0].Deck.Cards[^1];
        var selectedTop = groups[1].Deck.Cards[^1];
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: source.ObjectId, Seat: 1));

        Finish(world, runner, []);

        Assert.Same(groups[0], world.EncounterTopology!.Active);
        Assert.Equal(DeckType.DealtEncounterCardsDeck, activeTop.Area.Type);
        Assert.Equal(PlayArea.Of(1), activeTop.Area.PlayArea);
        Assert.False(activeTop.FaceUp);
        Assert.Equal(DeckType.EngagedEnemiesArea, selectedTop.Area.Type);
        Assert.Equal(PlayArea.Of(1), selectedTop.Area.PlayArea);
        Assert.Equal(2, groups[0].Deck.Cards.Count);
        Assert.Equal(2, groups[1].Deck.Cards.Count);
    }

    [Theory]
    [InlineData("07010")]
    [InlineData("07025")]
    [InlineData("07039")]
    [InlineData("07054")]
    public void BoostMovesCounterWithoutRevealingADeck(string face)
    {
        var (world, groups, source) = Setup(face);
        for (int index = 0; index < groups.Length; index++)
            groups[index].Scheme.PlaceTokens("k_threat", index == 3 ? 0 : 4);
        var runner = AuthoredCards.Runner();
        runner.ExecuteBoost(world, source, 1);
        Finish(world, runner, []);
        Assert.Same(groups[3], world.EncounterTopology!.Active);
        Assert.All(groups, group => Assert.Equal(3, group.Deck.Cards.Count));
        Assert.All(world.Seats, seat => Assert.Equal(0, seat.IdentityCard.Damage));
    }

    private static (World World, EncounterDeckGroup[] Groups, Card Source) Setup(string face, string[]? deckFaces = null)
    {
        var world = Hero("01001a", players: 2);
        foreach (var seat in world.Seats) world.CreateCard("01088", seat.Deck);
        world.CreateCard("07001b", world.AreaOf(DeckType.MainSchemesArea));
        var source = world.CreateCard(face, world.AreaOf(DeckType.RevealingArea));
        string[] villains = ["07002", "07017", "07032", "07046"];
        string[] schemes = ["07004", "07019", "07034", "07048"];
        var groups = Enumerable.Range(0, 4).Select(index =>
        {
            var deck = world.CreateArea(DeckType.EncounterDeck);
            Card[] cards = (deckFaces ?? ["07027", "07027", "07027"])
                .Select(child => world.CreateCard(child, deck)).ToArray();
            return new EncounterDeckGroup(
                world.CreateCard(villains[index], world.AreaOf(DeckType.VillainArea)),
                world.CreateCard(schemes[index], world.AreaOf(DeckType.SideSchemesArea)),
                deck, world.CreateArea(DeckType.EncounterDiscardPile),
                index == Array.IndexOf(Reprints, face) ? [.. cards, source] : cards);
        }).ToArray();
        world.BindEncounterTopology(new EncounterTopology(world, groups, groups[0].Villain));
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
