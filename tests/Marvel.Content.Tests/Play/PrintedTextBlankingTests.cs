using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PrintedTextBlankingTests
{
    [Fact]
    [Rule("rr:keywords.1")]
    public void PrintedKeywordsDisappearWhileSeparatelyGrantedKeywordsAndStatsRemain()
    {
        // Keywords "appear at the start of a card's text box". Blanking printed
        // text does not remove an independently granted lasting effect.
        var runner = Runner();
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var identity = world.Seats[0].IdentityCard;
        Assert.True(Keywords.Has(world, minion, "guard", world.Facts));
        Assert.Equal(1, StateFields.Modified(world, minion, "guard", world.Facts, world.Players));
        long attack = StateFields.Modified(world, minion, "attack", world.Facts, world.Players);
        world.Effects.Register(PrintedText.UntilEndOfPhase(world, identity, minion));
        Assert.False(Keywords.Has(world, minion, "guard", world.Facts));
        Assert.Equal(0, StateFields.Modified(world, minion, "guard", world.Facts, world.Players));
        Assert.Equal(attack, StateFields.Modified(world, minion, "attack", world.Facts, world.Players));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Keywords.Ranged,
            Card: identity.ObjectId, Affects: minion.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound)));
        Assert.True(Keywords.Has(world, minion, Keywords.Ranged, world.Facts));
        PhaseEnd.EndPlayerPhase(world, []);
        Assert.True(Keywords.Has(world, minion, "guard", world.Facts));
        Assert.True(Keywords.Has(world, minion, Keywords.Ranged, world.Facts));
    }

    [Fact]
    [Rule("rr:resource.2")]
    public void WrongResourcePreservesTheRobotPaymentAndQueriesArePure()
    {
        // "Wild resources can be used as their type or any of the other types";
        // an energy resource alone cannot pay a mental resource requirement.
        var runner = Runner();
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var robot = world.CreateCard("05028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var mental = world.CreateCard("01089", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        var registered = world.Effects.Registered.ToArray();
        var action = Assert.Single(runner.Actions(world, 0), action => action.Card == robot.ObjectId);
        Assert.False(runner.CanTakeDamage(world, robot, world.Seats[0].IdentityCard));
        Assert.Throws<RulesNotImplementedException>(() =>
            runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId], [])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
        Assert.Equal(registered, world.Effects.Registered);
        Assert.Same(world.Seats[0].Hand, mental.Area);
    }

    [Fact]
    [Rule("rr:ability.13")]
    public void AlterEgoCannotTriggerTheRobotHeroAction()
    {
        // "The ability can only be used if the player triggering the ability
        // is in the specified form."
        var runner = Runner();
        var (_, world) = Playing(_ => { }, abilities: runner);
        var robot = world.CreateCard("05028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("01089", world.Seats[0].Hand);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == robot.ObjectId);
        world.Seats[0].IdentityCard.TurnTo("01001a");
        Assert.Contains(runner.Actions(world, 0), action => action.Card == robot.ObjectId);
    }

    private static AbilityRunner Runner() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"05028","abilities":[
          {"trigger":{"timing":"Constant","subject":"this"},
           "effect":{"preventDamageWhile":{"card":"this","condition":{"atLeast":{"value":1,"count":1}}}}},
          {"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
           "cost":{"spend":"B"},"effect":{"blankPrintedTextUntilEndOfPhase":"this"}}
        ]}]}
        """));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:ability.8")]
    [Rule("rr:lasting-effects.5")]
    [Rule("rr:traits.2")]
    public void RobotMentalPaymentSuppressesItsWholeBodyUntilPhaseEnd(int actingPlayer)
    {
        // Printed 05028: "Spend a [mental] resource"; "until the end of the phase"
        // treats its printed text box as blank. rr:ability.8: "Any player can use"
        // an optional encounter ability. rr:traits.2 excludes traits from blanking.
        var runner = Runner();
        var (_, world) = Playing(_ => { }, hero: true,
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        world.Seats[1].IdentityCard.TurnTo("01010a");
        var robot = world.CreateCard("05028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var source = world.Seats[actingPlayer].IdentityCard;
        var mental = world.CreateCard("01089", world.Seats[actingPlayer].Hand);
        Assert.Contains('B', Resources.GeneratedBy(mental.FaceId, world.Facts));
        Assert.False(runner.CanTakeDamage(world, robot, source));
        var action = Assert.Single(runner.Actions(world, actingPlayer), action => action.Card == robot.ObjectId);

        runner.ExecuteAction(world, new PlayerAction(action, [mental.ObjectId], []));

        Assert.Equal(DeckType.DiscardPile, mental.Area.Type);
        Assert.True(PrintedText.IsBlanked(world, robot));
        Assert.True(runner.CanTakeDamage(world, robot, source));
        Assert.DoesNotContain(runner.Actions(world, actingPlayer), action => action.Card == robot.ObjectId);
        Assert.True(Traits.Has(world, robot, "ROBOT", world.Facts));
        Assert.Equal(2, StateFields.Modified(world, robot, "attack", world.Facts, world.Players));
        PhaseEnd.EndPlayerPhase(world, []);
        Assert.False(PrintedText.IsBlanked(world, robot));
        Assert.False(runner.CanTakeDamage(world, robot, source));
        Assert.Contains(runner.Actions(world, actingPlayer), action => action.Card == robot.ObjectId);
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    [Rule("rr:lasting-effects.1")]
    public void ANewCopyDoesNotInheritTheBlankAndExistingLastingEffectsSurvive()
    {
        // "A card that leaves play and then re-enters play is considered to be
        // a new copy"; lasting effects persist beyond their creating ability.
        var runner = Runner();
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var area = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        var robot = world.CreateCard("05028", area);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "attack",
            Amount: 3, Card: robot.ObjectId, Affects: world.Seats[0].IdentityCard.ObjectId,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound)));
        world.Effects.Register(PrintedText.UntilEndOfPhase(world, robot, robot));
        Assert.True(PrintedText.IsBlanked(world, robot));
        Assert.Contains(world.Effects.Active(), effect => effect.Kind == "attack" && effect.Amount == 3);
        Discard.Card(world, robot, "test departure", []);
        World.MoveToTop(robot, area);
        Assert.False(PrintedText.IsBlanked(world, robot));
        Assert.False(runner.CanTakeDamage(world, robot, world.Seats[0].IdentityCard));
        Assert.Contains(world.Effects.Active(), effect => effect.Kind == "attack" && effect.Amount == 3);
    }
}
