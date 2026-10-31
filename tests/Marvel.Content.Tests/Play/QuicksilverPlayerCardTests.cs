using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows for the Protection, Basic and extra cards on synthetic boards.
public sealed class QuicksilverPlayerCardTests : CoreCardTestBase
{
    private const string Energy = "01088";
    private const string Genius = "01089";
    private const string Strength = "01090";

    [Fact]
    [Rule("rr:play-put-into-play")]
    [Rule("rr:search")]
    public void MultipleManPutsACopyIntoPlayFromTheDeckAndShuffles()
    {
        // rr:play-put-into-play: putting a card into play is not playing it, so no
        // cost is paid. rr:search: the searched deck is shuffled afterwards.
        var world = QuicksilverBoards.Board();
        var first = QuicksilverBoards.Hand(world, "14012");
        var copy = world.CreateCard("14012", world.Seats[0].Deck);
        var paying = new[] { QuicksilverBoards.Hand(world, Genius), QuicksilverBoards.Hand(world, Genius) };

        CardPlay.Play(world, world.Facts, QuicksilverBoards.Runner(world), world.Seats[0], first, [.. paying.Select(card => card.ObjectId)], []);
        QuicksilverBoards.Drive(world, prompt => prompt.Affordances.FirstOrDefault(a => a.AnchorId == first.ObjectId) is { } response
            ? Decision.Take(response.Id)
            : prompt.Affordances.FirstOrDefault(a => a.Id == copy.ObjectId) is { } chosen ? Decision.Take(chosen.Id) : null);

        Assert.Equal(DeckType.AlliesArea, first.Area.Type);
        Assert.Equal(DeckType.AlliesArea, copy.Area.Type);
        Assert.Equal(12, world.Seats[0].Deck.Cards.Count);
    }

    [Fact]
    [Rule("rr:heal")]
    public void WarlockSpendsAMentalResourceToHealHimself()
    {
        // rr:heal: healing removes damage. 14013: "Action: Spend a mental resource →
        // heal up to 2 damage from Warlock."
        var world = QuicksilverBoards.Board();
        var warlock = QuicksilverBoards.InPlay(world, "14013", DeckType.AlliesArea);
        Assert.DoesNotContain(QuicksilverBoards.Runner(world).Actions(world, 0), a => a.Card == warlock.ObjectId);
        warlock.TakeDamage(2);
        var genius = QuicksilverBoards.Hand(world, Genius);

        QuicksilverBoards.Play(world, warlock, [genius]);
        QuicksilverBoards.Drive(world, _ => null);

        Assert.Equal(0, warlock.Damage);
        Assert.Equal(DeckType.DiscardPile, genius.Area.Type);
    }

    [Theory]
    [InlineData(Energy, 1)]
    [InlineData(Genius, 0)]
    [Rule("rr:prevent.1")]
    [Rule("rr:defend-defense.4.8")]
    public void SideStepPreventsThreeAndPaidWithEnergyDamagesTheAttacker(string payment, int villainDamage)
    {
        // rr:prevent.1: "When damage is prevented, reduce the amount of damage the
        // target takes". rr:defend-defense.4.8 lets a defense ability answer damage.
        // Rhino's undefended attack deals 2; Side Step prevents all of it and, paid
        // with energy, deals 1 damage to Rhino.
        var world = QuicksilverBoards.Board();
        var quicksilver = world.Seats[0].IdentityCard;
        var villain = QuicksilverBoards.Villain(world);
        var step = QuicksilverBoards.Hand(world, "14015");
        var resource = QuicksilverBoards.Hand(world, payment);
        KangTemporalFixture.Attack(world, villain, 0);

        QuicksilverBoards.Drive(world, prompt => prompt.Affordances.FirstOrDefault(a => a.AnchorId == step.ObjectId) is { } play
            ? Decision.Take(play.Id, [], [resource.ObjectId]) : null);

        Assert.Equal(DeckType.DiscardPile, step.Area.Type);
        Assert.Equal(0, quicksilver.Damage);
        Assert.Equal(villainDamage, villain.Damage);
    }

