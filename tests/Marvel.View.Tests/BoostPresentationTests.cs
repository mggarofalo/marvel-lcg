using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.View.Tests;

public sealed class BoostPresentationTests : EventPresentationTestBase
{
    [Theory]
    [InlineData(true, "ATK", 2, "icons")]
    [InlineData(false, "SCH", 1, "icon")]
    public void NamesTheContributionAndCurrentStatWithoutPromisingDamage(bool attacking, string stat, int icons, string noun)
    {
        var boost = new BoostResolved(7, 9, icons, attacking, 5)
        {
            Subjects = new Dictionary<int, string> { [7] = "Assault", [9] = "Rhino" },
        };
        var presented = Assert.Single(EventPresenter.Present([boost], World()));
        Assert.Equal($"Assault added {icons} boost {noun}; Rhino's {stat} is now 5.", presented.Summary);
        Assert.Equal([7, 9], presented.Anchors);
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void BothSubjectsMustBeReadableAndUnrelatedNamesAreRemoved(bool hideCard, bool hideEnemy, bool retained)
    {
        var facts = new VisibilityFacts();
        var world = new World(facts, players: 1);
        var seat = world.CreateSeat("player");
        seat.IdentityCard = world.CreateCard("hero", seat.Hero);
        var enemy = world.CreateCard("enemy", world.AreaOf(DeckType.VillainArea));
        var card = world.CreateCard("event", world.AreaOf(DeckType.EncounterDiscardPile));
        var secret = world.CreateCard("event", seat.Deck);
        var boost = new BoostResolved(hideCard ? secret.ObjectId : card.ObjectId,
            hideEnemy ? secret.ObjectId : enemy.ObjectId, 0, true, 3)
        {
            Subjects = new Dictionary<int, string>
                { [card.ObjectId] = "Boost card", [enemy.ObjectId] = "Rhino", [secret.ObjectId] = "Secret" },
        };
        var visible = WorldProjection.For(world, null, [boost],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players));
        if (!retained) Assert.Empty(visible.Events);
        else
        {
            var safe = Assert.IsType<BoostResolved>(Assert.Single(visible.Events));
            Assert.Equal(2, safe.Subjects!.Count);
            Assert.DoesNotContain(secret.ObjectId, safe.Subjects.Keys);
        }
    }

    private sealed class VisibilityFacts : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "hero" => CardKind.Hero,
            "enemy" => CardKind.EncounterVillain,
            "ally" => CardKind.Ally,
            _ => CardKind.Event,
        };
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public IReadOnlyList<string> Traits(string faceId) => [];
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
