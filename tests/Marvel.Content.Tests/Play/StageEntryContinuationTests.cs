using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class StageEntryContinuationTests : CoreCardTestBase
{
    [Rule("rr:when-revealed-abilities")]
    [Theory]
    [InlineData("klaw")]
    [InlineData("rhino")]
    public void UnprojectedStageSearchCannotSupplyGuessedHealthToALaterDamageEffect(string scenario)
    {
        // On revealing a new villain stage, "all “When Revealed” abilities on
        // the card resolve." Klaw II finds The Immortal Klaw (+10 hit points).
        var runner = Runner("01006", "Action", """
            {"attack":{"target":{"query":"villain"},"effect":{"seq":[
              {"dealAttackDamage":{"cards":{"query":"villain"},"amount":100}},
              {"dealAttackDamage":{"cards":{"query":"villain"},"amount":18}},
              {"draw":{"player":"you","count":1}}
            ]}}}
            """, cost: """{"exhaust":"this"}""", includeAuthored: true);
        Card? source = null;
        Card? villain = null;
        var error = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, "01006");
            villain = board.TheCardIn(DeckType.VillainArea);
        }, hero: true, abilities: runner, scenario: scenario));
        Assert.Contains("unprojected When Revealed", error.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:when-revealed-abilities")]
    [Rule("rr:villain-defeat.3.1")]
    [Rule("rr:villain-defeat.3.2")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AuthoredPressTheAdvantageReadsItsOriginalTargetAfterKlawStageEntry(bool stunned)
    {
        // The new stage is "the same character" and status cards carry over.
        // Press the Advantage checks that enemy after stage entry settles.
        var world = Hero("01001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var first = world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));
        var next = world.CreateCard("01114", world.AreaOf(DeckType.VillainDeck));
        var scheme = world.CreateCard("01127", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        first.TakeDamage(10);
        if (stunned) Statuses.Give(world, first, Statuses.Stunned);
        var source = world.CreateCard("04043", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var drawn = world.CreateCard("01089", world.Seats[0].Deck);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [])).Events.ToList();
        var prompt = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(first.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Same(next, world.TheCardIn(DeckType.VillainArea));
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Equal(28, Damage.Health(world, Cards, next));
        Assert.Same(stunned ? world.Seats[0].Hand : world.Seats[0].Deck, drawn.Area);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(stunned, Statuses.Has(world, next, Statuses.Stunned));
    }
}
