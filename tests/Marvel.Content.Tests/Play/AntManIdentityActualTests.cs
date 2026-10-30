using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AntManIdentityActualTests
{
    [Theory]
    [InlineData("12001a", "12001b")]
    [InlineData("12001a", "12001c")]
    [InlineData("12001b", "12001a")]
    [InlineData("12001b", "12001c")]
    [InlineData("12001c", "12001a")]
    [InlineData("12001c", "12001b")]
    [Rule("rr:form-change-form.2")]
    [Rule("rr:ability.11")]
    public void AllDirectedChangesOfferOnlyTheDestinationResponse(string from, string to)
    {
        // rr:form-change-form.2: "When a player changes form, only the form changes."
        // rr:ability.11: "all interrupt and response abilities are optional" unless forced.
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        string observer = $$$$$"""{"card":"01006","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},"effect":{"changeForm":{"player":"you","to":{"face":"{{{{{to}}}}}"}}}}]}""";
        var runner = AntManF1Fixture.Runner(["12001a", "12001b", "12001c"], observer);
        var world = new World(cards, 1, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        seat.IdentityCard.TurnTo(from);
        seat.IdentityCard.TakeDamage(3);
        var scheme = world.CreateCard("01116b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 2);
        var villain = world.CreateCard("01114", world.AreaOf(DeckType.VillainArea));
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var prompt = Sequence.Work(world, cards, runner, events);
        Assert.NotNull(prompt);
        Assert.Equal(to, seat.IdentityCard.FaceId);
        Assert.Equal(3, seat.IdentityCard.Damage);
        Assert.True(prompt.Cancellable);
        var response = Assert.Single(prompt.Affordances, item => item.AnchorId == seat.IdentityCard.ObjectId);
        int[] targets = to == "12001a" ? [scheme.ObjectId] : to == "12001c" ? [villain.ObjectId] : [];
        if (to != "12001b")
        {
            int wrong = to == "12001a" ? villain.ObjectId : scheme.ObjectId;
            Assert.False(DecisionAssessment.Assess(prompt, Decision.Take(response.Id, [wrong], [])).IsAccepted);
            Assert.True(DecisionAssessment.Assess(prompt, Decision.Take(response.Id, targets, [])).IsAccepted);
        }
        Sequence.Answer(world, cards, runner, prompt, Decision.Take(response.Id, targets, []), events);
        Sequence.Finish(world, cards, runner, events);
        Assert.Equal(to == "12001b" ? 2 : 3, seat.IdentityCard.Damage);
        Assert.Equal(to == "12001a" ? 1 : 2, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(to == "12001c" ? 1 : 0, villain.Damage);
        Assert.Equal(0, seat.FormChangedInRound);
        Assert.False(world.Agenda.IsBusy);
    }
}
