using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent prepared-board checks with the unmodified production book.</summary>
public sealed class RonanJudgeProductionBookReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:boost-boost-icon.2")]
    [Rule("rr:triggering-condition.1")]
    public void BoostEntryCanRespondToItsOwnActivationsLaterDefeat(bool lethal)
    {
        // Only text "beneath the divider line is active" while resolving as a
        // boost; after entry, 90002 answers the defeat "once per occurrence".
        var f = new RonanRemainingFixture();
        var judge = f.Card("90002", DeckType.EncounterDeck);
        var ally = Ally(f, "01041", 0, lethal ? 1 : 0);
        f.Activate(true);
        f.Finish(prompt => Defend(prompt, ally));
        Assert.Equal(DeckType.SideSchemesArea, judge.Area.Type);
        Assert.Equal(6, judge.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(lethal ? DeckType.DiscardPile : DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(lethal ? 2 : 0, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.Null(f.World.Attack);
    }

    [Fact]
    [Rule("rr:indirect-damage.3")]
    [Rule("rr:triggering-condition.1")]
    public void TwoIndirectDefeatsResolveTwiceAndDoNotLeakIntoTheNextAttack()
    {
        // Indirect damage is "resolved simultaneously"; an ability triggers
        // "once per occurrence of its triggering condition", here two defeats.
        var f = new RonanRemainingFixture();
        f.Reveal("90002");
        f.Finish();
        var first = Ally(f, "01041", 1, 2);
        var second = Ally(f, "01083", 1, 2);
        f.Activate(true);
        f.Finish(prompt =>
        {
            if (prompt.Asking == Question.Defender)
            {
                Attack.MakeIndirect(f.World);
                return Decision.Decline;
            }
            if (prompt.Asking == Question.Element)
                return Decision.Take(prompt.Affordances[0].Id, [first.ObjectId, second.ObjectId], []);
            return prompt.Asking == Question.Order ? Decision.Take(prompt.Affordances[0].Id) : Decision.Decline;
        });
        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Assert.Equal(4, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Single(f.Events.OfType<AttackCompleted>());
        f.Activate(true);
        f.Finish();
        Assert.Equal(4, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(2, f.Events.OfType<AttackCompleted>().Count());
        Assert.Null(f.World.Attack);
    }

    [Fact]
    [Rule("rr:attack-enemy-activation.7")]
    public void EnemyNonattackDamageDefeatDoesNotSatisfyThePrintedResponse()
    {
        // "not by an ability that resolves during the attack": an enemy source
        // and a friendly defeat alone do not make damage attack damage.
        var f = new RonanRemainingFixture();
        f.Reveal("90002");
        f.Finish();
        var ally = Ally(f, "01041", 0, 1);
        DamagePlacement.Resolve(f.World, f.World.Facts,
            new(f.Villain, ally, 2, "ReviewNonattack", "Deal_Damage"), f.Events);
        f.Finish();
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(0, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Empty(f.Events.OfType<AttackCompleted>());
    }

    private static Card Ally(RonanRemainingFixture f, string face, int controller, int damage)
    {
        var ally = f.World.CreateCard(face, f.World.AreaOf(DeckType.AlliesArea,
            PlayArea.Of(controller), cardOwner: controller));
        Assert.Equal(3, DamagePlacement.Health(f.World, f.World.Facts, ally));
        ally.TakeDamage(damage);
        return ally;
    }

    private static Decision Defend(Prompt prompt, Card ally) =>
        prompt.Asking == Question.Defender
            ? Decision.Take(Assert.Single(prompt.Affordances, option => option.AnchorId == ally.ObjectId).Id)
            : Decision.Decline;
}
