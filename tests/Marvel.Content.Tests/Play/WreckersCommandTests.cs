using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Canonical 07007 in explicitly arranged physical encounter groups.</summary>
public sealed class WreckersCommandTests
{
    [Theory]
    [InlineData("scheme", new long[] { 3, 1, 1, 1 })]
    [InlineData("attack", new long[] { 0, 0, 0, 0 })]
    [InlineData("foreign", new long[] { 0, 3, 0, 0 })]
    [InlineData("confused", new long[] { 0, 0, 0, 0 })]
    [Rule("rr:confuse-confused.7")]
    public void OnlyCompletedWreckerSchemesPlaceThreatOnOtherVillainsSchemes(string mode, long[] expected)
    {
        // 07007: "After Wrecker schemes, place 1 threat on each other villain's side scheme."
        // A confused character is "not considered to have schemed" (rr:confuse-confused.7).
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07007", 0);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Assert.Equal(new[] { groups[0].Villain.ObjectId }, runner.AttachmentTargets(world, source));
        Reveal.Resolve(world, world.Facts, source, 0, []);
        Assert.Equal(groups[0].Villain.ObjectId, source.Area.Host);
        Assert.Equal(3, StateFields.Modified(world, groups[0].Villain, "scheme", world.Facts, 1));
        var unpaired = world.CreateCard("07019", world.AreaOf(DeckType.SideSchemesArea));
        if (mode == "confused") Statuses.Give(world, groups[0].Villain, Statuses.Confused);
        AgendaOperation operation = mode == "attack" ? new EnemyAttackOperation() : new EnemySchemeOperation();
        world.Agenda.Add(new(operation, 1, 1, Subject: groups[mode == "foreign" ? 1 : 0].Villain.ObjectId, Seat: 0));
        WreckingSchemeFixture.Finish(world, runner);
        Assert.Equal(expected, groups.Select(group => group.Scheme.Tokens.GetValueOrDefault("k_threat")));
        Assert.Equal(0, unpaired.Tokens.GetValueOrDefault("k_threat"));
        Assert.Same(groups[1], world.EncounterTopology!.Active);
    }

    [Fact]
    public void DefeatedVillainsPairedSchemeReceivesNoCommandThreat()
    {
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07007", 0);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Reveal.Resolve(world, world.Facts, source, 0, []);
        World.MoveToTop(groups[2].Villain, world.AreaOf(DeckType.RemovedArea));
        world.Agenda.Add(new(new EnemySchemeOperation(), 1, 1, Subject: groups[0].Villain.ObjectId, Seat: 0));
        WreckingSchemeFixture.Finish(world, runner);
        Assert.Equal(new long[] { 3, 1, 0, 1 },
            groups.Select(group => group.Scheme.Tokens.GetValueOrDefault("k_threat")));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [Rule("rr:forced.5")]
    [Rule("rr:forced.6")]
    public void OtherSchemesReachThresholdTogetherAndFirstPlayerOrdersTheirResponses(int first)
    {
        // "the first player determines the order"; each forced ability must "resolve
        // as completely as possible before the next". Both Command recipients reach 10 together.
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07007", 0, players: 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.FirstPlayer = 1;
        Reveal.Resolve(world, world.Facts, source, 0, []);
        groups[1].Scheme.PlaceTokens("k_threat", 9);
        groups[2].Scheme.PlaceTokens("k_threat", 9);
        var events = new List<GameEvent>();
        world.Agenda.Add(new(new EnemySchemeOperation(), 1, 1, Subject: groups[0].Villain.ObjectId, Seat: 0));
        var choice = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(choice);
        Assert.False(choice.Cancellable);
        Assert.Equal(1, choice.Player);
        Assert.Equal(new[] { groups[1].Scheme.ObjectId, groups[2].Scheme.ObjectId }.Order(),
            choice.Affordances.Select(offer => offer.AnchorId).Order());
        Assert.Equal(new long[] { 3, 10, 10, 1 },
            groups.Select(group => group.Scheme.Tokens.GetValueOrDefault("k_threat")));
        Assert.All(world.Seats, seat => Assert.False(Statuses.Has(world, seat.IdentityCard, Statuses.Stunned)));
        Sequence.Answer(world, world.Facts, runner, choice,
            Decision.Take(choice.Affordances.Single(offer => offer.AnchorId == groups[first].Scheme.ObjectId).Id), events);
        WreckingSchemeFixture.Finish(world, runner, events);
        Assert.Equal(new long[] { 3, 3, 3, 1 },
            groups.Select(group => group.Scheme.Tokens.GetValueOrDefault("k_threat")));
        var resets = events.OfType<FieldSet>().Where(change => change.Field == "k_threat" && change.To == 3)
            .Select(change => change.Card).Where(id => id != groups[0].Scheme.ObjectId);
        Assert.Equal(new[] { groups[first].Scheme.ObjectId, groups[3 - first].Scheme.ObjectId }, resets);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void EitherHeroPaysTwoPhysicalResourcesAndDiscardsToTheOwningEncounterPile(int player)
    {
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07007", 0, players: 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Reveal.Resolve(world, world.Facts, source, player, []);
        world.CreateCard("01088", world.Seats[player].Deck);
        var payment = world.CreateCard("01090", world.Seats[player].Hand);
        var action = Assert.Single(runner.Actions(world, player), item => item.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], []));
        WreckingSchemeFixture.Finish(world, runner);
        Assert.Same(groups[0].Discard, source.Area);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.True(world.Seats[player].IdentityCard.Ready);
        Assert.Equal(2, StateFields.Modified(world, groups[0].Villain, "scheme", world.Facts, 1));
        Assert.DoesNotContain(runner.Actions(world, player), item => item.Card == source.ObjectId);
    }

    [Theory]
    [InlineData("alter-ego")]
    [InlineData("empty")]
    [InlineData("mental")]
    [InlineData("onePhysical")]
    public void HeroFormAndTwoPhysicalResourcesAreRequired(string condition)
    {
        var (world, _, source) = WreckingEncounterCardFixture.Create("07007", 0,
            condition == "alter-ego" ? "01001b" : "01001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Reveal.Resolve(world, world.Facts, source, 0, []);
        if (condition != "empty") world.CreateCard(condition switch
        {
            "mental" => "01089",
            "onePhysical" => "01005",
            _ => "01090"
        }, world.Seats[0].Hand);
        string before = world.Digest().Canonical();
        Assert.DoesNotContain(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
    }
}
