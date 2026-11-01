using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class JessicaDrewInspectionTests
{
    [Theory]
    [InlineData(DeckType.PlayerDeck)]
    [InlineData(DeckType.EncounterDeck)]
    [InlineData(DeckType.ExperimentalWeaponsDeck)]
    [InlineData(DeckType.SideSchemeDeck)]
    public void ActualInspectionExposesOnlyTopCardToActingSeatAndRevokesAfterDone(DeckType kind)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == 0 ? "04031b" : "01001a", seat.Hero);
        }
        var area = kind == DeckType.PlayerDeck ? world.Seats[1].Deck : world.AreaOf(kind);
        world.CreateCard("01088", area);
        var top = world.CreateCard("01089", area);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"04031b","abilities":[{
              "trigger":{"timing":"Action","event":"WhenActionTriggered","subject":"this"},
              "limitPerRound":1,"effect":{"peekTop":"anyDeck"}
            }]}]}
            """));
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 0));
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var decks = Sequence.Work(world, cards, runner, events)!;
        AssertConcealed(Project(world, decks, 0), area.Id);
        AssertConcealed(Project(world, decks, 1), area.Id);
        var selected = Assert.Single(decks.Affordances, option => option.AnchorId == area.Id);
        Sequence.Answer(world, cards, runner, decks, Decision.Take(selected.Id), events);
        var inspection = Sequence.Work(world, cards, runner, events)!;

        var entitled = Project(world, inspection, 0);
        Assert.Equal(PublicDecisionKind.CardLook, entitled.Prompt!.PublicKind);
        var visible = entitled.World.Areas.Single(item => item.Id == area.Id).Cards;
        Assert.Null(visible[0].Id);
        Assert.Null(visible[0].Face);
        Assert.Equal(top.ObjectId, visible[1].Id);
        Assert.Equal("01089", visible[1].Face!.Id);
        Assert.Equal(cards.Text("01089"), visible[1].Face!.RulesText);
        Assert.Equal([top.ObjectId], entitled.Prompt.ContextCardIds);
        Assert.Equal([world.Seats[0].IdentityCard!.ObjectId], entitled.Prompt.CauseCardIds);
        Assert.Equal("Done", Assert.Single(entitled.Prompt.Affordances).CommitLabel);
        var presentation = PromptPresentation.From(entitled.Prompt, entitled.World);
        Assert.Equal((DecisionSubject.Inspecting, "Genius"), (presentation.Frame.Subject, presentation.Frame.SubjectTitle));
        Assert.Contains(presentation.Frame.Causes, cause => cause.Contains("Jessica Drew", StringComparison.Ordinal));
        var other = Project(world, inspection, 1);
        Assert.Null(other.Prompt);
        AssertConcealed(other, area.Id);

        Sequence.Answer(world, cards, runner, inspection,
            Decision.Take(Assert.Single(inspection.Affordances).Id), events);
        Sequence.Finish(world, cards, runner, events);
        AssertConcealed(Project(world, null, 0), area.Id);
        AssertConcealed(Project(world, null, 1), area.Id);
        Assert.False(top.FaceUp);
        Assert.Equal(["01088", "01089"], area.Cards.Select(card => card.FaceId));
    }

    private static VisibleResult Project(World world, Prompt? prompt, int seat) =>
        WorldProjection.For(world, prompt, [], new RestrictedVisibilityPolicy(seat).Authorize(null, world.Players));

    private static void AssertConcealed(VisibleResult result, int area) =>
        Assert.All(result.World.Areas.Single(item => item.Id == area).Cards, card =>
        {
            Assert.Null(card.Id);
            Assert.Null(card.Face);
        });
}
