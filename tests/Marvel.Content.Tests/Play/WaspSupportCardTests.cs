using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows on synthetic boards; no public pack admission.
public sealed class WaspSupportCardTests : CoreCardTestBase
{
    private const string Whiplash = "01172";
    private const string Mercenary = "01101";

    private static long Value(World world, Card card, string field) =>
        StateFields.Modified(world, card, field, world.Facts, world.Players);

    [Theory]
    [InlineData("13001c", 1, 0, "GIANT")]
    [InlineData("13001a", 0, 1, "TINY")]
    [InlineData("13001b", 0, 0, null)]
    [Rule("rr:traits.1")]
    public void AntManFollowsTheIdentitysSizeForm(string face, int attack, int thwart, string? trait)
    {
        // rr:traits.1: "Traits have no inherent effects on the game."
        var world = WaspBoards.Board("13001b");
        var antMan = WaspBoards.InPlay(world, "13002", DeckType.AlliesArea);
        long baseAttack = Value(world, antMan, "attack");
        long baseThwart = Value(world, antMan, "thwart");
        world.Seats[0].IdentityCard.TurnTo(face);
        Assert.Equal(baseAttack + attack, Value(world, antMan, "attack"));
        Assert.Equal(baseThwart + thwart, Value(world, antMan, "thwart"));
        Assert.Equal(trait is not null, trait is not null && Traits.Has(world, antMan, trait, world.Facts));
    }

    [Theory]
    [InlineData("13001c", 0, 1)]
    [InlineData("13001a", 1, 0)]
    [Rule("rr:upgrade.1")]
    public void WaspsHelmetBoostsTheOtherSizePower(string face, int attack, int thwart)
    {
        // rr:upgrade.1: "An upgrade is active so long as it is in play, and it remains in play until a card ability causes it to leave play."
        var world = WaspBoards.Board(face);
        var wasp = world.Seats[0].IdentityCard;
        long baseAttack = Value(world, wasp, "attack");
        long baseThwart = Value(world, wasp, "thwart");
        WaspBoards.InPlay(world, "13010", DeckType.UpgradesArea);
        Assert.Equal(baseAttack + attack, Value(world, wasp, "attack"));
        Assert.Equal(baseThwart + thwart, Value(world, wasp, "thwart"));
    }

    [Theory]
    [InlineData("13001c", 1, false)]
    [InlineData("13001a", 0, true)]
    [InlineData("13001b", 0, false)]
    [Rule("rr:piercing")]
    [Rule("rr:retaliate-x.1")]
    public void RedRoomTrainingGrantsRetaliateOrBasicPiercingByForm(string face, long retaliate, bool piercing)
    {
        // rr:piercing: "An attack with the piercing keyword discards any tough status cards from the attacked character before dealing damage."
        // rr:retaliate-x.1: "The retaliate X keyword is equivalent to the following triggered ability: “Forced Response: After this character is attacked, deal X damage to the attacker.”"
        var world = WaspBoards.Board(face);
        var wasp = world.Seats[0].IdentityCard;
        WaspBoards.InPlay(world, "13008", DeckType.UpgradesArea);
        Assert.Equal(retaliate, Value(world, wasp, "retaliate"));
        Assert.Equal(piercing, AttackKeywords.Has(world, wasp, wasp, "piercing", basic: true));
        Assert.False(AttackKeywords.Has(world, wasp, wasp, "piercing", basic: false));
    }

    [Fact]
    [Rule("rr:piercing")]
    public void TinyBasicAttackWithRedRoomTrainingDiscardsTough()
    {
        // rr:piercing: "An attack with the piercing keyword discards any tough status cards from the attacked character before dealing damage."
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        WaspBoards.InPlay(world, "13008", DeckType.UpgradesArea);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, villain, Statuses.Tough);
        BasicPowerInitiation.BasicAttack(world, Cards, 0, villain, []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(0, Statuses.Count(world, villain, Statuses.Tough));
        Assert.Equal(1, villain.Damage);
    }

