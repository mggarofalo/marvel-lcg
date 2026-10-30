using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.KangObligationFixture;

namespace Marvel.Content.Tests.Play;

// Fear of Kang (11049) and the full authored book on a nonpublic Rhino board;
// Kang's public admission stays closed.
public sealed class KangFearOfKangTests : CoreCardTestBase
{
    private const string FearOfKang = "11049";
    private const string MasterOfTime = "11047";

    [Theory]
    [InlineData(MasterOfTime)]
    [InlineData("11001")]
    [InlineData("11006")]
    [Rule("rr:cannot")]
    [Rule("rr:obligation.1")]
    public void TheHoldersHeroCannotAttackAnyKang(string kang)
    {
        // 11049: "You cannot attack Kang." MARVEL-894 (Q2): every enemy titled Kang,
        // the Master of Time minion included (rulings 8da857fcc70a81a2, 7b48d5829d4d0f62).
        // Player 1 holds it; player 0 is not bound, and Rhino stays attackable.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var enemy = kang == MasterOfTime ? KangTemporalFixture.Engaged(world, kang, 1)
            : world.CreateCard(kang, world.AreaOf(DeckType.VillainArea));
        Obligation(world, FearOfKang, 1);
        var holder = world.Seats[1].IdentityCard;
        var other = world.Seats[0].IdentityCard;

        Assert.DoesNotContain(enemy, BasicPowers.Attackable(world, world.Facts, 1, holder));
        Assert.Contains(f.Villain, BasicPowers.Attackable(world, world.Facts, 1, holder));
        Assert.Contains(enemy, BasicPowers.Attackable(world, world.Facts, 0, other));
        Assert.Throws<RulesNotImplementedException>(() =>
            BasicPowerInitiation.BasicAttack(world, world.Facts, 1, enemy, []));
        Assert.True(holder.Ready);
    }

