using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows on synthetic boards.
public sealed class QuicksilverIdentityTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:basic-power.1.2")]
    [Rule("rr:limit")]
    public void SuperSpeedReadiesQuicksilverAfterABasicThwartOncePerPhase()
    {
        // 14001a: "Response: After you use one of Quicksilver's basic powers (THW,
        // ATK, or DEF), ready him. (Limit once per phase.)" rr:basic-power.1.2: thwart
        // power "can be used by a character to perform a basic thwart". rr:limit:
        // "Each copy of an ability with such a limit may be used X times per the
        // specified period".
        var world = QuicksilverBoards.Board();
        var quicksilver = world.Seats[0].IdentityCard;
        var main = QuicksilverBoards.MainScheme(world);
        main.PlaceTokens("k_threat", 6);
        var offered = new List<int>();

        BasicThwartPowers.BasicThwart(world, Cards, 0, main, []);
        QuicksilverBoards.Drive(world, QuicksilverBoards.Take(quicksilver, offered));
        Assert.Single(offered);
        Assert.True(quicksilver.Ready);
        Assert.Equal(5, main.Tokens["k_threat"]);

        BasicThwartPowers.BasicThwart(world, Cards, 0, main, []);
        QuicksilverBoards.Drive(world, QuicksilverBoards.Take(quicksilver, offered));
        Assert.Single(offered);
        Assert.False(quicksilver.Ready);
        Assert.Equal(4, main.Tokens["k_threat"]);
    }

    [Fact]
    [Rule("rr:basic-power.1.1")]
    [Rule("rr:limit")]
    public void OneSuperSpeedLimitCoversAttackAndThwart()
    {
        // rr:basic-power.1.1: attack power "can be used by a character to perform a
        // basic attack". rr:limit: the once-per-phase limit belongs to the one printed
        // ability, whichever basic power triggered it.
        var world = QuicksilverBoards.Board();
        var quicksilver = world.Seats[0].IdentityCard;
        var villain = QuicksilverBoards.Villain(world);
        var main = QuicksilverBoards.MainScheme(world);
        main.PlaceTokens("k_threat", 6);
        var offered = new List<int>();

        BasicPowers.BasicAttack(world, Cards, 0, villain, []);
        QuicksilverBoards.Drive(world, QuicksilverBoards.Take(quicksilver, offered));
        Assert.Single(offered);
        Assert.True(quicksilver.Ready);
        Assert.Equal(1, villain.Damage);

        BasicThwartPowers.BasicThwart(world, Cards, 0, main, []);
        QuicksilverBoards.Drive(world, QuicksilverBoards.Take(quicksilver, offered));
        Assert.Single(offered);
        Assert.False(quicksilver.Ready);
    }

    [Fact]
    [Rule("rr:defend-defense.7.2")]
    public void SuperSpeedReadiesQuicksilverAfterTheAttackHeBasicDefended()
    {
        // rr:defend-defense.7.2: "If an ability triggers after a character uses a basic
        // power, that ability triggers after an attack in which a character made a
        // basic defense resolves." Ruling 9cfb9119e5c7f0d7: he readies "after he ...
        // exhausted and used his DEF to defend against an attack, after the damage
        // was dealt." Rhino (ATK 2) against DEF 1 deals Quicksilver 1.
        var world = QuicksilverBoards.Board();
        var quicksilver = world.Seats[0].IdentityCard;
        var offered = new List<int>();
        KangTemporalFixture.Attack(world, QuicksilverBoards.Villain(world), 0);

        QuicksilverBoards.Drive(world, prompt =>
            prompt.Affordances.SingleOrDefault(a => a.Verb == "Defense") is { } defense ? Decision.Take(defense.Id)
            : QuicksilverBoards.Take(quicksilver, offered)(prompt));

        Assert.Single(offered);
        Assert.Equal(1, quicksilver.Damage);
        Assert.True(quicksilver.Ready);
    }

    [Fact]
    [Rule("rr:stun-stunned.1")]
    public void AStunnedBasicAttackDoesNotTriggerSuperSpeed()
    {
        // rr:stun-stunned.1: "When this character would attack, remove each stunned
        // status card from it instead." Ruling 983ae8870c226cd4: he "is not considered
        // to have used a basic power and the condition of his Super Speed ability is
        // not met."
        var world = QuicksilverBoards.Board();
        var quicksilver = world.Seats[0].IdentityCard;
        var villain = QuicksilverBoards.Villain(world);
        Statuses.Give(world, quicksilver, Statuses.Stunned);
        var offered = new List<int>();

        BasicPowers.BasicAttack(world, Cards, 0, villain, []);
        QuicksilverBoards.Drive(world, QuicksilverBoards.Take(quicksilver, offered));

        Assert.Empty(offered);
        Assert.Equal(0, villain.Damage);
        Assert.False(quicksilver.Ready);
        Assert.False(Statuses.Has(world, quicksilver, Statuses.Stunned));
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 3)]
    [Rule("rr:cost.1")]
    [Rule("rr:subtitle")]
    public void SuperpoweredSiblingsDiscardsTwoAndDrawsMoreWithWanda(bool wanda, int drawn)
    {
        // 14001b: "Action: Discard 2 cards from your hand → draw 2 cards (draw 3
        // cards instead if Wanda Maximoff is in play). (Limit once per round.)"
        // rr:cost.1: the cost arrow separates the cost from the effect. rr:subtitle:
        // "A subtitle represents an alternate alias a character sometimes uses"; the
        // Scarlet Witch ally's subtitle is Wanda Maximoff.
        var world = QuicksilverBoards.Board("14001b");
        if (wanda) QuicksilverBoards.InPlay(world, "14002", DeckType.AlliesArea);
        var first = QuicksilverBoards.Hand(world, "01089");
        var second = QuicksilverBoards.Hand(world, "01090");
        var kept = QuicksilverBoards.Hand(world, "01088");
        var pietro = world.Seats[0].IdentityCard;

        QuicksilverBoards.Play(world, pietro, [], [first.ObjectId, second.ObjectId]);
        QuicksilverBoards.Drive(world, _ => null);

        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Assert.Same(world.Seats[0].Hand, kept.Area);
        Assert.Equal(1 + drawn, world.Seats[0].Hand.Cards.Count);
        Assert.DoesNotContain(QuicksilverBoards.Runner(world).Actions(world, 0), a => a.Card == pietro.ObjectId);
    }

    [Fact]
    [Rule("rr:identity.4")]
    public void WandaMaximoffOnlyInHandDoesNotCount()
    {
        // rr:identity.4: "The faceup side of an identity card is considered to be in
        // play." A card in hand is out of play, so the ally there is not in play.
        var world = QuicksilverBoards.Board("14001b");
        QuicksilverBoards.Hand(world, "14002");
        var first = QuicksilverBoards.Hand(world, "01089");
        var second = QuicksilverBoards.Hand(world, "01090");

        QuicksilverBoards.Play(world, world.Seats[0].IdentityCard, [], [first.ObjectId, second.ObjectId]);
        QuicksilverBoards.Drive(world, _ => null);

        Assert.Equal(3, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    [Rule("rr:cost.6")]
    public void SuperpoweredSiblingsNeedsTwoCardsToDiscard()
    {
        // The discard is the whole cost; one card cannot pay it (rr:cost.6 and
        // rr:initiating-abilities). The action is not offered.
        var world = QuicksilverBoards.Board("14001b");
        QuicksilverBoards.Hand(world, "01089");
        Assert.DoesNotContain(QuicksilverBoards.Runner(world).Actions(world, 0),
            a => a.Card == world.Seats[0].IdentityCard.ObjectId);
    }
}
