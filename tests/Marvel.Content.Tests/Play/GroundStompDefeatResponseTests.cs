using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class GroundStompDefeatResponseTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(1, true)]
    [Rule("rr:you-your.12")]
    [Rule("rr:triggering-condition.2")]
    [Rule("rr:exhausted.2")]
    public void GroundStompDefeatsOfferInterrogationRoom(int count, bool drone)
    {
        // Ground Stomp: "Deal 1 damage to each enemy." Interrogation Room:
        // "After you defeat a minion, exhaust Interrogation Room" to remove 1 threat.
        // rr:you-your.12 makes event abilities an extension of the identity;
        // rr:triggering-condition.2 gives the effect one shared response window.
        var recipe = new PlayerDeckRecipe([
            .. Setup.Hero("she_hulk").HeroDeck, .. Setup.Hero("spider_man").PlayerDeck]);
        var order = Dealer.DealOrder(Setup, drone ? "ultron" : "rhino", ["she_hulk"],
            facts: Cards, playerDecks: [recipe]);
        var world = WorldSetup.DealWithoutCardAbilities(Cards, Blueprints.From(order, Cards),
            ["She-Hulk"], 537);

        world.Seats[0].IdentityCard.TurnTo("01019a");
        Card room = world.Cards.First(card => card.FaceId == "01063");
        World.MoveToTop(room, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        Card stomp = world.Cards.First(card => card.FaceId == "01022");
        Card energy = world.Cards.First(card => card.FaceId == "01088");
        World.MoveToTop(stomp, world.Seats[0].Hand);
        World.MoveToTop(energy, world.Seats[0].Hand);
        Card scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 3);
        long threat = scheme.Tokens["k_threat"];
        var victims = ArrangeVictims(world, count, drone);
        var game = Game.Begin(world, Cards, AuthoredCards.Runner());
        while (game.Pending is { } prompt && prompt.Affordances.Any(a => a.Verb == Game.ResolveMulligans))
            game.Resolve(Decision.Take(prompt.Affordances.Single(a => a.Verb == Game.ResolveMulligans).Id));


        Affordance play = Assert.Single(game.PromptFor(0)!.Affordances, a =>
            a.AnchorId == stomp.ObjectId && a.Verb == Game.ActionVerb);
        game.Resolve(PaidDecision.Take(play, [energy.ObjectId]));

        Assert.All(victims, victim => Assert.Equal(
            drone ? DeckType.DiscardPile : DeckType.EncounterDiscardPile, victim.Area.Type));
        if (drone) Assert.False(EffectiveCards.HasProfile(victims[0]));
        var responses = game.Pending!.Affordances.Where(a => a.AnchorId == room.ObjectId).ToList();
        Assert.Equal(count, responses.Count);
        Affordance response = responses[0];
        game.Resolve(Decision.Take(response.Id));
        Affordance choice = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(scheme.ObjectId, choice.AnchorId);
        game.Resolve(Decision.Take(choice.Id));

        Assert.Equal(threat - 1, scheme.Tokens["k_threat"]);
        Assert.False(room.Ready);
        Assert.DoesNotContain(game.Pending!.Affordances, a => a.AnchorId == room.ObjectId);
    }

    private static List<Card> ArrangeVictims(World world, int count, bool drone)
    {
        if (drone)
        {
            Card physical = world.Seats[0].Deck.Cards[^1];
            FacedownMinions.EngageTop(world, 0, AuthoredCards.DroneProfile, "fixture", "Create_Drone", []);
            Assert.True(EffectiveCards.HasProfile(physical));
            return [physical];
        }
        var victims = world.Cards.Where(card => card.FaceId == "01101").Take(count).ToList();
        Assert.Equal(count, victims.Count);
        foreach (Card victim in victims)
        {
            World.MoveToTop(victim, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            victim.TakeDamage(2);
        }
        return victims;
    }
}
