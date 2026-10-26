using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

/// <summary>Prepared nonpublic engine board with actual authored programs; no product admission.</summary>
internal static class InvocationInteractionFixture
{
    internal static (World World, Game Game, InvocationDeck Deck, Card Master) Create(string face,
        bool syntheticNoDiscard = false, bool syntheticReturnOtherwise = false)
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var book = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(
            RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        if (syntheticNoDiscard)
        {
            // Explicit synthetic control: retaining an Invocation does not imply self-discard.
            var row = book["cards"]!.AsArray().Single(item => item!["card"]!.GetValue<string>() == face)!;
            row["abilities"]![0]!["effect"] = System.Text.Json.Nodes.JsonNode.Parse("""
                {"draw":{"player":"you","count":1}}
                """);
        }
        if (syntheticReturnOtherwise)
        {
            // Synthetic control: this return runs only when the Special procedure does not fully resolve.
            var row = book["cards"]!.AsArray().Single(item => item!["card"]!.GetValue<string>() == "09005")!;
            row["abilities"]![0]!["effect"] = System.Text.Json.Nodes.JsonNode.Parse("""
                {"otherwise":{"effect":{"resolveInvocation":"you"},"otherwise":{"returnInvocation":"you"}}}
                """);
        }
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var world = new World(facts, 2, 5489);
        for (int index = 0; index < 2; index++)
        {
            var seat = world.CreateSeat($"p{index}");
            seat.IdentityCard = world.CreateCard(index == 1 ? "09001b,09001a" : "01001b,01001a", seat.Hero);
            for (int count = 0; count < 20; count++) world.CreateCard("01088", seat.Deck);
        }
        world.CreateCard("01134", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01137b", world.AreaOf(DeckType.MainSchemesArea));
        var aside = world.CreateArea(DeckType.AsideDeck, 1, PlayArea.Of(1));
        var cards = Enumerable.Range(32, 5).Select(number => world.CreateCard($"090{number}", aside)).ToArray();
        var deck = InvocationDeck.Initialize(world, 1, cards, "prepared fixture", []);
        World.MoveToTop(cards.Single(card => card.FaceId == face), deck.Deck);
        var master = world.CreateCard("09005", world.Seats[1].Hand);
        var game = Game.Begin(world, facts, runner);
        while (game.Phase == GamePhase.Mulligan)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        while (game.Active != 1) game.Resolve(Decision.Decline);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.Verb == Game.ChangeForm).Id));
        return (world, game, deck, master);
    }

    internal static (Prompt Prompt, PromptPresentation Presentation) Project(World world, Game game)
    {
        var visible = WorldProjection.For(world, game.Pending, [], new RestrictedVisibilityPolicy(1).Authorize(null, 2));
        return (visible.Prompt!, PromptPresentation.From(visible.Prompt!, visible.World));
    }

    internal static Decision Pay(World world, Affordance offer, Card top)
    {
        int[] resources = [world.Seats[1].Hand.Cards.First(card => card.FaceId == "01088").ObjectId];
        return new Decision(offer.Id, [top.ObjectId], resources,
            Allocations: ResourcePayment.Allocate(Assert.Single(offer.CostOptions), resources)!);
    }
}
