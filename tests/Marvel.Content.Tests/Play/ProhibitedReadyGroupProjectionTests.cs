using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic labelled ability probes general projection without admitting 02048.
public sealed class ProhibitedReadyGroupProjectionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:target.4.1")]
    [Rule("rr:then.2")]
    public void ReadyGroupSkipsProhibitedTargetsWhenCheckingThenBeforePayment(bool allProhibited)
    {
        // rr:target.4.1: "does not resolve against" invalid game elements.
        // rr:then.2: the preceding instruction "must resolve in full".
        // One permitted exhausted identity completes the ready instruction;
        // no permitted identities leave it unresolved.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"02048","abilities":[
              {"trigger":{"timing":"Constant"},"effect":
                {"grant":{"card":"attachedTo","keyword":"ready_prohibited","amount":1}}}]},
              {"card":"01008","abilities":[
                {"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                 "cost":{"spend":"R"},"effect":{"attack":{"target":{"query":"villain"},
                   "effect":{"then":{"effect":{"ready":{"query":"identities"}},
                     "then":{"choose":{"options":[{"draw":{"player":"you","count":1}},
                       {"heal":{"card":"you","amount":1}}]}}}}}}}]}]}
            """));
        Card? source = null;
        Card? payment = null;
        var (game, world) = ActionAbilitySupportFixtures6.Playing(board =>
        {
            source = board.CreateCard("01008", board.AreaOf(DeckType.UpgradesArea,
                PlayArea.Of(0), cardOwner: 0));
            payment = board.CreateCard("01090", board.Seats[0].Hand);
            foreach (int seat in board.PlayerOrder)
            {
                board.Seats[seat].IdentityCard.Exhaust();
                if (seat != 0 && !allProhibited) continue;
                board.CreateCard("02048", board.AreaOf(DeckType.UpgradesArea,
                    PlayArea.Of(seat), host: board.Seats[seat].IdentityCard.ObjectId));
            }
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: new NoCardAbilities());
        world.Abilities = runner;
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long consumed = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        var stage = world.Agenda.Stage;
        var prompt = game.Pending;
        if (allProhibited)
        {
            Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
        }
        else
        {
            var rejected = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
            Assert.Contains("suspends inside a labelled power", rejected.Message, StringComparison.Ordinal);
        }
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(stage, world.Agenda.Stage);
        Assert.Same(prompt, game.Pending);
        Assert.Same(world.Seats[0].Hand, payment!.Area);
        Assert.All(world.Seats, seat => Assert.False(seat.IdentityCard.Ready));
    }
}
