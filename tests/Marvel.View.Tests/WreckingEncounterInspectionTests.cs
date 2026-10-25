using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class WreckingEncounterInspectionTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 0)]
    [InlineData(0, 1)]
    [InlineData(3, 1)]
    [Rule("rr:look-looked-at")]
    public void InspectionUsesOnlyTheActiveDeckAndRevokesPrivateFaces(int active, int player)
    {
        // rr:look-looked-at: "only the player who is resolving the ability can look at those cards."
        // pack:mc03:new-rules uses the active villain's deck for a generic encounter-deck reference.
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 2);
        for (int seatIndex = 0; seatIndex < 2; seatIndex++)
        {
            var seat = world.CreateSeat($"p{seatIndex}");
            seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        }
        var decoy = world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        string[] villains = ["07002", "07017", "07032", "07046"];
        string[] schemes = ["07004", "07019", "07034", "07048"];
        var groups = Enumerable.Range(0, 4).Select(index => Group(world, villains[index], schemes[index])).ToArray();
        world.BindEncounterTopology(new EncounterTopology(world, groups, groups[active].Villain));
        var runner = Runner();
        world.Abilities = runner;
        var orders = groups.Select(group => group.Deck.Cards.Select(card => card.ObjectId).ToArray()).ToArray();
        var action = Assert.Single(runner.Actions(world, player));
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, facts, runner, []));
        Assert.Equal(orders[active].AsEnumerable().Reverse().Take(3), prompt.ContextCardIds);
        Assert.DoesNotContain(decoy.ObjectId, prompt.ContextCardIds);
        VerifyVisibility(world, prompt, groups, active, player);
        Sequence.Answer(world, facts, runner, prompt, Decision.Take(0), []);
        Sequence.Finish(world, facts, runner, []);
        Assert.Equal(2, world.Seats[player].IdentityCard.Tokens.GetValueOrDefault("c_observed"));
        for (int index = 0; index < groups.Length; index++)
            Assert.Equal(orders[index], groups[index].Deck.Cards.Select(card => card.ObjectId));
        Assert.All(Project(world, null, player).World.Areas.Where(area => groups.Any(group => group.Deck.Id == area.Id))
            .SelectMany(area => area.Cards), card => Assert.Null(card.Face));
        foreach (var card in groups[active].Deck.Cards.ToArray())
            World.MoveToTop(card, groups[active].Discard);
        Assert.Empty(runner.Actions(world, player));
    }

    private static void VerifyVisibility(World world, Prompt prompt, EncounterDeckGroup[] groups, int active, int player)
    {
        var actor = Project(world, prompt, player);
        for (int index = 0; index < groups.Length; index++)
        {
            var cards = actor.World.Areas.Single(area => area.Id == groups[index].Deck.Id).Cards;
            Assert.Equal(index == active ? 3 : 0, cards.Count(card => card.Face is not null));
        }
        var other = Project(world, prompt, 1 - player);
        Assert.Null(other.Prompt);
        Assert.All(other.World.Areas.Where(area => groups.Any(group => group.Deck.Id == area.Id))
            .SelectMany(area => area.Cards), card => { Assert.Null(card.Id); Assert.Null(card.Face); });
    }

    private static EncounterDeckGroup Group(World world, string villain, string scheme)
    {
        var deck = world.CreateArea(DeckType.EncounterDeck);
        string[] faces = ["01104", "01121", "01104", "01104"];
        var cards = faces.Select(face => world.CreateCard(face, deck)).ToArray();
        return new EncounterDeckGroup(world.CreateCard(villain, world.AreaOf(DeckType.VillainArea)),
            world.CreateCard(scheme, world.AreaOf(DeckType.SideSchemesArea)), deck,
            world.CreateArea(DeckType.EncounterDiscardPile), cards);
    }

    private static AbilityRunner Runner() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"01001a","abilities":[{
          "trigger":{"timing":"Action","event":"WhenActionTriggered","subject":"this"},
          "effect":{"seq":[{"inspectEncounterTop":{"count":3,"result":"treacheriesLookedAt"}},
            {"placeCounters":{"card":"you","counter":"observed","count":{"result":"treacheriesLookedAt"}}}]}
        }]}]}
        """));

    private static VisibleResult Project(World world, Prompt? prompt, int seat) =>
        WorldProjection.For(world, prompt, [], new RestrictedVisibilityPolicy(seat).Authorize(null, world.Players));
}
