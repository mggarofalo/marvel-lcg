using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Test-local printed Resize and Giant response, without admitting the pack.
public sealed class ResizeIndependentReviewTests
{
    [Fact]
    [Rule("rr:initiating-abilities.2")]
    public void NonFirstPlayerResolvesTheirOwnFormResponseBeforeTheirDraw()
    {
        // rr:initiating-abilities.2: responses initiate immediately after their condition.
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"12005","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},"effect":{"seq":[{"changeForm":{"player":"you","to":"other-hero"}},{"draw":{"player":"you","count":1}}]}}]},
              {"card":"12001c","abilities":[{"trigger":{"event":"WhenFormChanged","timing":"Response","subject":"this"},"effect":{"chooseCard":{"from":{"query":"enemies"},"effect":{"dealDamage":{"cards":"chosen","amount":1}}}}}]}
            ]}
            """));
        var world = new World(cards, 2, 241);
        var first = world.CreateSeat("first");
        first.IdentityCard = world.CreateCard("01001a,01001b", first.Hero);
        var actor = world.CreateSeat("actor");
        actor.IdentityCard = world.CreateCard("12001a,12001b,12001c", actor.Hero);
        var villain = world.CreateCard("01114", world.AreaOf(DeckType.VillainArea));
        for (int i = 0; i < 4; i++)
        {
            world.CreateCard("01087", first.Deck);
            world.CreateCard("01087", actor.Deck);
        }
        var resize = world.CreateCard("12005", actor.Hand);
        world.Abilities = runner;
        Assert.Equal(0, world.FirstPlayer);
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == resize.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var prompt = Sequence.Work(world, cards, runner, events);
        Assert.NotNull(prompt);
        Assert.Equal(1, prompt.Player);
        Assert.Equal("12001c", actor.IdentityCard.FaceId);
        Assert.Equal("01001a", first.IdentityCard.FaceId);
        Assert.Empty(actor.Hand.Cards);
        var response = Assert.Single(prompt.Affordances, a => a.AnchorId == actor.IdentityCard.ObjectId);
        Sequence.Answer(world, cards, runner, prompt, Decision.Take(response.Id, [villain.ObjectId], []), events);
        Sequence.Finish(world, cards, runner, events);
        Assert.Equal(1, villain.Damage);
        Assert.Single(actor.Hand.Cards);
        Assert.Empty(first.Hand.Cards);
        Assert.Equal(DeckType.DiscardPile, resize.Area.Type);
        Assert.Single(events.OfType<CardsFlipped>());
        Assert.False(world.Agenda.IsBusy);
    }
}
