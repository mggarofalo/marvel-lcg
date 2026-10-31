using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Admitted Rise of Red Skull content: the piercing outcome that replay v31 changes.
public sealed class RedHousePiercingToughTests
{
    [Theory]
    [InlineData(true, 0, true)]
    [InlineData(false, 2, false)]
    [Rule("rr:piercing.2")]
    [Rule("rr:cannot")]
    public void BlackKnightsPiercingLeavesToughOnRedSkullWhileTheRedHouseProtectsHim(
        bool house, int damage, bool toughAfter)
    {
        // 04139: "Red Skull cannot take damage." 04012: "Black Knight's basic
        // attack gains piercing." rr:piercing.2: "If an attack with the piercing
        // keyword would deal no damage to the attacked character, it does not
        // discard tough status cards from that character." Once the Red House
        // leaves, the piercing discards Tough before Black Knight's 2 damage.
        var f = RedSkullFixture.Create();
        if (!house)
        {
            var redHouse = f.World.AreaOf(DeckType.SideSchemesArea).Cards.Single(card => card.FaceId == "04139");
            World.MoveToTop(redHouse, f.World.AreaOf(DeckType.SideSchemeDiscardPile));
        }
        Statuses.Give(f.World, f.Villain, Statuses.Tough);
        var knight = f.World.CreateCard("04012", f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));

        BasicPowers.AllyPower(f.World, f.World.Facts, knight, f.Villain, BasicPowers.AttackVerb, []);
        f.Finish();

        Assert.Equal(damage, f.Villain.Damage);
        Assert.Equal(toughAfter, Statuses.Has(f.World, f.Villain, Statuses.Tough));
    }
}
