using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkBeatCopMeaningTests
{
    [Fact]
    public void ActualActionsExplainMovementAndCapturedDamageWithoutChangingResolution()
    {
        var probed = new HulkFinalCardsFixture();
        var control = new HulkFinalCardsFixture();
        var source = probed.Support();
        var controlSource = control.Support();
        source.PlaceTokens("k_threat", 3);
        controlSource.PlaceTokens("k_threat", 3);
        var target = probed.Minion("01131");
        var controlTarget = control.Minion("01131");
        string digest = probed.World.Digest().Canonical();
        string random = JsonSerializer.Serialize(probed.World.Random.Generator.GetState());
        int count = probed.World.Agenda.Count;
        var current = probed.World.Agenda.Current;
        var actions = probed.Runner.Actions(probed.World, 1).Where(a => a.Card == source.ObjectId).ToArray();
        Assert.Contains("move 1 threat onto this card from", probed.Runner.Describe(probed.World, actions.Single(a => a.Ordinal == 0)).Description);
        Assert.Contains("Current damage: 3 before prevention/replacement", probed.Runner.Describe(probed.World, actions.Single(a => a.Ordinal == 1)).Description);
        Assert.Equal(digest, probed.World.Digest().Canonical());
        Assert.Equal(random, JsonSerializer.Serialize(probed.World.Random.Generator.GetState()));
        Assert.Equal(count, probed.World.Agenda.Count);
        Assert.Equal(current, probed.World.Agenda.Current);
        probed.Start(source, 1);
        control.Start(controlSource, 1);
        Assert.Equal(0, source.Tokens["k_threat"]);
        Assert.Contains("Current damage: 3 before prevention/replacement", probed.Pending!.Description);
        Assert.Contains("Current damage: 3 before prevention/replacement", probed.Pending.Affordances.Single(a => a.Id == target.ObjectId).Description);
        probed.Answer(Decision.Take(target.ObjectId));
        control.Answer(Decision.Take(controlTarget.ObjectId));
        probed.Finish();
        control.Finish();
        Assert.Equal(3, target.Damage);
        Assert.Equal(control.World.Digest().Canonical(), probed.World.Digest().Canonical());
        Assert.Equal(control.World.Agenda.Count, probed.World.Agenda.Count);
        Assert.Equal(control.World.Random.Generator.NextUInt32(), probed.World.Random.Generator.NextUInt32());
    }

    [Fact]
    public void ReenteredSourceCannotLendItsNewThreatToTheSuspendedChoice()
    {
        var f = new HulkFinalCardsFixture();
        var source = f.Support();
        source.PlaceTokens("k_threat", 3);
        var target = f.Minion();
        f.Start(source, 1);
        World.MoveToTop(source, f.World.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        source.PlaceTokens("k_threat", 9);
        var error = Assert.Throws<RulesNotImplementedException>(() =>
            f.Runner.Choosing(f.World, f.World.Agenda.Current!.Value));
        Assert.Contains("requires a choice and has no legal option", error.Message);
        Assert.Equal(0, target.Damage);
        Assert.Equal(9, source.Tokens["k_threat"]);
    }

    [Theory]
    [InlineData("{ \"tokensOn\": \"this\" }", true)]
    [InlineData("{ \"tokensOn\": \"chosen\" }", false)]
    [InlineData("{ \"count\": { \"cardsIn\": { \"area\": \"yourDeck\", \"kind\": \"Upgrade\" } } }", false)]
    public void ConcealedOrUnsupportedAmountsDoNotGainAKnownAmount(string amount, bool conceal)
    {
        // Synthetic direct choice isolates expression disclosure, not pack coverage.
        var f = new HulkFinalCardsFixture();
        var source = f.Support();
        source.PlaceTokens("k_threat", 7);
        f.Minion();
        var book = AbilityCatalog.Parse("""
            {"cards":[{"card":"10029","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"chooseCard":{"from":{"query":"minions"},
                "effect":{"dealDamage":{"cards":"chosen","amount":AMOUNT}}}}
            }]}]}
            """.Replace("AMOUNT", amount, StringComparison.Ordinal));
        var runner = new AbilityRunner(book);
        if (conceal) source.TurnFaceDown();
        var offer = runner.Describe(f.World, new(source.ObjectId, Marvel.Rules.Timing.AbilityType.Action, 1));
        Assert.DoesNotContain("Current damage", offer.Description ?? "");
        Assert.DoesNotContain("7", offer.Description ?? "");
    }
}