    [Fact]
    [Rule("rr:player-turn.5")]
    public void TheGameOffersTheHoldersHeroOnlyItsOtherTargetsAndTheAllyEvery()
    {
        // The turn's basic-attack offers read the same prohibition: Captain Marvel holds
        // Fear of Kang, so her attack offers Rhino alone; her ally's offers both.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        var kang = KangTemporalFixture.Engaged(world, MasterOfTime, 0);
        Obligation(world, FearOfKang, 0);
        var hero = world.Seats[0].IdentityCard;
        var ally = world.CreateCard("01059", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var game = Game.Begin(world, world.Facts, f.Runner);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            a => a.Verb == Game.ResolveMulligans).Id));

        var attacks = game.Pending!.Affordances.Where(a => a.Verb == BasicPowers.AttackVerb).ToArray();

        Assert.Equal([f.Villain.ObjectId],
            Assert.Single(attacks, a => a.AnchorId == hero.ObjectId).Targets!.Legal);
        Assert.Equal(new[] { f.Villain.ObjectId, kang.ObjectId }.Order(),
            Assert.Single(attacks, a => a.AnchorId == ally.ObjectId).Targets!.Legal.Order());
    }

    [Fact]
    [Rule("rr:cannot")]
    public void AFacedownCardWithAnotherProfileIsNotKang()
    {
        // A facedown Drone answers with its profile's characteristics, so a Kang card
        // engaged facedown as a Drone is not "Kang" and the holder may attack it.
        var (world, _) = KangTemporalFixture.Board(players: 1);
        world.CreateCard(MasterOfTime, world.Seats[0].Deck);
        var drone = FacedownMinions.EngageTop(world, 0, Marvel.Content.Tests.Cards.AuthoredCards.DroneProfile,
            "test", "Create_Drone", [])!;
        Obligation(world, FearOfKang, 0);

        Assert.Equal(MasterOfTime, drone.FaceId);
        Assert.Contains(drone, BasicPowers.Attackable(world, world.Facts, 0, world.Seats[0].IdentityCard));
    }

    [Fact]
    [Rule("rr:you-your.15")]
    public void TheHoldersAllyMayStillAttackKang()
    {
        // Ruling af0e9693cc645666: "the 'you' in 'Fear of Kang' only refers to the
        // identity, and you could attack Kang with allies." Jessica Jones (ATK 2)
        // attacks the Master of Time.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var kang = KangTemporalFixture.Engaged(world, MasterOfTime, 1);
        Obligation(world, FearOfKang, 1);
        var ally = world.CreateCard("01059", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));

        Assert.Contains(kang, BasicPowers.Attackable(world, world.Facts, 1, ally));
        AllyBasicPowers.AllyPower(world, world.Facts, ally, kang, BasicPowers.AttackVerb, []);
        f.Finish();

        Assert.Equal(2, kang.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:you-your.12")]
    public void AnAttackEventCannotChooseKangForTheHolder()
    {
        // Uppercut's attack is the identity's (rr:you-your.12): Rhino is offered as its
        // target and the Master of Time is not. Player 0 may choose either.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var kang = KangTemporalFixture.Engaged(world, MasterOfTime, 1);
        Obligation(world, FearOfKang, 1);
        var targets = new List<int[]>();
        for (int player = 0; player < 2; player++)
        {
            var hand = world.Seats[player].Hand;
            var uppercut = world.CreateCard("01054", hand);
            var genius = world.CreateCard("01089", hand);
            var energy = world.CreateCard("01088", hand);
            var action = Assert.Single(f.Runner.Actions(world, player), ability => ability.Card == uppercut.ObjectId);
            f.Runner.ExecuteAction(world, new PlayerAction(action, [genius.ObjectId, energy.ObjectId], []));
            f.Finish(prompt =>
            {
                targets.Add([.. prompt.Affordances.Select(option => option.AnchorId).Order()]);
                return Decision.Take(f.Villain.ObjectId);
            });
        }

        Assert.Equal([f.Villain.ObjectId, kang.ObjectId], targets[0]);
        Assert.Equal([f.Villain.ObjectId], targets[1]);
        Assert.Equal(10, f.Villain.Damage);
    }

    [Fact]
    [Rule("rr:cannot")]
    public void TheProhibitionEndsWhenTheObligationLeaves()
    {
        var (world, _) = KangTemporalFixture.Board(players: 1);
        var kang = KangTemporalFixture.Engaged(world, MasterOfTime, 0);
        var fear = Obligation(world, FearOfKang, 0);
        var hero = world.Seats[0].IdentityCard;
        Assert.DoesNotContain(kang, BasicPowers.Attackable(world, world.Facts, 0, hero));

        Discard.Card(world, fear, "test", []);

        Assert.Contains(kang, BasicPowers.Attackable(world, world.Facts, 0, hero));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [Rule("rr:obligation.6")]
    public void AlterEgoActionDiscardsARandomHandCardAndTheObligation(int handSize)
    {
        // 11049: "Alter-Ego Action: Discard a random card from your hand → discard this
        // obligation." The random pick draws on the seeded stream.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var fear = Obligation(world, FearOfKang, 1);
        AlterEgo(world, 1);
        var hand = Enumerable.Range(0, handSize).Select(_ => InHand(world, "01088", 1)).ToArray();
        long words = world.Random.Generator.WordsConsumed;

        Act(f, fear, 1);

        Assert.Equal(DeckType.EncounterDiscardPile, fear.Area.Type);
        Assert.Single(hand, card => card.Area.Type == DeckType.DiscardPile);
        Assert.Equal(handSize - 1, world.Seats[1].Hand.Cards.Count);
        Assert.True(world.Random.Generator.WordsConsumed > words);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [Rule("rr:obligation.6")]
    [Rule("rr:initiating-abilities.step.3")]
    public void AlterEgoActionNeedsTheHoldersAlterEgoAndAHandCard(bool hero, bool emptyHand, bool otherPlayer)
    {
        // The action is Alter-Ego only, its cost needs a card to discard, and only the
        // holder may trigger it (rr:obligation.6).
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var fear = Obligation(world, FearOfKang, 1);
        if (!hero) AlterEgo(world, 1);
        if (!emptyHand) InHand(world, "01088", 1);
        InHand(world, "01088", 0);
        AlterEgo(world, 0);

        Assert.False(Offers(f, fear, otherPlayer ? 0 : 1));
        Assert.Equal(otherPlayer, Offers(f, fear, 1));
    }
}
