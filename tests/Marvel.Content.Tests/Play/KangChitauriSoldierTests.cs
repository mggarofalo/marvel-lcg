using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual Chitauri Soldier and the full authored book on a nonpublic Rhino board;
// Kang's public admission stays closed.
public sealed class KangChitauriSoldierTests : CoreCardTestBase
{
    [Theory]
    [InlineData("01104", 0)] // No boost field.
    [InlineData("11030", 0)] // A boost star and no boost icons.
    [InlineData("01101", 1)]
    [InlineData("11032", 3)]
    [Rule("rr:indirect-damage.1")]
    [Rule("rr:boost-boost-icon.1")]
    public void ChitauriSoldierDiscardsTheTopCardAndDealsItsBoostIconsAsIndirectDamage(string top, int icons)
    {
        // 11031: "When Chitauri Soldier attacks you, discard the top card of the
        // encounter deck → take indirect damage equal to the number of boost icons
        // on that card." rr:boost-boost-icon.1: "A star icon is not itself
        // considered a boost icon." Player 1 is not the first player.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var soldier = KangTemporalFixture.Engaged(world, "11031", 1);
        var card = world.CreateCard(top, world.AreaOf(DeckType.EncounterDeck));
        int deck = world.AreaOf(DeckType.EncounterDeck).Cards.Count;
        KangTemporalFixture.Attack(world, soldier, 1);

        var events = f.Finish();

        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        Assert.Equal(deck - 1, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Equal(1 + icons, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.False(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Stunned));
        Assert.Single(events.OfType<AttackCompleted>(), e => e.Enemy == soldier.ObjectId);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void ChitauriSoldierIgnoresAnotherEnemysAttack()
    {
        // 11031 triggers only "When Chitauri Soldier attacks you."
        var (world, f) = KangTemporalFixture.Board(players: 1);
        KangTemporalFixture.Engaged(world, "11031", 0);
        var mercenary = KangTemporalFixture.Engaged(world, "01101", 0);
        var top = world.CreateCard("11032", world.AreaOf(DeckType.EncounterDeck));
        KangTemporalFixture.Attack(world, mercenary, 0);

        f.Finish();

        Assert.Equal(DeckType.EncounterDeck, top.Area.Type);
        Assert.Same(top, world.AreaOf(DeckType.EncounterDeck).Cards[^1]);
        Assert.Empty(world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:encounter-deck.1")]
    [Rule("rr:encounter-deck.2")]
    public void ChitauriSoldierEmptyingTheEncounterDeckStillCountsTheDiscardedCard()
    {
        // rr:encounter-deck.1: "If the encounter deck is empty, the encounter discard
        // pile is immediately shuffled to create a new encounter deck. When this
        // occurs, place an acceleration token next to the main scheme deck."
        // rr:encounter-deck.2: "If the encounter deck is emptied this way, that card
        // ability is considered to be fulfilled."
        var (world, f) = KangTemporalFixture.Board(players: 1, encounter: 0);
        var soldier = KangTemporalFixture.Engaged(world, "11031", 0);
        var last = world.CreateCard("11032", world.AreaOf(DeckType.EncounterDeck));
        var discarded = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDiscardPile));
        KangTemporalFixture.Attack(world, soldier, 0);

        f.Finish();

        Assert.Equal(4, world.Seats[0].IdentityCard.Damage);
        Assert.Empty(world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.Equal(new[] { discarded.ObjectId, last.ObjectId }.Order(),
            world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.ObjectId).Order());
        Assert.Equal(1, f.Main.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:interrupt")]
    [Rule("rr:indirect-damage.3.2")]
    [Rule("rr:tough.1")]
    public void ChitauriSoldierIndirectDamageSpendsToughBeforeTheAttackLands()
    {
        // rr:interrupt: an interrupt "resolves immediately before that triggering
        // condition resolves", so 11031's "When Chitauri Soldier attacks you" damage
        // precedes the attack's own damage. rr:indirect-damage.3.2: "A character
        // with a tough status card can be assigned indirect damage up to its remaining
        // hit points, and all damage assigned to it is prevented by its tough status
        // card." rr:tough.1 then leaves nothing to stop the attack's 1 damage.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        var identity = world.Seats[0].IdentityCard;
        var soldier = KangTemporalFixture.Engaged(world, "11031", 0);
        var top = world.CreateCard("11032", world.AreaOf(DeckType.EncounterDeck));
        Statuses.Give(world, identity, Statuses.Tough);
        KangTemporalFixture.Attack(world, soldier, 0);

        f.Finish();

        Assert.Equal(DeckType.EncounterDiscardPile, top.Area.Type);
        Assert.False(Statuses.Has(world, identity, Statuses.Tough));
        Assert.Equal(1, identity.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:indirect-damage.1")]
    public void ChitauriSoldierIndirectDamageIsDividedAmongTheAttackedPlayersCharacters()
    {
        // rr:indirect-damage.1: "Indirect damage dealt to a player can be divided as
        // that player chooses among characters under their control." The player puts
        // 2 of the 3 on Jessica Jones (3 hit points) and 1 on their identity; the
        // undefended attack then deals its 1 to the identity.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        var identity = world.Seats[0].IdentityCard;
        var ally = world.CreateCard("01059", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var soldier = KangTemporalFixture.Engaged(world, "11031", 0);
        world.CreateCard("11032", world.AreaOf(DeckType.EncounterDeck));
        KangTemporalFixture.Attack(world, soldier, 0);
        int assignments = 0;

        f.Finish(prompt =>
        {
            var option = Assert.Single(prompt.Affordances, a => a.Targets is { AllowRepeated: true });
            Assert.Equal(new[] { identity.ObjectId, ally.ObjectId }.Order(), option.Targets!.Legal.Order());
            assignments++;
            return new Decision(option.Id, [ally.ObjectId, ally.ObjectId, identity.ObjectId]);
        });

        Assert.Equal(1, assignments);
        Assert.Equal(2, ally.Damage);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(2, identity.Damage);
        Assert.False(world.Agenda.IsBusy);
    }
}
