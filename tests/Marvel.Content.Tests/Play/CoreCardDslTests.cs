using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreCardDslTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:ready")]
    [Fact]
    public void ArcReactorExhaustsAndReadiesIronMan()
    {
        // "Exhaust Arc Reactor → ready Iron Man." Readying changes only the
        // exhausted state, and the exhaust before the arrow is paid first.
        var world = Hero("01029a");
        var reactor = world.CreateCard(
            "01035", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.Seats[0].IdentityCard.Exhaust();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        var action = Assert.Single(runner.Actions(world, 0), action => action.Card == reactor.ObjectId);
        runner.Act(world, action, [], []);

        Assert.False(reactor.Ready);
        Assert.True(world.Seats[0].IdentityCard.Ready);
    }

    [Rule("rr:modifiers")]
    [Fact]
    public void JessicaJonesCountsTheSideSchemesCurrentlyInPlay()
    {
        // "+1 THW for each side scheme in play" is continuous: adding and
        // removing a scheme changes the modifier without replaying the ally.
        var world = Hero("01029a");
        var jessica = world.CreateCard(
            "01059", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.Abilities = AuthoredCards.Runner();

        Assert.Equal(1, Modified(world, jessica, "thwart"));

        var scheme = world.CreateCard("01138", world.AreaOf(DeckType.SideSchemesArea));
        Assert.Equal(2, Modified(world, jessica, "thwart"));

        World.MoveToTop(scheme, world.AreaOf(DeckType.EncounterDiscardPile));
        Assert.Equal(1, Modified(world, jessica, "thwart"));
    }

    [Rule("rr:traits")]
    [Rule("rr:modifiers")]
    [Fact]
    public void IronManCountsPrintedAndGrantedTechTraitsAndCapsAtSeven()
    {
        // "For each Tech upgrade you control" reads the current trait, not
        // only the ink. The constant hand-size effect therefore settles after
        // a lasting effect grants TECH to another controlled upgrade.
        var world = Hero("01029a");
        world.Abilities = AuthoredCards.Runner();
        var upgrades = world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0);
        world.CreateCard("01036", upgrades); // printed TECH
        var granted = world.CreateCard("01093", upgrades); // no printed TECH
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: Traits.Granted + "TECH",
            Amount: 1,
            Card: granted.ObjectId,
            Affects: granted.ObjectId));

        Assert.Equal(3, Modified(world, world.Seats[0].IdentityCard, "hand_size"));

        for (int copy = 0; copy < 6; copy++)
        {
            world.CreateCard("01037", upgrades);
        }

        Assert.Equal(7, Modified(world, world.Seats[0].IdentityCard, "hand_size"));
    }

    [Rule("rr:draw")]
    [Fact]
    public void AvengersMansionDrawsForTheChosenPlayersIdentity()
    {
        // "Choose a player. That player draws 1 card." The chosen identity
        // carries its owner's seat through the suspended choice.
        var world = Hero("01029a", players: 2);
        var mansion = world.CreateCard(
            "01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var drawn = world.CreateCard("01087", world.Seats[1].Deck);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 0), action => action.Card == mansion.ObjectId);

        runner.Act(world, action, [], []);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        runner.Chose(
            world, mansion, 0, waiting.Index,
            Decision.Take(world.Seats[1].IdentityCard.ObjectId), waiting.Tier);

        Assert.Equal(DeckType.HandsArea, drawn.Area.Type);
        Assert.Equal(1, drawn.Owner);
        Assert.False(mansion.Ready);
    }

    private static long Modified(World world, Card card, string field) =>
        StateFields.Modified(world, card, field, Cards, world.Players);

    private static World Hero(string faceId, int players = 1)
    {
        var world = new World(Cards, players);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            string identity = player == 0 ? faceId : "01001a";
            seat.IdentityCard = world.CreateCard(identity, seat.Hero);
        }

        return world;
    }
}
