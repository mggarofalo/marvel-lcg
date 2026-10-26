using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.BlackWidowEventCompositionFixture;

namespace Marvel.Content.Tests.Play;

public sealed class CovertOpsModifierReviewTests
{
    [Fact]
    [Rule("rr:steady.1")]
    public void SteadyVillainCanReceiveItsSecondConfusedCardWithoutAScheme()
    {
        // "This character can have 1 additional confused status card."
        var world = Board();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", -main.Tokens.GetValueOrDefault("k_threat"));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "steady", 1, Affects: villain.ObjectId));
        Statuses.Give(world, villain, Statuses.Confused);
        var source = PlayEvent(world, "08003");
        Sequence.Finish(world, world.Facts, world.Abilities!, []);
        Assert.Equal(2, Statuses.Count(world, villain, Statuses.Confused));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(0, main.Tokens["k_threat"]);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:stalwart.1")]
    public void StalwartVillainCannotMakeAnOtherwiseEffectlessEventLegal()
    {
        // "This character cannot have confused or stunned status cards."
        var world = Board();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", -main.Tokens.GetValueOrDefault("k_threat"));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "stalwart", 1, Affects: villain.ObjectId));
        Assert.Throws<RulesNotImplementedException>(() => PlayEvent(world, "08003"));
        Assert.Equal(4, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(0, Statuses.Count(world, villain, Statuses.Confused));
        Assert.False(world.Agenda.IsBusy);
        // The otherwise identical, equally priced card can resolve its threat sentence.
        main.PlaceTokens("k_threat", 6);
        var source = world.Seats[1].Hand.Cards.Single(card => card.FaceId == "08003");
        var runner = (AbilityRunner)world.Abilities!;
        var action = Assert.Single(runner.Actions(world, 1), ability => ability.Card == source.ObjectId);
        int[] payment = world.Seats[1].Hand.Cards.Where(card => card.FaceId == "01093").Select(card => card.ObjectId).ToArray();
        runner.ExecuteAction(world, new PlayerAction(action, payment, []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(main.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(2, main.Tokens["k_threat"]);
        Assert.Equal(0, Statuses.Count(world, villain, Statuses.Confused));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cannot.1")]
    public void CannotThwartRejectsIndependentSentenceBeforePaymentOrConfusionCancellation(bool confused)
    {
        // "If two abilities conflict, the ability with 'cannot' takes precedence."
        var world = Board();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", -main.Tokens.GetValueOrDefault("k_threat"));
        var identity = world.Seats[1].IdentityCard;
        if (confused) Statuses.Give(world, identity, Statuses.Confused);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, ThwartPermissions.ProhibitedField,
            1, Card: identity.ObjectId, Affects: identity.ObjectId));
        Assert.Throws<RulesNotImplementedException>(() => PlayEvent(world, "08003"));
        Assert.Equal(4, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(confused, Statuses.Has(world, identity, Statuses.Confused));
        Assert.False(Statuses.Has(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Confused));
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void EventThreatBonusRespectsIndependentSentenceAndCleansUpAfterBothBranches(int threat)
    {
        var world = Board();
        var rows = JsonNode.Parse(Rows)!;
        // Synthetic Shrink-shaped interrupt isolates the existing event modifier pipeline.
        rows["cards"]!.AsArray().Add(JsonNode.Parse("""
            {"card":"05011","abilities":[{
              "trigger":{"event":"WhenEventPlayInitiated","timing":"Interrupt","subject":"you","form":"hero"},
              "when":{"hasTrait":{"card":"trigger.subject","trait":"THWART"}},
              "cost":{"exhaust":"this"},
              "effect":{"modifyPlayedEvent":{"quantity":"threatRemoval","amount":2}}}]}
            """));
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(rows.ToJsonString()));
        var upgrade = world.CreateCard("05011", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", threat - main.Tokens.GetValueOrDefault("k_threat"));
        var source = PlayEvent(world, "08003");
        if (threat > 0)
        {
            var interrupt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, world.Abilities, []));
            var option = Assert.Single(interrupt.Affordances, offer => offer.AnchorId == upgrade.ObjectId);
            Sequence.Answer(world, world.Facts, world.Abilities, interrupt, Decision.Take(option.Id), []);
            var choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, world.Abilities, []));
            Sequence.Answer(world, world.Facts, world.Abilities, choice, Decision.Take(main.ObjectId), []);
        }
        else Assert.Null(Sequence.Work(world, world.Facts, world.Abilities, []));
        Sequence.Finish(world, world.Facts, world.Abilities, []);
        Assert.Equal(threat == 0 ? 0 : 2, main.Tokens["k_threat"]);
        Assert.True(Statuses.Has(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Confused));
        Assert.Equal(threat == 0, upgrade.Ready);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind == "eventThreatRemoval");
        Assert.False(world.Agenda.IsBusy);
    }
}
