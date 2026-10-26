using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic projection contracts; no Harvest public admission.</summary>
public sealed class ExhaustionResultProjectionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LabelledPowerRejectsUncommittedReceiptBeforePaymentAndLeavesQueriesPure(bool receipt)
    {
        Action<World> prepare = board =>
        {
            board.CreateCard("01008", board.AreaOf(DeckType.UpgradesArea,
                PlayArea.Of(0), cardOwner: 0));
            board.CreateCard("01090", board.Seats[0].Hand);
            board.CreateCard("05006", board.AreaOf(DeckType.SupportsArea,
                PlayArea.Of(0), cardOwner: 0));
        };
        var (game, world) = ActionAbilitySupportFixtures6.Playing(prepare, hero: true, abilities: new NoCardAbilities());
        var (_, control) = ActionAbilitySupportFixtures6.Playing(prepare, hero: true, abilities: new NoCardAbilities());
        var runner = Runner(receipt);
        world.Abilities = runner;
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long consumed = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        var stage = world.Agenda.Stage;
        var prompt = game.Pending;
        for (int attempt = 0; attempt < 2; attempt++)
        {
            if (receipt)
            {
                var rejected = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
                Assert.Contains("uncommitted exhaustion receipt", rejected.Message, StringComparison.Ordinal);
            }
            else Assert.Contains(runner.Actions(world, 0), action => world.Cards[action.Card].FaceId == "01008");
        }
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(stage, world.Agenda.Stage);
        Assert.Same(prompt, game.Pending);
        // A rejected query must leave the same fixed-number action available.
        var valid = Runner(false);
        world.Abilities = valid;
        control.Abilities = valid;
        var action = Assert.Single(valid.Actions(world, 0), option => world.Cards[option.Card].FaceId == "01008");
        var controlAction = Assert.Single(valid.Actions(control, 0), option => control.Cards[option.Card].FaceId == "01008");
        int payment = Assert.Single(world.Seats[0].Hand.Cards, card => card.FaceId == "01090").ObjectId;
        valid.ExecuteAction(world, new PlayerAction(action, [payment], []));
        valid.ExecuteAction(control, new PlayerAction(controlAction, [payment], []));
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        Assert.Equal(control.Random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(control.Agenda.Outstanding.Select(step => (step.What, step.Subject, step.Seat, step.Number)),
            world.Agenda.Outstanding.Select(step => (step.What, step.Subject, step.Seat, step.Number)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AreaProjectionRefusesToInventAnExhaustionAmount(bool receipt)
    {
        var (world, _, scope) = WreckingProjectionFixture.Create();
        var identity = new AbilityCardSelection.Query(AbilityCardQuery.Identities);
        AbilityEffect[] effects = [
            new AbilityEffect.CardAction(AbilityCardInstruction.Exhaust, identity),
            new AbilityEffect.Heal(identity, receipt ? new AbilityNumber.Result("exhausted")
                : new AbilityNumber.Constant(1))];
        string before = world.Digest().Canonical();
        if (receipt)
        {
            var error = Assert.Throws<RulesNotImplementedException>(() =>
                AbilityAreaProjectionQueries.EffectsMayChangeAnyArea(effects,
                    new HashSet<DeckType> { DeckType.DiscardPile }, scope.Context));
            Assert.Contains("uncommitted exhaustion receipt", error.Message, StringComparison.Ordinal);
        }
        else Assert.False(AbilityAreaProjectionQueries.EffectsMayChangeAnyArea(effects,
            new HashSet<DeckType> { DeckType.DiscardPile }, scope.Context));
        Assert.Equal(before, world.Digest().Canonical());
    }

    private static AbilityRunner Runner(bool receipt) => new(AbilityCatalog.Parse($$$$$$"""
        {"cards":[{"card":"01008","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
          "cost":{"spend":"R"},"effect":{"attack":{"target":{"query":"villain"},
            "effect":{"seq":[{"exhaust":{"query":"supports"}},
              {"dealDamage":{"cards":"chosen","amount":{{{{{{(receipt ? "{\"result\":\"exhausted\"}" : "1")}}}}}}}}]}}}
        }]}]}
        """));
}
