using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Events;
using Marvel.Rules.Timing;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DoctorStrangeEyeSuspensionTests
{
    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void ExhaustedEyeIsUnavailableThroughoutSuspendedPayment()
    {
        // "Pay the cost(s)." Actual Eye and Cloak rows use a synthetic lethal
        // resource-generator cost and optional defeat interrupt to suspend payment.
        var synthetic = AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01030","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
                "cost":{"dealDamage":{"cards":"this","amount":2}},"effect":{"generate":"G"}}]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}}]}
            ]}
            """);
        var book = AuthoredCards.Book;
        var runner = new AbilityRunner(book with
        {
            Abilities = [.. book.Abilities.Where(a => a.Card is not ("01030" or "01092")), .. synthetic.Abilities],
        });
        Card eye = null!, generator = null!, target = null!;
        var (_, world) = Playing(board =>
        {
            eye = board.CreateCard("09011", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            generator = board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            target = board.CreateCard("09009", board.Seats[0].Hand);
            InPlay(board, "01092");
        }, hero: true, abilities: runner);
        var events = new List<GameEvent>();
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], target, [eye.ObjectId, generator.ObjectId], events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(Question.Opportunity, prompt.Asking);
        Assert.False(eye.Ready);
        Assert.Equal(DeckType.RevealingArea, target.Area.Type);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var agenda = world.Agenda.Outstanding.ToArray();

        Assert.DoesNotContain(runner.ResourceAbilities(world, 0), source => source.Effect == eye.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteResource(world, 0, eye.ObjectId));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(agenda, world.Agenda.Outstanding);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(DeckType.UpgradesArea, target.Area.Type);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.False(eye.Ready);
        Assert.False(world.Agenda.IsBusy);
    }
}