    [Fact]
    [Rule("rr:upgrade.1")]
    public void ArmoredVestReprintGivesTheHeroOneDefense()
    {
        // rr:upgrade.1: "An upgrade is active so long as it is in play".
        var world = QuicksilverBoards.Board();
        var quicksilver = world.Seats[0].IdentityCard;
        long before = Modified(world, quicksilver, "defense");
        QuicksilverBoards.InPlay(world, "14016", DeckType.UpgradesArea);
        Assert.Equal(before + 1, Modified(world, quicksilver, "defense"));
    }

    [Theory]
    [InlineData("14017", "14015", true)]
    [InlineData("14017", "14003", false)]
    [InlineData("14030", "01060", true)]
    [InlineData("14030", "14032", false)]
    [Rule("rr:resource-ability.1.1")]
    public void RestrictedResourcesPayOnlyForTheirEventTrait(string upgrade, string face, bool allowed)
    {
        // rr:resource-ability.1.1: a restricted resource can only pay the named kind
        // of cost. Side Step is a Defense event and For Justice! a Thwart event.
        var world = QuicksilverBoards.Board();
        var runner = QuicksilverBoards.Runner(world);
        var source = QuicksilverBoards.InPlay(world, upgrade, DeckType.UpgradesArea);
        var card = QuicksilverBoards.Hand(world, face);
        Assert.Equal(allowed, CardPayment.Generators(world, world.Facts, world.Seats[0], runner, card)
            .Any(generator => generator.Effect == source.ObjectId));
    }

    [Theory]
    [InlineData("14022", "attack")]
    [InlineData("14023", "thwart")]
    [Rule("rr:lasting-effects")]
    public void DiscardingTheUpgradeGivesOneUntilThePhaseEnds(string upgrade, string field)
    {
        // rr:lasting-effects: the modifier lasts until the end of the phase.
        var world = QuicksilverBoards.Board();
        var quicksilver = world.Seats[0].IdentityCard;
        var card = QuicksilverBoards.InPlay(world, upgrade, DeckType.UpgradesArea);
        long before = Modified(world, quicksilver, field);

        QuicksilverBoards.Play(world, card, []);
        QuicksilverBoards.Drive(world, _ => null);

        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(before + 1, Modified(world, quicksilver, field));
    }

    [Fact]
    [Rule("rr:piercing")]
    public void BruteForcePiercesOnBasicAttackThenDiscardsItself()
    {
        // rr:piercing: "An attack with the piercing keyword discards any tough status
        // cards from the attacked character before dealing damage." 14029: "Forced
        // Response: After you make a basic attack, discard Brute Force."
        var world = QuicksilverBoards.Board();
        var villain = QuicksilverBoards.Villain(world);
        var brute = QuicksilverBoards.InPlay(world, "14029", DeckType.UpgradesArea);
        Statuses.Give(world, villain, Statuses.Tough);
        Assert.Equal(2, Modified(world, world.Seats[0].IdentityCard, "attack"));

        BasicPowers.BasicAttack(world, Cards, 0, villain, []);
        QuicksilverBoards.Drive(world, _ => null);

        Assert.False(Statuses.Has(world, villain, Statuses.Tough));
        Assert.Equal(2, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, brute.Area.Type);
    }

