using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Nonpublic all-five-card fixtures, including explicitly synthetic paid Specials.</summary>
internal static class SpellMasteryFixture
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static AbilityRunner Runner(bool paid, bool unsupported, bool choice, string? special = null) => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"09001a","abilities":[{
          "name":"Spell Mastery",
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "cost":{"seq":[{"exhaust":"this"},{"invocationTop":"you"}]},
          "effect":{"resolveInvocation":"you"}
        }]},{"card":"09005","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "cost":{"invocationTop":"you"},
          "effect":{"then":{"effect":{"resolveInvocation":"you"},"then":{"returnInvocation":"you"}}}
        }]},{"card":"09036","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Special","subject":"this"},
          "effect":{"seq":[{"draw":{"player":"you","count":3}},{"discardInvocation":"this"}]}
        }]},{"card":"01030","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
          "cost":{"dealDamage":{"cards":"this","amount":2}},"effect":{"generate":"YY"}
        }]},{"card":"01092","abilities":[{
          "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
          "effect":{"draw":{"player":"you","count":1}}
        }]}]}
        """.Replace("""{"seq":[{"draw":{"player":"you","count":3}},{"discardInvocation":"this"}]}""",
            special ?? """{"seq":[{"draw":{"player":"you","count":3}},{"discardInvocation":"this"}]}""", StringComparison.Ordinal)
        .Replace("09036", paid && !unsupported ? "09032" : "09036", StringComparison.Ordinal)
        .Replace("""{"draw":{"player":"you","count":3}}""", paid
            ? """{"if":{"test":{"finalStep":"true"},"then":{"draw":{"player":"you","count":9}},"else":{"draw":{"player":"you","count":3}}}}"""
            : """{"draw":{"player":"you","count":3}}""", StringComparison.Ordinal)
        .Replace("\"seq\":[{\"draw\":{\"player\":\"you\",\"count\":3}}",
            choice ? "\"seq\":[{\"choose\":{\"options\":[{\"draw\":{\"player\":\"you\",\"count\":3}},{\"draw\":{\"player\":\"you\",\"count\":1}}]}}"
                : "\"seq\":[{\"draw\":{\"player\":\"you\",\"count\":3}}", StringComparison.Ordinal)));

    internal static Affordance Mastery(Game game, Card hero) => Assert.Single(game.Pending!.Affordances,
        offer => offer.AnchorId == hero.ObjectId && offer.Verb == Game.ActionVerb);

    internal static (World World, Game Game, InvocationDeck Deck, AbilityRunner Runner) Board(int player, bool last = false, bool paid = false, bool live = false, bool unsupported = false,
        int master = 0, bool choice = false) =>
        CreateBoard(Runner(paid, unsupported, choice), player, last, paid, live, master);

    // Synthetic Special control: a real decision can occur after the Invocation has left its deck.
    internal static (World World, Game Game, InvocationDeck Deck, AbilityRunner Runner) BoardWithSpecial(string special, bool last) =>
        CreateBoard(Runner(false, false, false, special), 1, last, false, false, 1);

    private static (World World, Game Game, InvocationDeck Deck, AbilityRunner Runner) CreateBoard(
        AbilityRunner runner, int player, bool last, bool paid, bool live, int master)
    {
        // Master mode 3 supplies a fixed shuffle control whose resolved copy is below the new top.
        var world = new World(Cards, player + 1, Seed(master));
        for (int index = 0; index <= player; index++)
        {
            var seat = world.CreateSeat($"p{index}");
            seat.IdentityCard = world.CreateCard(index == player ? "09001b,09001a" : "01001b,01001a", seat.Hero);
            for (int i = 0; i < 20; i++) world.CreateCard("01088", seat.Deck);
        }
        world.CreateCard("01134", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01137b", world.AreaOf(DeckType.MainSchemesArea));
        var aside = world.CreateArea(DeckType.AsideDeck, player, PlayArea.Of(player));
        var cards = Enumerable.Range(32, 5).Select(id => world.CreateCard($"090{id}", aside)).ToArray();
        var deck = InvocationDeck.Initialize(world, player, cards, "setup", []);
        // Fixture arrangement precedes public top exposure and keeps all five physical cards.
        World.MoveToTop(cards.Single(card => card.FaceId == (paid ? "09032" : "09036")), deck.Deck);
        if (last)
            foreach (var card in deck.Deck.Cards.Where(card => card.FaceId != "09036").ToArray())
                World.MoveToTop(card, deck.Discard);
        if (live)
        {
            var generator = world.CreateCard("01030",
                world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player));
            generator.TakeDamage(2);
            world.CreateCard("01092",
                world.AreaOf(DeckType.SupportsArea, PlayArea.Of(player), cardOwner: player));
        }
        ConfigureMaster(world, player, master);
        var game = Game.Begin(world, Cards, runner);
        while (game.Phase == GamePhase.Mulligan)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        while (game.Active != player)
            game.Resolve(Decision.Decline);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, offer => offer.Verb == Game.ChangeForm).Id));
        return (world, game, deck, runner);
    }

    private static uint Seed(int master) => master == 3 ? 1u : 5489u;

    private static void ConfigureMaster(World world, int player, int master)
    {
        if (master > 0) world.CreateCard("09005", world.Seats[player].Hand);
        if (master == 2) CardPayment.ReduceNextCardCost(world, world.Seats[player].IdentityCard, player, 2);
    }

    internal static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Stage,
        Pending = world.Agenda.Outstanding.Select(step => new { step.What, step.Index, step.Subject, step.Seat }),
    });
}
