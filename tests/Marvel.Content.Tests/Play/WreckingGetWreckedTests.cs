using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingGetWreckedTests : CoreCardTestBase
{
    private static readonly string[] Reprints = ["07013", "07028", "07040", "07057"];

    [Rule("rr:first-player.1")]
    [Theory]
    [InlineData("07013", true)]
    [InlineData("07028", true)]
    [InlineData("07040", true)]
    [InlineData("07057", true)]
    [InlineData("07013", false)]
    [InlineData("07028", false)]
    [InlineData("07040", false)]
    [InlineData("07057", false)]
    public void FirstPlayerChoosesTiedVillainWhichActivatesAgainstTheRevealingPlayer(string face, bool hero)
    {
        // 07013/07028/07040/07057: "The villain whose side scheme has the least threat attacks you."
        // In alter-ego: "The villain whose side scheme has the most threat schemes."
        // rr:first-player.1: "the first player selects among the eligible options."
        Assert.Equal(Resolve(face, hero, probe: false), Resolve(face, hero, probe: true));
    }

    [Rule("rr:first-player.1")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UniqueRankedVillainRequiresNoFirstPlayerChoice(bool hero)
    {
        // rr:first-player.1 assigns a selection when "there are multiple eligible targets".
        // 07040 attacks with the least-threat villain or schemes with the most-threat villain.
        var (world, runner, source, groups, _, _) = Setup("07040", hero);
        groups[hero ? 3 : 2].Scheme.PlaceTokens("k_threat", 1);
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: source.ObjectId, Seat: 1));
        // Only optional attack responses/defense may ask; an Element target choice fails this driver.
        Finish(world, runner, []);

        Assert.Equal(hero ? 2 : 0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(hero ? 1 : 11, groups[2].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Same(groups[0], world.EncounterTopology!.Active);
        Assert.Equal(groups[2].Discard, source.Area);
    }

    private static (string Digest, long Words) Resolve(string face, bool hero, bool probe)
    {
        var (world, runner, source, groups, threat, main) = Setup(face, hero);
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: source.ObjectId, Seat: 1));
        var events = new List<GameEvent>();
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(0, prompt.Player);
        Assert.False(prompt.Cancellable);
        Assert.Equal(new[] { groups[2].Villain.ObjectId, groups[3].Villain.ObjectId },
            prompt.Affordances.Select(option => option.Id));
        Assert.Contains(source.ObjectId, prompt.ContextCardIds);
        if (probe) Probe(world, source, runner, prompt);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(groups[2].Villain.ObjectId), events);
        Finish(world, runner, events);

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(hero ? 2 : 0, world.Seats[1].IdentityCard.Damage);
        for (int index = 0; index < groups.Length; index++)
            Assert.Equal(threat[index] + (!hero && index == 2 ? 2 : 0),
                groups[index].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Same(groups[0], world.EncounterTopology!.Active);
        // pack:mc03:new-rules: "it is dealt from the active villain's deck."
        Assert.Single(groups[0].Deck.Cards);
        Assert.Equal(2, groups[2].Deck.Cards.Count);
        Assert.Equal(groups[Array.IndexOf(Reprints, face)].Discard, source.Area);
        if (hero)
            Assert.Contains(events.OfType<AttackBoundary>(),
                item => !item.Started && item.Source == groups[2].Villain.ObjectId);
        return (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);
    }

    private static (World World, AbilityRunner Runner, Card Source, EncounterDeckGroup[] Groups,
        int[] Threat, Card Main) Setup(string face, bool hero)
    {
        var world = Hero("01001a", players: 2);
        if (!hero)
        {
            World.MoveToTop(world.Seats[1].IdentityCard, world.AreaOf(DeckType.RemovedArea));
            world.Seats[1].IdentityCard = world.CreateCard("01001b", world.Seats[1].Hero);
        }
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        foreach (var seat in world.Seats)
            world.CreateCard("01088", seat.Deck);
        var main = world.CreateCard("07001b", world.AreaOf(DeckType.MainSchemesArea));
        var source = world.CreateCard(face, world.AreaOf(DeckType.RevealingArea));
        string[] faces = ["07002", "07017", "07032", "07046"];
        string[] schemes = ["07004", "07019", "07034", "07048"];
        var groups = Enumerable.Range(0, 4).Select(index =>
            Group(world, faces[index], schemes[index], Reprints[index] == face ? source : null)).ToArray();
        world.BindEncounterTopology(new EncounterTopology(world, groups, groups[0].Villain));
        int[] threat = hero ? [8, 9, 1, 1] : [0, 1, 8, 8];
        for (int index = 0; index < groups.Length; index++)
            groups[index].Scheme.PlaceTokens("k_threat", threat[index]);
        return (world, runner, source, groups, threat, main);
    }

    private static void Probe(World world, Card source, AbilityRunner runner, Prompt expected)
    {
        var continuation = AbilityContinuationFixtures.Pending(world, source);
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed,
            world.Agenda.Outstanding.ToArray());
        var actual = runner.Choosing(world, continuation)!;
        Assert.Equal(expected.Player, actual.Player);
        Assert.Equal(expected.Affordances.Select(option => option.Id), actual.Affordances.Select(option => option.Id));
        Assert.Equal(before.Item1, world.Digest().Canonical());
        Assert.Equal(before.Item2, world.Random.Generator.WordsConsumed);
        Assert.Equal(before.Item3, world.Agenda.Outstanding);
    }

    private static void Finish(World world, AbilityRunner runner, List<GameEvent> events)
    {
        for (int steps = 0; ; steps++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) return;
            Assert.True(steps < 12);
            Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        }
    }

    private static EncounterDeckGroup Group(World world, string face, string scheme, Card? source)
    {
        var deck = world.CreateArea(DeckType.EncounterDeck);
        var boosts = new[] { world.CreateCard("07037", deck), world.CreateCard("07037", deck) };
        return new(world.CreateCard(face, world.AreaOf(DeckType.VillainArea)),
            world.CreateCard(scheme, world.AreaOf(DeckType.SideSchemesArea)), deck,
            world.CreateArea(DeckType.EncounterDiscardPile), source is null ? boosts : [.. boosts, source]);
    }
}
