using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Synthetic contracts for <c>discardHandOfKind</c>. Each replaces Hard to Keep
/// Down's row in the authored book; no played scenario is claimed.
/// </summary>
public sealed class HandKindDiscardTests : CoreCardTestBase
{
    // Hard to Keep Down (01104) discards each event from the revealing player's
    // hand, then places 1 threat on the main scheme for each card discarded.
    private const string DiscardingTreachery = """
        [{"trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{"seq":[{"discardHandOfKind":"event"},
            {"placeThreat":{"scheme":{"query":"mainScheme"},"amount":{"result":"discarded"}}}]}}]
        """;

    // Maria Hill, an ally, and Energy, a resource.
    private static readonly string[] NotEvents = ["01067", "01088"];

    [Fact]
    public void AHandKindOtherThanEventIsRefused()
    {
        // Only the kind a card needs is admitted.
        var thrown = Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse("""
            {"cards":[{"card":"01104","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"discardHandOfKind":"ally"}}]}]}
            """)));
        Assert.Contains("expected the hand card kind 'event'", thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [Rule("rr:you-your.8")]
    public void EveryEventInTheRevealingPlayersHandIsDiscardedAndCounted(int events)
    {
        // rr:you-your.8: "If a game step or card ability discards cards from 'your'
        // hand, the player resolving that game step or card ability discards the
        // cards from their hand." Player 1, not the first player, reveals. Their
        // ally and resource stay; player 0's event stays.
        var (world, f) = Board();
        var kept = NotEvents
            .Select(face => world.CreateCard(face, world.Seats[1].Hand)).ToList();
        var discarded = Enumerable.Range(0, events)
            .Select(_ => world.CreateCard("01005", world.Seats[1].Hand)).ToList();
        var other = world.CreateCard("01005", world.Seats[0].Hand);
        f.Reveal("01104", player: 1);

        f.Finish();

        Assert.Equal(kept.Select(card => card.ObjectId).Order(),
            world.Seats[1].Hand.Cards.Select(card => card.ObjectId).Order());
        Assert.All(discarded, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.Equal([other], world.Seats[0].Hand.Cards);
        Assert.Equal(events, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(world.Agenda.IsBusy);
    }

    // The authored book with Hard to Keep Down's abilities replaced by the synthetic ones.
    private static (World World, RedSkullFixture Fixture) Board()
    {
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var row = book["cards"]!.AsArray().Single(card => (string?)card!["card"] == "01104")!;
        row["abilities"] = JsonNode.Parse(DiscardingTreachery);
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var (world, _) = KangTemporalFixture.Board(players: 2, encounter: 0);
        world.Abilities = runner;
        return (world, new RedSkullFixture(world, runner));
    }
}
