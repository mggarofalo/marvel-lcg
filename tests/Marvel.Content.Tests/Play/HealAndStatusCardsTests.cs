using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Two cards that read what they just did, and one that acts on a player.
/// </summary>
/// <remarks>
/// <para>
/// <b>"If no damage was healed this way" cannot be answered in advance.</b>
/// <c>rr:heal</c> heals <i>up to</i> the amount, so a villain at full health
/// heals nothing and one damaged by less than four heals less than four. A card
/// that checked the villain's health before healing would read a number the
/// heal never reaches, which is why <c>Damage.Heal</c> now answers with what it
/// actually moved and the card reads <c>result.healed</c>.
/// </para>
/// <para>
/// <b>"You are confused" puts a status on a card.</b> <c>rr:you-your</c> is
/// emphatic — if "you" <i>can</i> resolve as the player's identity it
/// <i>must</i> — and <c>.5</c> spells this exact case out.
/// </para>
/// </remarks>
public sealed class HealAndStatusCardsTests
{
    private const string Campaign = "rhino";
    private const uint Seed = 12345;

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:heal")]
    [Fact]
    public void HardToKeepDownHealsTheVillainAndDoesNotSurge()
    {
        // Four damage on the villain, four healed, and no surge: the card's
        // condition is about what the heal did, and it did something.
        var world = Deal();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        villain.TakeDamage(4);
        int queued = Queue(world).Cards.Count;

        Reveal(world, AuthoredCards.HardToKeepDown);

        Assert.Equal(0, villain.Damage);
        Assert.Equal(queued, Queue(world).Cards.Count);
    }

    [Rule("rr:heal")]
    [Fact]
    public void AVillainAtFullHealthHealsNothingAndTheCardSurges()
    {
        // The branch the whole `result` mechanism exists for. Nothing to heal,
        // so nothing was healed, so the card surges -- and `rr:surge.1` makes
        // that "deal yourself 1 facedown encounter card".
        var world = Deal();
        int queued = Queue(world).Cards.Count;

        Reveal(world, AuthoredCards.HardToKeepDown);

        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(queued + 1, Queue(world).Cards.Count);
    }

    [Rule("rr:heal")]
    [Fact]
    public void HealingLessThanTheAmountAskedForStillCountsAsHealing()
    {
        // One damage and a heal of four. The villain heals one, and "no damage
        // was healed this way" is false -- so no surge. The reading that
        // compared the amount asked for with the amount moved would surge here,
        // and the reading that checked health beforehand would be right by
        // accident on this board and wrong on the one above.
        var world = Deal();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        villain.TakeDamage(1);
        int queued = Queue(world).Cards.Count;

        Reveal(world, AuthoredCards.HardToKeepDown);

        Assert.Equal(0, villain.Damage);
        Assert.Equal(queued, Queue(world).Cards.Count);
    }

    [Fact]
    public void NoVillainMeansNoDamageHealedAndTheCardSurges()
    {
        // "Rhino heals 4 damage. If no damage was healed this way, this card
        // gains surge" has an answer for a board with no Rhino on it, and the
        // answer is the surge. So the node records nothing healed rather than
        // throwing at a card that is not there -- which is the opposite of what
        // `giveStatus` does, because the sentences are different.
        var world = Deal();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        World.MoveToTop(villain, world.AreaOf(DeckType.VillainDeck));
        int queued = Queue(world).Cards.Count;

        Reveal(world, AuthoredCards.HardToKeepDown);

        Assert.Equal(queued + 1, Queue(world).Cards.Count);
    }

    [Rule("rr:you-your.5")]
    [Rule("rr:confuse-confused.2")]
    [Fact]
    public void FalseAlarmConfusesTheRevealingPlayersIdentity()
    {
        // "If an ability 'confuses' a character, give that character a confused
        // status card." Here "You are confused" puts that status on the
        // identity of the player resolving the card -- not on the first player,
        // and not on a card the ability happens to be looking at. Revealed by
        // the *second* player so that "the revealing player" is a claim.
        var world = Deal("spider_man", "she_hulk");

        Reveal(world, AuthoredCards.FalseAlarm, player: 1);

        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
        Assert.True(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Confused));
    }

    [Rule("rr:status-cards.1")]
    [Fact]
    public void FalseAlarmSurgesWhenTheIdentityIsAlreadyConfused()
    {
        // "If you are already confused, this card gains surge." The two
        // branches are exclusive in fact as well as in the sentence:
        // `rr:status-cards.1` caps a character at one card of each type, so a
        // second confused card could not have landed anyway.
        var world = Deal();
        var identity = world.Seats[0].IdentityCard;
        Statuses.Give(world, identity, Statuses.Confused);
        int queued = Queue(world).Cards.Count;

        Reveal(world, AuthoredCards.FalseAlarm);

        Assert.Equal(1, Statuses.Count(world, identity, Statuses.Confused));
        Assert.Equal(queued + 1, Queue(world).Cards.Count);
    }

    private static Area Queue(World world, int player = 0) =>
        world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player));

    private static void Reveal(World world, string faceId, int player = 0)
    {
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
        AuthoredCards.Runner().WhenRevealed(world, card, player);
    }

    private static World Deal(params string[] heroes)
    {
        string[] playing = heroes.Length > 0 ? heroes : ["spider_man"];
        return WorldSetup.DealWithoutCardAbilities(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, playing), Cards),
            [.. playing.Select(hero => Setup.Hero(hero).Name)],
            Seed);
    }
}
