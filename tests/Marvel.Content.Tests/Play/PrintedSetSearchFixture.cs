using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Test-local 09006 program uses its printed text on nonpublic prepared boards.
// It proves the shared selector prerequisite, not production content admission.
internal static class PrintedSetSearchFixture
{
    internal const string Areas = "\"areas\":[\"yourDeck\",\"yourDiscardPile\"]";

    internal static AbilityRunner Runner(string fields = Areas + ",\"set\":\"doctor_strange\"", string? guardFields = null)
    {
        string selection = "{\"cardsIn\":{" + fields + "}}";
        return new AbilityRunner(AbilityCatalog.Parse("""
        {"cards":[{"card":"09006","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"alter-ego"},
          "effect":{"if":{"test":{"exists":GUARD},
            "then":{"chooseCard":{"from":SELECTION,"effect":{"seq":[
              {"addToHand":"chosen"},{"shuffle":"yourDeck"}]}}},
            "else":{"shuffle":"yourDeck"}}}
        }]}]}
        """.Replace("SELECTION", selection, StringComparison.Ordinal)
            .Replace("GUARD", "{\"cardsIn\":{" + (guardFields ?? fields) + "}}", StringComparison.Ordinal)));
    }

    internal static World Board(AbilityRunner? runner = null)
    {
        var world = ThorActualFixture.Board("06001a", 2);
        var seat = world.Seats[1];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea));
        seat.IdentityCard = world.CreateCard("09001b,09001a", seat.Hero);
        var aside = world.CreateArea(DeckType.AsideDeck, 1, PlayArea.Of(1));
        var cards = Enumerable.Range(32, 5).Select(id => world.CreateCard($"090{id}", aside)).ToArray();
        var invocation = InvocationDeck.Initialize(world, 1, cards, "test setup", []);
        World.MoveToTop(cards.Single(c => c.FaceId == "09032"), invocation.Discard);
        world.Abilities = runner ?? Runner();
        return world;
    }

    internal static Area Discard(World world, int player = 1) =>
        world.AreaOf(DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player);

    internal static void PlayStudies(World world)
    {
        var seat = world.Seats[1];
        var source = world.CreateCard("09006", seat.Hand);
        var resource = world.CreateCard("01093", seat.Hand);
        var runner = world.Abilities!;
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [resource.ObjectId], []));
    }

    internal static Prompt Prompt(World world) => Assert.IsType<Prompt>(
        Sequence.Work(world, world.Facts, world.Abilities!, []));

    internal static void Answer(World world, Prompt prompt, int id) =>
        Sequence.Answer(world, world.Facts, world.Abilities!, prompt, Decision.Take(id), []);

    internal static void Finish(World world) => Sequence.Finish(world, world.Facts, world.Abilities!, []);
}
