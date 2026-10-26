using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class ThorHeimdallPrivacyTests
{
    [Fact]
    [Rule("rr:look-looked-at")]
    public void ActualEntryRevealsPhysicalGroupOnlyToActingSeatUntilReturn()
    {
        // "only the player who is resolving the ability can look at those cards".
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));
        var world = new World(facts, 31);
        world.Abilities = runner;
        for (int i = 0; i < 2; i++)
        {
            var seat = world.CreateSeat($"p{i}");
            seat.IdentityCard = world.CreateCard(i == 0 ? "01001a" : "06001a", seat.Hero);
        }
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        for (int i = 0; i < 4; i++) world.CreateCard("01104", deck);
        var ally = world.CreateCard("06020", world.Seats[1].Hand);
        CardPlay.PutAllyIntoPlay(world, facts, runner, ally, 1, "fixture", []);
        var prompt = Sequence.Work(world, facts, runner, [])!;
        var response = Assert.Single(prompt.Affordances, a => a.AnchorId == ally.ObjectId);
        Sequence.Answer(world, facts, runner, prompt, Decision.Take(response.Id), []);
        prompt = Sequence.Work(world, facts, runner, [])!;
        Assert.All(Project(prompt, 1).World.Areas.Single(a => a.Id == deck.Id).Cards, c => Assert.Null(c.Face));
        Sequence.Answer(world, facts, runner, prompt, Decision.Take(0), []);
        prompt = Sequence.Work(world, facts, runner, [])!;
        int[] group = prompt.ContextCardIds.ToArray();
        Assert.Equal(3, group.Length);
        Assert.Equal(3, Project(prompt, 1).World.Areas.Single(a => a.Id == deck.Id).Cards.Count(c => c.Face is not null));
        Assert.Null(Project(prompt, 0).Prompt);
        Assert.All(Project(prompt, 0).World.Areas.Single(a => a.Id == deck.Id).Cards,
            c => { Assert.Null(c.Id); Assert.Null(c.Face); });
        Sequence.Answer(world, facts, runner, prompt, Decision.Take(group[1]), []);
        prompt = Sequence.Work(world, facts, runner, [])!;
        Assert.Equal(2, Project(prompt, 1).World.Areas.Single(a => a.Id == deck.Id).Cards.Count(c => c.Face is not null));
        Assert.All(Project(prompt, 0).World.Areas.Single(a => a.Id == deck.Id).Cards, c => Assert.Null(c.Face));
        Sequence.Answer(world, facts, runner, prompt, new Decision(ally.ObjectId, [group[2], group[0]]), []);
        Sequence.Finish(world, facts, runner, []);
        Assert.Equal(new[] { group[2], group[0] }, deck.Cards.Reverse().Take(2).Select(c => c.ObjectId));
        Assert.All(Project(null, 1).World.Areas.Single(a => a.Id == deck.Id).Cards, c => Assert.Null(c.Face));
        Assert.Empty(world.Agenda.Outstanding);

        VisibleResult Project(Prompt? current, int seat) => WorldProjection.For(world, current, [],
            new RestrictedVisibilityPolicy(seat).Authorize(null, world.Players));
    }
}
