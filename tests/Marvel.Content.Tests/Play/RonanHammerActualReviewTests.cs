using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent actual 90004 checks; synthetic supporting abilities are named explicitly.</summary>
public sealed class RonanHammerActualReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:activation.2")]
    public void RevealUsesNonFirstEngagedSeatAndItsFormInsteadOfRevealingSeat(bool hero)
    {
        // Minions activate against the engaged player, whose form determines attack or scheme.
        var f = new RonanRemainingFixture();
        f.World.FirstPlayer = 0;
        f.World.Seats[hero ? 0 : 1].IdentityCard.TurnTo("01001b");
        var ronan = f.World.CreateCard("90001", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var hammer = f.Reveal("90004", 0);
        f.Finish();
        Assert.Equal(0, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(hero ? 3 : 0, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(hero ? 0 : 3, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(hero ? [ronan.ObjectId] : [], f.Events.OfType<AttackCompleted>().Select(item => item.Enemy));
        Assert.Equal(DeckType.EncounterDiscardPile, hammer.Area.Type);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
    }

    [Fact]
    [Rule("rr:boost-boost-icon.2")]
    public void ActualBoostDealsToMovedFirstPlayerRatherThanItsResolvingPlayer()
    {
        // Only the boost text is active; printed 90004 names the first player.
        var f = new RonanRemainingFixture();
        f.World.FirstPlayer = 1;
        var ally = f.World.CreateCard("01083", f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        f.Card("90004", DeckType.EncounterDeck);
        f.Activate(true, 0);
        f.Finish(prompt => Defend(prompt, ally));
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(5, Assert.Single(f.Events.OfType<AttackCompleted>()).DamageDealt);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.False(Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards).FaceUp);
    }

    [Fact]
    [Rule("rr:activation.8")]
    public void ActualBoostCannotBorrowDefeatFromAnActivationQueuedByAnotherBoost()
    {
        // A newly initiated activation resolves "after the current activation has finished resolving".
        // Synthetic 01105 queues a minion attack; 90004 is retained from the actual production book.
        var f = new RonanRemainingFixture(WithSupport("01105", """
            {"card":"01105","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
              "effect":{"enemyAttacks":{"enemies":{"query":"minions"},"against":"you"}}
            }]}
            """));
        var minion = f.World.CreateCard("01102", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        f.World.Seats[1].IdentityCard.TakeDamage(4);
        var hammer = f.Card("90004", DeckType.EncounterDeck);
        f.Card("01105", DeckType.EncounterDeck);
        Attack.GiveAdditionalBoostCard(f.World, f.Villain, "test", f.Events);
        f.Activate(true);
        f.Finish();
        Assert.Equal([f.Villain.ObjectId, minion.ObjectId], f.Events.OfType<AttackCompleted>().Select(item => item.Enemy));
        Assert.True(f.World.Seats[1].Eliminated);
        Assert.Equal(DeckType.EncounterDiscardPile, hammer.Area.Type);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:damage.step.6")]
    public void ActualBoostDoesNotCountRecoveredRecipientOrSeparateInterruptDefeat(bool collateral)
    {
        // Step 6 resolves "when [character] would be defeated" before actual defeat.
        // Synthetic Mockingbird recovers; its optional collateral damage owns a separate defeat.
        var support = """
            {"card":"01083","abilities":[{
              "trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt","subject":"this"},
              "effect":{"seq":[COLLATERAL{"heal":{"card":"trigger.subject","amount":100}}]}
            }]}
            """.Replace("COLLATERAL", collateral
                ? """{"dealDamage":{"cards":{"titled":"Black Cat"},"amount":3}},""" : "", StringComparison.Ordinal);
        var f = new RonanRemainingFixture(WithSupport("01083", support));
        var ally = f.World.CreateCard("01083", f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var other = f.World.CreateCard("01002", f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        f.Card("90004", DeckType.EncounterDeck);
        f.Activate(true);
        f.Finish(prompt => Defend(prompt, ally));
        Assert.Equal(5, Assert.Single(f.Events.OfType<AttackCompleted>()).DamageDealt);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(0, ally.Damage);
        Assert.Equal(collateral ? DeckType.DiscardPile : DeckType.AlliesArea, other.Area.Type);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
    }

    private static AbilityRunner WithSupport(string replacedFace, string support)
    {
        using var actual = JsonDocument.Parse(AuthoredCards.Text);
        var rows = actual.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.NotEqual("90004", replacedFace);
        var text = "{\"cards\":[" + string.Join(",", rows.Where(row => row.GetProperty("card").GetString() != replacedFace)
            .Select(row => row.GetRawText()).Append(support)) + "]}";
        using var fixture = JsonDocument.Parse(text);
        Assert.Equal(rows.Single(row => row.GetProperty("card").GetString() == "90004").GetRawText(),
            fixture.RootElement.GetProperty("cards").EnumerateArray().Single(row => row.GetProperty("card").GetString() == "90004").GetRawText());
        return new AbilityRunner(AbilityCatalog.Parse(text));
    }

    private static Decision Defend(Prompt prompt, Card ally) => prompt.Asking == Question.Defender
        && prompt.Affordances.Any(option => option.AnchorId == ally.ObjectId)
            ? Decision.Take(prompt.Affordances.Single(option => option.AnchorId == ally.ObjectId).Id)
            : Decision.Decline;
}
