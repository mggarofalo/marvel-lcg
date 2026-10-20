using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SchemeDefeatResponsibilityTests : CoreCardTestBase
{
    [Theory]
    [InlineData(0, true, false)]
    [InlineData(1, true, false)]
    [InlineData(0, false, false)]
    [InlineData(1, false, false)]
    [InlineData(1, true, true)]
    [InlineData(1, false, true)]
    [Rule("rr:ownership-and-control.5")]
    public void SurveillanceTeamDefeatActivatesAgainstItsActingController(int player, bool hero, bool transferred)
    {
        // 04070: "Crossbones activates against the player who defeated this scheme."
        // rr:ownership-and-control.5: a changed-control card moves to its controller's play area.
        var (world, villain, scheme) = Board(player, hero);
        int owner = transferred ? 1 - player : player;
        var team = world.CreateCard("01064",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(owner), cardOwner: owner));
        Reveal.EnterPlay(world, Cards, team, [], abilities: AuthoredCards.Runner());
        if (transferred) World.MoveToTop(team, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(player)));
        Assert.Equal(owner, team.Owner);
        var runner = AuthoredCards.Runner();
        var action = Assert.Single(runner.Actions(world, player), offer => offer.Card == team.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(player, choice.Player);
        var selected = Assert.Single(choice.Affordances, offer => offer.AnchorId == scheme.ObjectId);
        Sequence.Answer(world, Cards, runner, choice, Decision.Take(selected.Id), events);
        Sequence.Finish(world, Cards, runner, events);
        AssertActivation(world, villain, scheme, hero, events);
        Assert.False(team.Ready);
        Assert.Equal(2, team.Tokens["c_snoop"]);
    }

    [Theory]
    [InlineData(0, true, false)]
    [InlineData(1, true, false)]
    [InlineData(0, false, false)]
    [InlineData(1, false, false)]
    [InlineData(1, true, true)]
    [InlineData(1, false, true)]
    [Rule("rr:thwart.1")]
    [Rule("rr:ally.3")]
    public void AllyBasicThwartDefeatRetainsControllerThroughActivationAndConsequence(int player, bool hero, bool transferred)
    {
        // "A hero or ally can use their basic thwart power"; consequential damage follows its resolution.
        var (world, villain, scheme) = Board(player, hero);
        int owner = transferred ? 1 - player : player;
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(owner), cardOwner: owner));
        if (transferred) World.MoveToTop(ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player)));
        var events = new List<GameEvent>();
        AllyBasicPowers.AllyPower(world, Cards, ally, scheme, BasicPowers.ThwartVerb, events);
        Sequence.Finish(world, Cards, AuthoredCards.Runner(), events);
        AssertActivation(world, villain, scheme, hero, events);
        Assert.Equal(owner, ally.Owner);
        Assert.False(ally.Ready);
        Assert.Equal(1, ally.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:you-your.15")]
    [Rule("rr:you-your.18")]
    public void ExplicitResponsiblePlayerDoesNotBecomeIdentityCredit(bool simultaneous)
    {
        // Allies' and supports' functions "are not considered to be performed by the controlling player's identity".
        var world = Hero("01001a", 2);
        var occurrence = new Occurrence(1, []);
        var scheme = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", 1);
        var request = new ThreatRemovalRequest(scheme, 1, "test", "Remove_Threat")
            { ResponsiblePlayer = 1, RecordDefeatOn = occurrence };
        if (simultaneous)
        {
            var second = world.CreateCard("01108", world.AreaOf(DeckType.SideSchemesArea));
            second.PlaceTokens("k_threat", 1);
            ThreatBatchRemoval.Remove(world, world.ThreatAbilities,
                [request, new(second, 1, "test", "Remove_Threat")
                    { ResponsiblePlayer = 0, RecordDefeatOn = occurrence }], []);
        }
        else Threat.Remove(world, Cards, world.ThreatAbilities, request, []);
        Assert.All(occurrence.Defeats, defeat => Assert.Equal(-1, defeat.By));
        Assert.Equal(1, occurrence.Defeats.Single(defeat => defeat.Card == scheme.ObjectId).ResponsiblePlayer);
        Assert.Equal(simultaneous ? 2 : 1, occurrence.Defeats.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:you-your.15")]
    [Rule("rr:you-your.18")]
    public void TransferredSupportOrAllyDoesNotTriggerEitherPlayersIdentityOnlyResponse(bool ally)
    {
        // Synthetic observation of "after you defeat" isolates identity credit from 04070's explicit player.
        var (world, villain, scheme) = Board(1, hero: false);
        var observer = AbilityCatalog.Parse("""
            {"cards":[{"card":"01063","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"ForcedResponse","subject":"game"},
              "when":{"defeatedByYou":"you"},
              "effect":{"heal":{"card":"you","amount":1}}
            }]}]}
            """);
        var runner = new AbilityRunner(AuthoredCards.Book with
        {
            Abilities = [.. AuthoredCards.Book.Abilities.Where(ability => ability.Card != "01063"), .. observer.Abilities]
        });
        world.Abilities = runner;
        foreach (var seat in world.Seats)
        {
            seat.IdentityCard.TakeDamage(2);
            world.CreateCard("01063", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(seat.Index), cardOwner: seat.Index));
        }
        var area = ally ? DeckType.AlliesArea : DeckType.SupportsArea;
        var source = world.CreateCard(ally ? "01083" : "01064", world.AreaOf(area, PlayArea.Of(0), cardOwner: 0));
        Reveal.EnterPlay(world, Cards, source, [], abilities: runner);
        World.MoveToTop(source, world.AreaOf(area, PlayArea.Of(1)));
        var events = new List<GameEvent>();
        if (ally) AllyBasicPowers.AllyPower(world, Cards, source, scheme, BasicPowers.ThwartVerb, events);
        else
        {
            var action = Assert.Single(runner.Actions(world, 1), offer => offer.Card == source.ObjectId);
            events.AddRange(runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events);
            var choice = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
            Sequence.Answer(world, Cards, runner, choice,
                Decision.Take(Assert.Single(choice.Affordances, offer => offer.AnchorId == scheme.ObjectId).Id), events);
        }
        Sequence.Finish(world, Cards, runner, events);
        AssertActivation(world, villain, scheme, hero: false, events);
        Assert.All(world.Seats, seat => Assert.Equal(2, seat.IdentityCard.Damage));
        Assert.Equal(0, source.Owner);
    }

    [Fact]
    public void EncounterRemovalDoesNotCreditThePlayerResolvingItsReveal()
    {
        // Synthetic encounter removal has a resolving seat but no player-performed defeating action.
        var (world, _, _) = Board(1, hero: true);
        var removal = AbilityCatalog.Parse("""
            {"cards":[{"card":"04067","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"removeThreat":{"scheme":{"query":"schemes"},"amount":1}}
            }]}]}
            """);
        var runner = new AbilityRunner(AuthoredCards.Book with
        {
            Abilities = [.. AuthoredCards.Book.Abilities.Where(ability => ability.Card != "04067"), .. removal.Abilities]
        });
        world.Abilities = runner;
        var source = world.CreateCard("04067", world.AreaOf(DeckType.RevealingArea));
        Agendas.Happening(world);
        runner.ExecuteReveal(world, source, 1);
        var failure = Assert.Throws<RulesNotImplementedException>(() => Sequence.Finish(world, Cards, runner, []));
        Assert.Contains("no defeating player", failure.Message, StringComparison.Ordinal);
    }

    private static (World World, Card Villain, Card Scheme) Board(int player, bool hero)
    {
        var world = new World(Cards, 2);
        for (int seatIndex = 0; seatIndex < 2; seatIndex++)
        {
            var seat = world.CreateSeat($"p{seatIndex}");
            seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
        }
        // Different forms distinguish the responsible player from the other seat and first player.
        if (!hero) Forms.Change(world.Seats[player], Cards);
        if (hero) Forms.Change(world.Seats[1 - player], Cards);
        world.FirstPlayer = 1 - player;
        world.Abilities = AuthoredCards.Runner();
        var villain = world.CreateCard("04058", world.AreaOf(DeckType.VillainArea));
        Statuses.Give(world, villain, "stunned");
        Statuses.Give(world, villain, "confused");
        var scheme = world.CreateCard("04070", world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", 1);
        return (world, villain, scheme);
    }

    private static void AssertActivation(World world, Card villain, Card scheme, bool hero, List<GameEvent> events)
    {
        Assert.Equal(!hero, Statuses.Has(world, villain, "stunned"));
        Assert.Equal(hero, Statuses.Has(world, villain, "confused"));
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
        Assert.Single(events.OfType<FieldSet>(), change => change.Card == scheme.ObjectId && change.Field == "k_threat");
        Assert.False(world.Agenda.IsBusy);
    }
}
