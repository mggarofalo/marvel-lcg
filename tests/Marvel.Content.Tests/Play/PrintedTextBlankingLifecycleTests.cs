using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PrintedTextBlankingLifecycleTests
{
    [Fact]
    [Rule("rr:permanent.1")]
    public void PermanentMinionRejectsBlankingBeforeSpendingItsMentalResource()
    {
        // "Effects on cards not from this card's set cannot ... blank any part
        // of its text box." Same-set exceptions remain outside this capability.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"05028","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"spend":"B"},"effect":{"blankPrintedTextUntilEndOfPhase":"this"}
            }]}]}
            """));
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var minion = world.CreateCard("05028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var payment = world.CreateCard("01089", world.Seats[0].Hand);
        var offered = Assert.Single(runner.Actions(world, 0), action => action.Card == minion.ObjectId);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "permanent",
            Amount: 1, Card: minion.ObjectId, Affects: minion.ObjectId,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound)));
        string before = world.Digest().Canonical();
        var pending = world.Agenda.Outstanding.ToArray();
        var effects = world.Effects.Registered.ToArray();
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == minion.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(offered, [payment.ObjectId], [])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(effects, world.Effects.Registered);
        Assert.Same(world.Seats[0].Hand, payment.Area);
    }

    [Fact]
    [Rule("rr:keywords.1")]
    public void NumericPrintedKeywordsAreRemovedBeforeIndependentModifiers()
    {
        // Keywords "appear at the start of a card's text box"; independently
        // created lasting effects are not that printed text.
        var (_, world) = Playing(_ => { }, hero: true);
        var minion = world.CreateCard("01172", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var source = world.Seats[0].IdentityCard;
        Assert.Equal(1, StateFields.Modified(world, minion, "retaliate", world.Facts, 1));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "retaliate",
            Amount: 2, Card: source.ObjectId, Affects: minion.ObjectId,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound)));
        Assert.Equal(3, StateFields.Modified(world, minion, "retaliate", world.Facts, 1));
        world.Effects.Register(PrintedText.UntilEndOfPhase(world, source, minion));
        Assert.Equal(2, StateFields.Modified(world, minion, "retaliate", world.Facts, 1));
        Assert.Equal(3, StateFields.Modified(world, minion, "attack", world.Facts, 1));
        PhaseEnd.EndPlayerTurn(world, 0, []);
        Assert.True(PrintedText.IsBlanked(world, minion));
        PhaseEnd.EndPlayerPhase(world, []);
        Assert.False(PrintedText.IsBlanked(world, minion));
        Assert.Equal(3, StateFields.Modified(world, minion, "retaliate", world.Facts, 1));
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.7")]
    [Rule("rr:lasting-effects.5")]
    public void SuspendedAbilityFinishesWhileBlankedAndSerializedDecisionsReplay()
    {
        // "The card is played or the ability ... resolves."
        // Blanking the source does not undo an already initiated ability.
        var original = Prepared();
        var replay = Prepared();
        int before = original.World.Seats[0].Hand.Cards.Count;
        var originalAction = Assert.Single(original.Game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == original.Source.ObjectId);
        var replayAction = Assert.Single(replay.Game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == replay.Source.ObjectId);
        original.Game.Resolve(Decision.Take(originalAction.Id));
        replay.Game.Resolve(Decision.Take(replayAction.Id));
        Assert.Equal(Question.Option, original.Game.Pending!.Asking);
        Assert.Equal(Question.Option, replay.Game.Pending!.Asking);
        foreach (var fixture in new[] { original, replay })
        {
            Assert.True(PersistentCardFacts.Describe(fixture.World, fixture.Source)!.HasUnresolvedAbilities);
            fixture.World.Effects.Register(PrintedText.UntilEndOfPhase(fixture.World,
                fixture.World.Seats[0].IdentityCard, fixture.Source));
            Assert.Empty(PersistentCardFacts.Describe(fixture.World, fixture.Source)!.Abilities);
            Assert.False(PersistentCardFacts.Describe(fixture.World, fixture.Source)!.HasUnresolvedAbilities);
            Assert.DoesNotContain(fixture.Runner.Actions(fixture.World, 0),
                action => action.Card == fixture.Source.ObjectId);
        }
        var decision = Decision.Take(1);
        var restored = JsonSerializer.Deserialize<Decision>(JsonSerializer.Serialize(decision))!;
        original.Game.Resolve(decision);
        replay.Game.Resolve(restored);
        Assert.Equal(before + 2, replay.World.Seats[0].Hand.Cards.Count);
        Assert.Equal(original.World.Digest().Canonical(), replay.World.Digest().Canonical());
        Assert.Equal(original.World.Effects.Registered, replay.World.Effects.Registered);
        Assert.Equal(original.World.Random.Generator.WordsConsumed, replay.World.Random.Generator.WordsConsumed);
        Assert.False(replay.World.Agenda.IsBusy);
        Assert.Equal(Question.TurnOption, replay.Game.Pending!.Asking);
    }

    [Theory]
    [InlineData("05028", "Response", "{\"blankPrintedTextUntilEndOfPhase\":\"this\"}")]
    [InlineData("05028", "Action", "{\"seq\":[{\"blankPrintedTextUntilEndOfPhase\":\"this\"},{\"draw\":{\"player\":\"you\",\"count\":1}}]}")]
    public void UnreviewedBlankingBodiesFailBeforeAnyGameOrCosts(string card, string timing, string effect)
    {
        // Admission is an engine scope choice: only a standalone self-blank
        // action has projection support for this prerequisite.
        string json = $$$"""
            {"cards":[{"card":"{{{card}}}","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"{{{timing}}}","subject":"this"},
              "cost":{"spend":"B"},"effect":{{{effect}}}
            }]}]}
            """;
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(json)));
    }

    [Fact]
    [Rule("rr:ability")]
    [Rule("rr:lasting-effects.1")]
    public void RegisteredConstantsStopWhileBlankedButIndependentLastingEffectsContinue()
    {
        // A constant ability "remains active while the card is in play";
        // blank text supplies no such ability. A lasting effect outlives its source.
        var (_, world) = Playing(_ => { }, hero: true);
        var minion = world.CreateCard("05028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var identity = world.Seats[0].IdentityCard;
        long printed = StateFields.Modified(world, identity, "attack", world.Facts, 1);
        world.Effects.Register(new ContinuousEffect(EffectSource.ConstantAbility, "attack",
            Amount: 2, Card: minion.ObjectId, Affects: identity.ObjectId));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "attack",
            Amount: 3, Card: minion.ObjectId, Affects: identity.ObjectId,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound)));
        Assert.Equal(printed + 5, StateFields.Modified(world, identity, "attack", world.Facts, 1));
        world.Effects.Register(PrintedText.UntilEndOfPhase(world, identity, minion));
        Assert.Equal(printed + 3, StateFields.Modified(world, identity, "attack", world.Facts, 1));
        PhaseEnd.EndPlayerPhase(world, []);
        Assert.Equal(printed + 5, StateFields.Modified(world, identity, "attack", world.Facts, 1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:ability.1")]
    public void BlankedSourceDoesNotDiscoverNewEntryOrActivationAbilities(bool activation)
    {
        // "Card abilities only interact with ... cards that are in play";
        // a blank printed source supplies no new triggered ability.
        // Synthetic minion lifecycle abilities isolate discovery, not printed content.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"05028","abilities":[
              {"trigger":{"event":"WhenCardEntersPlay","timing":"WhenRevealed","subject":"this"},
               "effect":{"draw":{"player":"firstPlayer","count":1}}},
              {"trigger":{"event":"WhenActivationCompleted","timing":"ForcedResponse","subject":"this"},
               "effect":{"draw":{"player":"firstPlayer","count":1}}}
            ]}]}
            """));
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var minion = world.CreateCard("05028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        int before = world.Seats[0].Hand.Cards.Count;
        Invoke();
        Assert.Equal(before + 1, world.Seats[0].Hand.Cards.Count);
        world.Effects.Register(PrintedText.UntilEndOfPhase(world, world.Seats[0].IdentityCard, minion));
        Invoke();
        Assert.Equal(before + 1, world.Seats[0].Hand.Cards.Count);
        PhaseEnd.EndPlayerPhase(world, []);
        int expired = world.Seats[0].Hand.Cards.Count;
        Invoke();
        Assert.Equal(expired + 1, world.Seats[0].Hand.Cards.Count);

        void Invoke()
        {
            var result = activation
                ? runner.CompleteActivation(world, new EnemyActivation(minion.ObjectId, 0, true))
                : runner.ExecuteEntry(world, minion);
            Assert.True(result.IsComplete);
        }
    }

    private static (Game Game, World World, Card Source, AbilityRunner Runner) Prepared()
    {
        // Synthetic minion action isolates continuation identity; it is not
        // Robot's printed action and does not admit a product card.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"05028","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"choose":{"options":[
                {"draw":{"player":"you","count":1}},
                {"draw":{"player":"you","count":2}}
              ]}}
            }]}]}
            """));
        Card? source = null;
        var (game, world) = Playing(board => source = board.CreateCard("05028",
            board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))), hero: true, abilities: runner);
        return (game, world, source!, runner);
    }
}
