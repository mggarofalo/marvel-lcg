using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class ThorSearchPrivacyReviewTests
{
    [Fact]
    [Rule("rr:search")]
    public void ActualForAsgardSearchIsPrivateAndExposureEndsWithItsChoice()
    {
        // The searching player "is permitted to look at each of the cards in the searched area."
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse(File.ReadAllText(
            RepositoryPaths.Dataset("abilities", "abilities.json"))));
        var world = new World(facts, 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == 0 ? "06001a,06001b" : "01001a,01001b", seat.Hero);
        }
        world.Seats[0].IdentityCard.TurnTo("06001b");
        world.Abilities = runner;
        var chosen = world.CreateCard("06009", world.Seats[0].Deck);
        var other = world.CreateCard("06007", world.Seats[0].Deck);
        var nonmatching = world.CreateCard("01088", world.Seats[0].Deck);
        var card = world.CreateCard("06004", world.Seats[0].Hand);
        var payment = world.CreateCard("01003", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, facts, runner, []));
        var owner = WorldProjection.For(world, prompt, [], new RestrictedVisibilityPolicy(0).Authorize(null, 2));
        var stranger = WorldProjection.For(world, prompt, [], new RestrictedVisibilityPolicy(1).Authorize(null, 2));
        Assert.NotNull(owner.Prompt);
        Assert.Null(stranger.Prompt);
        Assert.Contains(owner.World.Areas.SelectMany(a => a.Cards), c => c.Id == chosen.ObjectId && c.Face is not null);
        Assert.DoesNotContain(stranger.World.Areas.SelectMany(a => a.Cards), c => c.Id == chosen.ObjectId || c.Id == other.ObjectId);
        Assert.DoesNotContain(owner.World.Areas.SelectMany(a => a.Cards), c => c.Id == nonmatching.ObjectId);
        Sequence.Answer(world, facts, runner, prompt, Decision.Take(chosen.ObjectId), []);
        Sequence.Finish(world, facts, runner, []);
        var after = WorldProjection.For(world, null, [], new RestrictedVisibilityPolicy(0).Authorize(null, 2));
        Assert.Contains(after.World.Areas.SelectMany(a => a.Cards), c => c.Id == chosen.ObjectId && c.Face is not null);
        Assert.DoesNotContain(after.World.Areas.SelectMany(a => a.Cards), c => c.Id == other.ObjectId);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
