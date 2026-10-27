using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkStarterAllyTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [Rule("rr:stun-stunned.7")]
    public void BrawnResponseRequiresCompletedAttackButNotDamage(bool tough, bool stunned)
    {
        // A canceled stunned attack "is not considered to have attacked".
        var f = new HulkStarterFixture();
        var ally = f.World.CreateCard("10011", f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 0));
        if (tough) Statuses.Give(f.World, f.Villain, Statuses.Tough);
        if (stunned) Statuses.Give(f.World, ally, Statuses.Stunned);
        long threat = f.Scheme.Tokens["k_threat"];
        int offered = 0;
        AllyBasicPowers.AllyPower(f.World, f.World.Facts, ally, f.Villain, BasicPowers.AttackVerb, f.Events);
        f.Finish(prompt =>
        {
            var offer = prompt.Affordances.SingleOrDefault(a => a.AnchorId == ally.ObjectId);
            if (offer is null) return null;
            offered++;
            Assert.Equal(1, prompt.Player);
            return new Decision(offer.Id, [f.Scheme.ObjectId], []);
        });
        Assert.Equal(stunned ? 0 : 1, offered);
        Assert.Equal(threat - (stunned ? 0 : 1), f.Scheme.Tokens["k_threat"]);
        Assert.Equal(stunned || tough ? 0 : 1, f.Villain.Damage);
        Assert.False(ally.Ready);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActualSentryEntryDealsOneFacedownEncounterToItsControllerInEitherForm(bool alterEgo)
    {
        var f = new HulkStarterFixture();
        if (alterEgo) f.Hero.TurnTo("01001b");
        var source = f.Held("10012");
        var pay = new[] { f.Held("10022"), f.Held("10022") };
        var top = f.World.AreaOf(DeckType.EncounterDeck).Cards[^1];
        long threat = f.Scheme.Tokens["k_threat"];
        CardPlay.Play(f.World, f.World.Facts, f.Runner, f.World.Seats[1], source,
            pay.Select(card => card.ObjectId).ToArray(), f.Events);
        f.Finish();
        Assert.Equal(DeckType.AlliesArea, source.Area.Type);
        Assert.Equal(1, source.Area.PlayArea.Player);
        Assert.Equal(DeckType.DealtEncounterCardsDeck, top.Area.Type);
        Assert.Equal(1, top.Area.PlayArea.Player);
        Assert.False(top.FaceUp);
        Assert.Equal(threat, f.Scheme.Tokens["k_threat"]);
        Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        f.Finish();
        Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
    }

    [Fact]
    public void ActualMartialProwessEntersUnderAnotherPlayersControlAndPaysActualDropKick()
    {
        var f = new HulkStarterFixture();
        var source = f.World.CreateCard("10018", f.World.Seats[0].Hand);
        var pay = f.World.CreateCard("10022", f.World.Seats[0].Hand);
        CardPlay.Play(f.World, f.World.Facts, f.Runner, f.World.Seats[0], source, [pay.ObjectId], f.Events,
            [f.Hero.ObjectId]);
        f.Finish();
        Assert.Equal(1, source.Area.PlayArea.Player);
        var eventCard = f.PlayEvent("10014", f.Villain, source, f.Held("10022"));
        f.Finish();
        Assert.False(source.Ready);
        Assert.Equal(DeckType.UpgradesArea, source.Area.Type);
        Assert.Equal(4, f.Villain.Damage);
        Assert.True(Statuses.Has(f.World, f.Villain, Statuses.Stunned));
        Assert.Equal(DeckType.DiscardPile, eventCard.Area.Type);
        var duplicate = f.Held("10018");
        Assert.DoesNotContain(f.Hero.ObjectId, CardPlay.LegalAttachmentTargets(f.World, f.World.Facts,
            f.World.Seats[1], duplicate, f.Runner)!);
    }
}
