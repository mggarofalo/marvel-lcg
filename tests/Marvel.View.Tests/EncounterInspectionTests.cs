using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class EncounterInspectionTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void HiddenContextRequiresBothInspectionKindAndExplicitExposure(bool look, bool expose)
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var hidden = world.CreateCard("01104", deck);
        var prompt = new Prompt(0, Question.Element, Marvel.Rules.Timing.TimingPriority.Untimed,
            "test", "Context", false, [])
        {
            PublicKind = look ? PublicDecisionKind.CardLook : PublicDecisionKind.CardSearch,
            ContextCardIds = [hidden.ObjectId], ExposesConcealedCandidates = expose,
        };
        var projected = Project(world, prompt, 0);
        Assert.Null(Assert.Single(projected.World.Areas.Single(area => area.Id == deck.Id).Cards).Face);
        Assert.DoesNotContain(hidden.ObjectId, projected.Prompt!.ContextCardIds);
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, 1, false)]
    [InlineData(2, 1, false)]
    [InlineData(3, 2, false)]
    [InlineData(3, 0, true)]
    [InlineData(4, 2, false)]
    [Rule("rr:look-looked-at")]
    [Rule("rr:look-looked-at.1.1")]
    public void InspectionIsPrivateOrderedAndRetainsTreacheryCount(int size, int treacheries, bool onlyMinions)
    {
        // rr:look-looked-at: "only the player who is resolving the ability can look at those cards."
        // rr:look-looked-at.1.1: cards are "returned to that deck in the same order."
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        }
        var deck = world.AreaOf(DeckType.EncounterDeck);
        string[] faces = ["01104", "01121", "01104", "01104"];
        for (int index = 0; index < size; index++) world.CreateCard(onlyMinions ? "01121" : faces[index], deck);
        var scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 5);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01001a","abilities":[{
              "trigger":{"timing":"Action","event":"WhenActionTriggered","subject":"this"},
              "effect":{"seq":[
                {"inspectEncounterTop":{"count":3,"result":"treacheriesLookedAt"}},
                {"removeThreat":{"scheme":{"query":"mainScheme"},"amount":{"result":"treacheriesLookedAt"}}}
              ]}
            }]}]}
            """));
        world.Abilities = runner;
        var order = deck.Cards.Select(card => card.ObjectId).ToArray();
        if (size == 0)
        {
            Assert.Empty(runner.Actions(world, 0));
            return;
        }
        var action = Assert.Single(runner.Actions(world, 0));
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var prompt = Sequence.Work(world, facts, runner, events)!;
        Assert.Equal(PublicDecisionKind.CardLook, prompt.PublicKind);
        Assert.Equal(order.AsEnumerable().Reverse().Take(3), prompt.ContextCardIds);
        Assert.Equal(5, scheme.Tokens.GetValueOrDefault("k_threat"));
        var actor = Project(world, prompt, 0);
        var shown = actor.World.Areas.Single(area => area.Id == deck.Id).Cards;
        Assert.Equal(Math.Min(3, size), shown.Count(card => card.Face is not null));
        Assert.Equal(prompt.ContextCardIds, actor.Prompt!.ContextCardIds);
        Assert.All(Project(world, prompt, 1).World.Areas.Single(area => area.Id == deck.Id).Cards,
            card => { Assert.Null(card.Face); Assert.Null(card.Id); });
        Assert.Null(Project(world, prompt, 1).Prompt);
        Sequence.Answer(world, facts, runner, prompt, Decision.Take(0), events);
        Sequence.Finish(world, facts, runner, events);
        Assert.Equal(5 - treacheries, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(order, deck.Cards.Select(card => card.ObjectId));
        Assert.All(deck.Cards, card => Assert.False(card.FaceUp));
        Assert.All(Project(world, null, 0).World.Areas.Single(area => area.Id == deck.Id).Cards,
            card => Assert.Null(card.Face));
    }

    private static VisibleResult Project(World world, Prompt? prompt, int seat) =>
        WorldProjection.For(world, prompt, [], new RestrictedVisibilityPolicy(seat).Authorize(null, world.Players));
}
