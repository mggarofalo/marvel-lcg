using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Rhino's third stage, and the reveal nobody was asking for.
/// </summary>
/// <remarks>
/// <para>
/// <c>01096</c> is the expert scenario's last villain stage, and it is the
/// first card in the pool whose text is on a <i>villain stage the deck can
/// advance to</i> rather than on the stage setup dealt. That made it the card
/// that noticed a gap: <c>Defeat</c> moved the new stage into the villain's
/// play area and stopped there, so neither the keywords it prints nor its own
/// "When Revealed" ran.
/// </para>
/// <para>
/// <c>rr:when-revealed-abilities</c> is explicit about the case — "when a
/// player reveals a card from the encounter deck, a new scheme stage, <b>or a
/// new villain stage</b>, all 'When Revealed' abilities on the card resolve" —
/// and <c>rr:villain-defeat.1</c> adds that the reveal cannot be cancelled.
/// </para>
/// </remarks>
public sealed class RhinoThreeTests
{
    private const string Campaign = "rhino_expert";
    private const uint Seed = 12345;

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:form-change-form.5")]
    [Fact]
    public void RhinoThreeStunsEveryHero()
    {
        // "**When Revealed:** Stun each hero." Two players, both standing up,
        // and both take one -- which is what makes this a plural node rather
        // than a card naming one target.
        var world = Deal("spider_man", "spider_man");
        Forms.Change(world.Seats[0], Cards);
        Forms.Change(world.Seats[1], Cards);

        Reveal(world, AuthoredCards.RhinoThree);

        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.True(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Stunned));
    }

    [Rule("rr:form-change-form.5")]
    [Fact]
    public void APlayerInAlterEgoFormHasNoHeroToStun()
    {
        // "While a player is in alter-ego form, card abilities that interact
        // with their hero do not interact with their identity." So "each hero"
        // passes over the player who has flipped down, and the one still
        // standing takes the stun alone. A reading that stunned every identity
        // would punish the safer play, which is the opposite of what the card
        // does at a table.
        var world = Deal("spider_man", "spider_man");
        Forms.Change(world.Seats[0], Cards);

        Reveal(world, AuthoredCards.RhinoThree);

        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.False(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Stunned));
    }

    [Rule("rr:villain-defeat")]
    [Rule("rr:when-revealed-abilities")]
    [Rule("rr:toughness")]
    [Fact]
    public void AdvancingToRhinoThreeRunsItsKeywordAndItsText()
    {
        // The whole route, with the real cards: defeat the stage in play and
        // the next one arrives already tough and having stunned the table.
        // Nobody reveals it by hand -- `Defeat` is the only caller, which is
        // exactly what was missing.
        var world = Deal();
        Forms.Change(world.Seats[0], Cards);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var next = world.CreateCard(
            AuthoredCards.RhinoThree, world.AreaOf(DeckType.VillainDeck));
        world.Abilities = AuthoredCards.Runner();

        Agendas.Happening(world);

        Damage.Deal(
            world, Cards, villain, villain,
            Cards.PrintedValue(villain.FaceId, "HP", world.Players),
            "test", "Attack", []);

        Assert.Equal(DeckType.VillainArea, next.Area.Type);
        Assert.True(Statuses.Has(world, next, Statuses.Tough));
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
    }

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