    [Fact]
    [Rule("rr:damage")]
    public void BeatEmUpDamagesTheVillainAndEachEngagedMinion()
    {
        // 14032: "Hero Action: Deal 1 damage to the villain and each minion engaged with you."
        var world = QuicksilverBoards.Board(players: 2);
        var villain = QuicksilverBoards.Villain(world);
        var mine = QuicksilverBoards.Engaged(world, "01101");
        var theirs = QuicksilverBoards.Engaged(world, "01101", player: 1);
        var beat = QuicksilverBoards.Hand(world, "14032");

        QuicksilverBoards.Play(world, beat, [QuicksilverBoards.Hand(world, Energy)]);
        QuicksilverBoards.Drive(world, _ => null);

        Assert.Equal(1, villain.Damage);
        Assert.Equal(1, mine.Damage);
        Assert.Equal(0, theirs.Damage);
    }

    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 0)]
    [Rule("rr:team-up")]
    [Rule("rr:cancel")]
    public void OrderAndChaosCancelsATreacheryAndDamagesTheVillainWithWanda(bool wanda, int damage)
    {
        // rr:team-up: "a card with the team-up keyword cannot be played unless both of
        // the named friendly characters (identity or ally) are in play." rr:cancel: the
        // When Revealed effects of the treachery do not resolve. The Scarlet Witch
        // ally names the second character. "I'm Tough" (01105) would toughen Rhino.
        var world = QuicksilverBoards.Board();
        var villain = QuicksilverBoards.Villain(world);
        if (wanda) QuicksilverBoards.InPlay(world, "14002", DeckType.AlliesArea);
        var order = QuicksilverBoards.Hand(world, "14018");
        var payment = QuicksilverBoards.Hand(world, Strength);
        var treachery = world.CreateCard("01105", world.AreaOf(DeckType.EncounterDeck));
        Deal.EncounterCard(world, treachery, 0, "Order and Chaos test deal", []);
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCards), 1, 4, Plan: true));

        QuicksilverBoards.Drive(world, prompt => prompt.Affordances.FirstOrDefault(a => a.AnchorId == order.ObjectId) is { } play
            ? Decision.Take(play.Id, [], [payment.ObjectId]) : null);

        Assert.Equal(damage, villain.Damage);
        Assert.Equal(!wanda, Statuses.Has(world, villain, Statuses.Tough));
        Assert.Equal(wanda ? DeckType.DiscardPile : DeckType.HandsArea, order.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, treachery.Area.Type);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, false)]
    [Rule("rr:defend-defense.4.8")]
    [Rule("rr:stun-stunned")]
    public void NeverBackDownAddsTwoDefenseAndStunsTheAttackerOfAnUndamagedHero(int bonus, bool stunned)
    {
        // 14014: "When you defend against an attack, you get +2 DEF for this attack.
        // If you take no damage from this attack, stun the attacking enemy." Rhino's
        // ATK 2 against DEF 3 deals nothing; with +2 ATK it deals 1.
        var world = QuicksilverBoards.Board();
        var quicksilver = world.Seats[0].IdentityCard;
        var villain = QuicksilverBoards.Villain(world);
        if (bonus > 0)
            world.Effects.Register(new(EffectSource.LastingEffect, "attack", Amount: bonus, Affects: villain.ObjectId));
        var never = QuicksilverBoards.Hand(world, "14014");
        var payment = QuicksilverBoards.Hand(world, Energy);
        KangTemporalFixture.Attack(world, villain, 0);

        QuicksilverBoards.Drive(world, prompt =>
            prompt.Affordances.SingleOrDefault(a => a.Verb == "Defense") is { } defense ? Decision.Take(defense.Id)
            : prompt.Affordances.FirstOrDefault(a => a.AnchorId == never.ObjectId) is { } play
                ? Decision.Take(play.Id, [], [payment.ObjectId]) : null);

        Assert.Equal(DeckType.DiscardPile, never.Area.Type);
        Assert.Equal(stunned ? 0 : 1, quicksilver.Damage);
        Assert.Equal(stunned, Statuses.Has(world, villain, Statuses.Stunned));
    }

    [Theory]
    [InlineData("01102", true, 3)]
    [InlineData("01104", true, 1)]
    [InlineData("01102", false, 1)]
    [Rule("rr:basic-power.1.1")]
    [Rule("rr:boost-boost-icon")]
    public void ScarletWitchAddsTheDiscardedBoostIconsToHerBasicAttack(string top, bool use, int damage)
    {
        // 14002: "When you use one of Scarlet Witch's basic powers, discard the top card
        // of the encounter deck. For each boost icon discarded this way, Scarlet Witch
        // gets +1 to that power for this use." Sandman (01102) prints two boost icons;
        // Hard to Keep Down (01104) prints none. rr:boost-boost-icon: "Boost icons are
        // located at the bottom-right of the card."
        var world = QuicksilverBoards.Board(encounter: top);
        var villain = QuicksilverBoards.Villain(world);
        var wanda = QuicksilverBoards.InPlay(world, "14002", DeckType.AlliesArea);
        var offered = new List<int>();

        AllyBasicPowers.AllyPower(world, world.Facts, wanda, villain, BasicPowers.AttackVerb, []);
        QuicksilverBoards.Drive(world, prompt => use ? QuicksilverBoards.Take(wanda, offered)(prompt) : null);

        Assert.Equal(damage, villain.Damage);
        Assert.Equal(use ? 1 : 0, world.AreaOf(DeckType.EncounterDiscardPile).Cards.Count);
    }

    [Fact]
    [Rule("rr:basic-power.1.2")]
    public void ScarletWitchAddsTheDiscardedBoostIconsToHerBasicThwart()
    {
        // The bonus is to "that power for this use": THW 1 plus Sandman's two icons.
        var world = QuicksilverBoards.Board(encounter: "01102");
        var main = QuicksilverBoards.MainScheme(world);
        main.PlaceTokens("k_threat", 5);
        var wanda = QuicksilverBoards.InPlay(world, "14002", DeckType.AlliesArea);

        AllyBasicPowers.AllyPower(world, world.Facts, wanda, main, BasicPowers.ThwartVerb, []);
        QuicksilverBoards.Drive(world, QuicksilverBoards.Take(wanda));

        Assert.Equal(2, main.Tokens["k_threat"]);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 2)]
    [Rule("rr:heal")]
    [Rule("rr:choose-game-element.3.1")]
    public void UnitedWeStandHealsUpToTheStageNumberOfDamagedCharacters(int stage, int healed)
    {
        // 14031: "Heal 1 damage from up to X friendly characters (to a maximum of 3
        // characters), where X is equal to the villain's stage number." Rhino I is
        // stage 1 and Rhino III stage 3; Quicksilver and Warlock are damaged.
        var world = QuicksilverBoards.Board(villain: stage == 3 ? "01096" : "01094");
        var quicksilver = world.Seats[0].IdentityCard;
        var warlock = QuicksilverBoards.InPlay(world, "14013", DeckType.AlliesArea);
        quicksilver.TakeDamage(2);
        warlock.TakeDamage(2);
        var stand = QuicksilverBoards.Hand(world, "14031");

        QuicksilverBoards.Play(world, stand, []);
        QuicksilverBoards.Drive(world, prompt => prompt.Affordances.FirstOrDefault(a => a.Targets is not null) is { } choose
            ? Decision.Take(choose.Id) with { Targets = [.. choose.Targets!.Legal.Take(choose.Targets.Max)] } : null);

        Assert.Equal(4 - healed, quicksilver.Damage + warlock.Damage);
        Assert.Equal(DeckType.DiscardPile, stand.Area.Type);
    }

    [Fact]
    [Rule("rr:heal")]
    [Rule("rr:initiating-abilities.step.2")]
    public void UnitedWeStandNeedsADamagedFriendlyCharacter()
    {
        // rr:heal: "damage the character has sustained can be removed", so only a
        // damaged character is a valid recipient. rr:initiating-abilities.step.2: "If
        // the card or ability does not have at least one valid target, it cannot be
        // played or initiated."
        var world = QuicksilverBoards.Board();
        var stand = QuicksilverBoards.Hand(world, "14031");
        Assert.DoesNotContain(QuicksilverBoards.Runner(world).Actions(world, 0), a => a.Card == stand.ObjectId);
        world.Seats[0].IdentityCard.TakeDamage(1);
        Assert.Contains(QuicksilverBoards.Runner(world).Actions(world, 0), a => a.Card == stand.ObjectId);
    }
}