    [Theory]
    [InlineData("13001a", true)]
    [InlineData("13001c", false)]
    [Rule("rr:prevent.1")]
    public void BioSyntheticWingsPreventOneRetaliationDamageWhileTiny(string face, bool prevents)
    {
        // rr:prevent.1: "When damage is prevented, reduce the amount of damage the target takes (i.e. the amount of damage that is placed on the target)."
        // Whiplash's retaliate deals Wasp 1 damage after her basic attack.
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board(face);
        var identity = world.Seats[0].IdentityCard;
        var wings = WaspBoards.InPlay(world, "13009", DeckType.UpgradesArea);
        Assert.True(Traits.Has(world, identity, "AERIAL", world.Facts));
        var whiplash = WaspBoards.Engaged(world, Whiplash);
        BasicPowerInitiation.BasicAttack(world, Cards, 0, whiplash, []);
        while (Sequence.Work(world, Cards, runner, []) is { } prompt)
        {
            if (prompt.Affordances.SingleOrDefault(item => item.Verb == BasicPowerDivision.Verb) is { } divide)
            {
                // Giant divides its basic attack; every point goes to Whiplash.
                Sequence.Answer(world, Cards, runner, prompt, Decision.Take(divide.Id) with
                    { Targets = [.. Enumerable.Repeat(whiplash.ObjectId, divide.Targets!.Min)] }, []);
                continue;
            }
            var offer = prompt.Affordances.SingleOrDefault(item => item.AnchorId == wings.ObjectId);
            Assert.Equal(prevents, offer is not null);
            Sequence.Answer(world, Cards, runner, prompt, offer is null ? Decision.Decline : Decision.Take(offer.Id), []);
        }
        Assert.Equal(prevents ? 0 : 1, identity.Damage);
        Assert.Equal(!prevents, wings.Ready);
    }

    [Fact]
    [Rule("rr:traits.1")]
    public void BioSyntheticWingsGrantNoTraitToNadia()
    {
        // rr:traits.1: "Traits have no inherent effects on the game."
        var world = WaspBoards.Board("13001b");
        WaspBoards.InPlay(world, "13009", DeckType.UpgradesArea);
        Assert.False(Traits.Has(world, world.Seats[0].IdentityCard, "AERIAL", world.Facts));
    }

    [Fact]
    [Rule("rr:ally.1")]
    public void BootCampBoostsOnlyItsControllersAllies()
    {
        // rr:ally.1: "If an ally enters play, it remains in play until a card ability or game effect causes it to leave play."
        var world = WaspBoards.Board("13001a", players: 2);
        var mine = WaspBoards.InPlay(world, "01020", DeckType.AlliesArea);
        var theirs = WaspBoards.InPlay(world, "01068", DeckType.AlliesArea, player: 1);
        long mineBase = Value(world, mine, "attack");
        long theirsBase = Value(world, theirs, "attack");
        WaspBoards.InPlay(world, "13016", DeckType.SupportsArea, player: 1);
        Assert.Equal(mineBase, Value(world, mine, "attack"));
        Assert.Equal(theirsBase + 1, Value(world, theirs, "attack"));
    }

