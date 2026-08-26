using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// A generator that is not a card in hand — <c>rr:resource-ability</c>.
/// </summary>
/// <remarks>
/// <para>
/// Peter Parker prints "Scientist — <b>Resource</b>: generate a [mental]
/// resource. <i>(Limit once per round.)</i>", and the recorded prompt has been
/// carrying it all along: it lists <b>six</b> generators for a hand of six cards
/// one of which is being played. A list built from hand cards alone was short by
/// exactly that one, which is why <c>PlayerPhaseTests</c> could not assert the
/// generators at all.
/// </para>
/// <para>
/// <c>rr:resource-ability.1</c> is why it sits beside the cards in hand rather
/// than in a window: one "can be triggered <b>anytime the player who controls
/// the ability is generating resources to pay a cost</b>" — another way to make
/// a resource, not another moment.
/// </para>
/// </remarks>
public sealed class ResourceAbilityTests
{
    /// <summary>
    /// `01006` Aunt May — a support costing one. An <i>event</i> would not do:
    /// `rr:player-turn.2` does not list one, so `CardPlay.Price` refuses to
    /// offer it and there would be no cost to pay.
    /// </summary>
    private const string Cheap = "01006";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:resource-ability.1")]
    [Fact]
    public void ItIsOfferedBesideTheCardsInHand()
    {
        var world = Deal();
        Empty(world);
        var card = world.CreateCard(Cheap, world.Seats[0].Hand);

        var price = CardPlay.Price(world, Cards, world.Seats[0], card);

        // The hand holds nothing but the card being played, so every generator
        // here is an ability. It is the identity's, and it makes a mental.
        var source = Assert.Single(price!.Sources!);
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, source.Effect);
        Assert.Equal("B", source.Generates);
    }

    [Rule("rr:cost.3")]
    [Fact]
    public void UsingOneDoesNotDiscardTheCardItIsOn()
    {
        // `rr:cost.3` spends resources "by discarding cards from their hand",
        // which is the *other* way to make one. An identity is not discardable
        // at all — `rr:identity.3` — so a resource ability that worked by
        // discarding could not exist.
        var world = Deal();
        Empty(world);
        var card = world.CreateCard(Cheap, world.Seats[0].Hand);
        var identity = world.Seats[0].IdentityCard;

        Pay(world, card, identity.ObjectId);

        Assert.Equal(DeckType.HeroArea, identity.Area.Type);
        Assert.Equal(DeckType.SupportsArea, card.Area.Type);
    }

    [Rule("rr:limit")]
    [Fact]
    public void OnceARoundMeansOnceARound()
    {
        // "Each copy of an ability with such a limit may be used X times per
        // the specified period, **per instance of that ability**." One use, and
        // the second play of the round is not offered it.
        var world = Deal();
        Empty(world);
        var first = world.CreateCard(Cheap, world.Seats[0].Hand);
        var second = world.CreateCard(Cheap, world.Seats[0].Hand);

        Pay(world, first, world.Seats[0].IdentityCard.ObjectId);

        // Nothing left to pay with, so the second card is not offered at all.
        Assert.Null(CardPlay.Price(world, Cards, world.Seats[0], second));
    }

    [Rule("rr:limit")]
    [Fact]
    public void TheNextRoundGivesItBack()
    {
        // A round, not a game. The use is kept as a lasting effect rather than
        // a token — a card's tokens are the digest's fields, and counting uses
        // there would put a number in every recorded board — so ending the
        // round clears it without anything having to remember.
        var world = Deal();
        Empty(world);
        var first = world.CreateCard(Cheap, world.Seats[0].Hand);
        var second = world.CreateCard(Cheap, world.Seats[0].Hand);

        Pay(world, first, world.Seats[0].IdentityCard.ObjectId);
        Assert.Null(CardPlay.Price(world, Cards, world.Seats[0], second));

        PhaseEnd.EndVillainPhase(world, Cards, []);

        Assert.NotNull(CardPlay.Price(world, Cards, world.Seats[0], second));
    }

    [Fact]
    public void TheUseIsNotOnTheWire()
    {
        // Stated directly, because it is the reason the count is a lasting
        // effect: using the ability must not change the digest beyond what the
        // payment itself did.
        var world = Deal();
        Empty(world);
        var card = world.CreateCard(Cheap, world.Seats[0].Hand);
        var identity = world.Seats[0].IdentityCard;

        Pay(world, card, identity.ObjectId);

        Assert.DoesNotContain(
            identity.Tokens.Keys, key => key.Contains("used", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            identity.Tokens.Keys, key => key.Contains("spent", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Plays a card, paying with the ability, and ignores what it does on
    /// landing.
    /// </summary>
    /// <remarks>
    /// The card entering play is not what these tests are about, so the *play*
    /// is given `NoCardAbilities` while the board keeps the real runner — which
    /// is what `CardPlay.Spend` reaches for when a generator is an ability
    /// rather than a card.
    /// </remarks>
    private static void Pay(World world, Card card, int with) => CardPlay.Play(
        world, Cards, new NoCardAbilities(), world.Seats[0], card, [with], []);

    private static void Empty(World world)
    {
        foreach (var card in world.Seats[0].Hand.Cards.ToList())
        {
            World.MoveToTop(card, world.Seats[0].Deck);
        }
    }

    private static World Deal()
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        world.Abilities = AuthoredCards.Runner();
        return world;
    }
}
