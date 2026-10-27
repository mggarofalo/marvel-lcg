using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic compiled programs exercise the generic contract without admitting a pack.
public sealed class BoundedDamageLegalityTests
{
    private const string Group = """{"dealDamage":{"cards":{"query":"minionsEngagedWithYou"},"amount":3,"maximum":3}}""";

    [Theory]
    [InlineData(-3)]
    [InlineData(-2)]
    [InlineData(1)]
    [Rule("rr:event.5")]
    [Rule("rr:target.3")]
    public void LiveEventModifierControlsInitiationPreviewAndEverySelectedInstance(int modifier)
    {
        // Event damage: "each of those instances is modified". A valid target
        // is one that "any part of that ability can affect".
        var runner = Runner("01005", "Action", Group);
        Card? source = null;
        Card[] targets = [];
        int[] payment = [];
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard("01005", board.Seats[0].Hand);
            payment = Enumerable.Range(0, 3).Select(_ => board.CreateCard("01087", board.Seats[0].Hand).ObjectId).ToArray();
            targets = Enumerable.Range(0, 2).Select(_ => board.CreateCard("01184",
                board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)))).ToArray();
        }, hero: true, abilities: runner);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "eventDamage", Amount: modifier, Card: source!.ObjectId, Affects: source.ObjectId));
        var before = world.Digest().Canonical();
        var actions = runner.Actions(world, 0).Where(action => action.Card == source.ObjectId).ToList();
        if (modifier == -3)
        {
            Assert.Empty(actions);
            Assert.Equal(before, world.Digest().Canonical());
            Assert.All(targets, target => Assert.Equal(0, target.Damage));
            Assert.All(payment, id => Assert.Same(world.Seats[0].Hand, world.Cards[id].Area));
            return;
        }
        runner.ExecuteAction(world, new PlayerAction(Assert.Single(actions), payment, []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var offer = Assert.Single(prompt.Affordances);
        Assert.All(offer.Targets!.Details!.Values, detail => Assert.Contains($"Deal {3 + modifier} damage", detail));
        Sequence.Answer(world, world.Facts, runner, prompt, new Decision(offer.Id,
            targets.Select(target => target.ObjectId).ToArray()), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.All(targets, target => Assert.Equal(3 + modifier, target.Damage));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:target.3.7")]
    public void LiveProhibitionRejectsStaleRecipientAndAllProhibitedRecipientsPreventInitiation()
    {
        // A target that "cannot take damage" is not valid for damage alone.
        var runner = Program("""
            ,{"card":"01184","abilities":[{"trigger":{"timing":"Constant"},
              "effect":{"preventDamageWhile":{"card":"this","condition":{
                "atLeast":{"value":{"damageOn":"this"},"count":1}}}}}]}
            """);
        Card? source = null;
        Card? target = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            target = board.CreateCard("01184", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            target.TakeDamage(1);
        }, hero: true, abilities: runner);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
        target!.TakeDamage(-1);
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source!.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var offer = Assert.Single(prompt.Affordances);
        target.TakeDamage(1);
        string before = Snapshot(world);
        Assert.ThrowsAny<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner,
            prompt, new Decision(offer.Id, [target.ObjectId]), []));
        Assert.Equal(before, Snapshot(world));
        var refreshed = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Empty(Assert.Single(refreshed.Affordances).Targets!.Legal);
        Sequence.Answer(world, world.Facts, runner, refreshed, new Decision(offer.Id, []), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(1, target.Damage);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:damage.step.1")]
    [Rule("rr:damage.step.5")]
    public void SuspendedPreventionKeepsTheWholeGroupBeforePlacementAndContinuationDrawsOnce()
    {
        // "Would deal/be dealt" windows precede "placing of damage" on every recipient.
        var runner = Program("""
            ,{"card":"01184","abilities":[{
              "trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","target":"this"},
              "effect":{"choose":{"options":[
                {"preventDamage":{"card":"trigger.target","amount":1}},
                {"preventDamage":{"card":"trigger.target","amount":2}}]}}}]}
            """, draw: true);
        Card? source = null;
        Card? first = null;
        Card? second = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            first = board.CreateCard("01184", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            second = board.CreateCard("01182", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        int hand = world.Seats[0].Hand.Cards.Count;
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            item => item.AnchorId == source!.ObjectId).Id));
        game.Resolve(new Decision(Assert.Single(game.Pending!.Affordances).Id, [first!.ObjectId, second!.ObjectId]));
        Assert.Equal(Question.Option, game.Pending!.Asking);
        Assert.Equal(0, first.Damage);
        Assert.Equal(0, second.Damage);
        Assert.Equal(hand, world.Seats[0].Hand.Cards.Count);
        game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));
        Assert.Equal(2, first.Damage);
        Assert.Equal(3, second.Damage);
        Assert.Equal(hand + 1, world.Seats[0].Hand.Cards.Count);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static AbilityRunner Program(string extra, bool draw = false) => new(AbilityCatalog.Parse($$"""
        {"cards":[{"card":"01006","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
          "effect":{{(draw ? "{\"seq\":[" + Group + ",{\"draw\":{\"player\":\"you\",\"count\":1}}]}" : Group)}}
        }]}{{extra}}]}
        """));

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Cards = world.Digest().Canonical(), Rng = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, Pending = world.Agenda.Outstanding,
    });
}