    [Fact]
    [Rule("rr:attachment.1")]
    public void BeetleArmorAddsFourHitPointsToItsHost()
    {
        // rr:attachment.1: "If an attachment attaches to a character, it may modify that character’s ATK, SCH, and/or THW values, as indicated by the values in the associated fields on the attachment card."
        var world = WaspBoards.Board("13001a");
        var beetle = WaspBoards.Engaged(world, "13028");
        long health = DamagePlacement.Health(world, world.Facts, beetle);
        world.CreateCard("13029", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, beetle.ObjectId));
        Assert.Equal(health + 4, DamagePlacement.Health(world, world.Facts, beetle));
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(2, 4)]
    [InlineData(3, 5)]
    [Rule("rr:thwart.1")]
    public void RunningInterferenceRemovesTwoPlusTheStageToThree(int stage, int removed)
    {
        // rr:thwart.1: "A hero or ally can use their basic thwart power to thwart a scheme."
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        var printed = world.TheCardIn(DeckType.VillainArea)!;
        World.MoveToTop(printed, world.AreaOf(DeckType.EncounterDiscardPile));
        world.CreateCard(stage switch { 1 => "01094", 2 => "01095", _ => "01096" }, world.AreaOf(DeckType.VillainArea));
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 8);
        var card = world.CreateCard("13031", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId], []));
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(8 - removed, main.Tokens["k_threat"]);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.1")]
    public void PinpointStrikeGainsADamageAndOverkillOnlyWhileTiny()
    {
        // rr:attack-player-ability-type.1: "A hero or ally can use their basic attack power to attack an enemy."
        foreach (var (face, expectedVillain) in new[] { ("13001a", 5L), ("13001c", 0L) })
        {
            var runner = AuthoredCards.Runner();
            var world = WaspBoards.Board(face);
            var minion = WaspBoards.Engaged(world, Mercenary);
            var villain = world.TheCardIn(DeckType.VillainArea)!;
            var card = world.CreateCard("13004", world.Seats[0].Hand);
            int[] payment = [world.CreateCard("01089", world.Seats[0].Hand).ObjectId,
                world.CreateCard("01089", world.Seats[0].Hand).ObjectId];
            var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
            runner.ExecuteAction(world, new PlayerAction(action, payment, []));
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
            Sequence.Answer(world, Cards, runner, prompt, Decision.Take(minion.ObjectId), []);
            while (Sequence.Work(world, Cards, runner, []) is { } response)
                Sequence.Answer(world, Cards, runner, response, Decision.Decline, []);
            Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
            // rr:overkill: 8 damage on a 3-hit-point minion spills 5 onto the villain.
            Assert.Equal(expectedVillain, villain.Damage);
        }
    }

    [Fact]
    [Rule("rr:response.1")]
    public void IronheartDrawsOneAfterBeingPlayedFromHand()
    {
        // rr:response.1: "Players can only trigger response abilities on cards they control or on encounter cards."
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        var seat = world.Seats[0];
        var ironheart = world.CreateCard("13018", seat.Hand);
        int[] payment = [world.CreateCard("01089", seat.Hand).ObjectId];
        CardPlay.Play(world, world.Facts, runner, seat, ironheart, payment, []);
        var response = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Sequence.Answer(world, Cards, runner, response,
            Decision.Take(Assert.Single(response.Affordances, item => item.AnchorId == ironheart.ObjectId).Id), []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Single(seat.Hand.Cards);
        Assert.Equal(DeckType.AlliesArea, ironheart.Area.Type);
    }

    [Theory]
    [InlineData(0, "thwart")]
    [InlineData(1, "attack")]
    [Rule("rr:response.1")]
    public void MilesMoralesGainsTwoInTheChosenPower(int option, string field)
    {
        // rr:response.1: "Players can only trigger response abilities on cards they control or on encounter cards."
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        var seat = world.Seats[0];
        var miles = world.CreateCard("13019", seat.Hand);
        int[] payment = [world.CreateCard("01090", seat.Hand).ObjectId, world.CreateCard("01089", seat.Hand).ObjectId];
        CardPlay.Play(world, world.Facts, runner, seat, miles, payment, []);
        var response = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Sequence.Answer(world, Cards, runner, response,
            Decision.Take(Assert.Single(response.Affordances, item => item.AnchorId == miles.ObjectId).Id), []);
        long before = Value(world, miles, field);
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Sequence.Answer(world, Cards, runner, choice, Decision.Take(choice.Affordances[option].Id), []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(before + 2, Value(world, miles, field));
    }
}
