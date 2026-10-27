using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BoundedDamageRecipientTests
{
    private const string Effect = """{"dealDamage":{"cards":{"query":"enemies"},"amount":3,"maximum":3}}""";

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [Rule("rr:choose-game-element.3.1")]
    [Rule("rr:target.3.6")]
    public void OneDistinctGroupDamagesOnlyChosenEnemiesWithoutAttacking(int count)
    {
        // "The same target cannot be chosen multiple times this way."
        // "Damage that is dealt but not taken ... is considered to affect a target."
        // Synthetic program models printed 10005, without admitting the Hulk pack.
        var (game, world, source, targets) = Board();
        Statuses.Give(world, targets[1], Statuses.Tough);
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Stunned);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "retaliate",
            Amount: 2, Affects: targets[0].ObjectId));
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source.ObjectId);
        var start = game.Resolve(Decision.Take(action.Id));
        var offer = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(0, offer.Targets!.Min);
        Assert.Equal(3, offer.Targets.Max);
        Assert.False(offer.Targets.AllowRepeated);
        Assert.Contains(world.TheCardIn(DeckType.VillainArea)!.ObjectId, offer.Targets.Legal);
        Assert.Contains(targets[2].ObjectId, offer.Targets.Legal);
        Assert.DoesNotContain(world.Seats[0].IdentityCard.ObjectId, offer.Targets.Legal);
        Assert.All(targets, target => Assert.Equal(0, target.Damage));
        var selected = targets.Take(count).Select(card => card.ObjectId).Reverse().ToArray();
        var end = game.Resolve(new Decision(offer.Id, selected));
        Assert.Equal(count > 0 ? 3 : 0, targets[0].Damage);
        Assert.Equal(0, targets[1].Damage);
        Assert.Equal(count == 3 ? 3 : 0, targets[2].Damage);
        Assert.Equal(count < 2, Statuses.Has(world, targets[1], Statuses.Tough));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.DoesNotContain(start.Events.Concat(end.Events), item => item.Verb == "Attack");
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("tooMany")]
    [InlineData("stale")]
        [InlineData("wrongOffer")]
    public void InvalidOrStaleGroupLeavesRngPromptAndPendingResolutionUntouched(string kind)
    {
        var (game, world, source, targets) = Board();
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            item => item.AnchorId == source.ObjectId).Id));
        var offer = Assert.Single(game.Pending!.Affordances);
        if (kind == "stale") World.MoveToTop(targets[0], world.AreaOf(DeckType.EncounterDiscardPile));
        int[] selected = kind switch
        {
            "duplicate" => [targets[0].ObjectId, targets[0].ObjectId],
            "tooMany" => offer.Targets!.Legal.Take(4).ToArray(),
            _ => [targets[0].ObjectId],
        };
        string before = Snapshot(world);
        Assert.ThrowsAny<ArgumentException>(() => game.Resolve(new Decision(
            kind == "wrongOffer" ? -9 : offer.Id, selected)));
        Assert.Equal(before, Snapshot(world));
        Assert.False(game.IsRootPrompt);
        game.Resolve(new Decision(offer.Id, []));
        Assert.True(game.IsRootPrompt);
    }

    [Fact]
    public void QueriesAndSelectionOrderMatchAnUnprobedControl()
    {
        var (game, world, source, targets) = Board();
        var (controlGame, control, controlSource, controlTargets) = Board();
        string before = Snapshot(world);
        for (int i = 0; i < 3; i++)
            foreach (var action in ((AbilityRunner)world.Abilities).Actions(world, 0))
                _ = ((AbilityRunner)world.Abilities).Describe(world, action);
        Assert.Equal(before, Snapshot(world));
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            item => item.AnchorId == source.ObjectId).Id));
        controlGame.Resolve(Decision.Take(Assert.Single(controlGame.Pending!.Affordances,
            item => item.AnchorId == controlSource.ObjectId).Id));
        before = Snapshot(world);
        var offer = Assert.Single(game.Pending!.Affordances);
        for (int i = 0; i < 3; i++)
        {
            _ = ((AbilityRunner)world.Abilities).Choosing(world, world.Agenda.Current!.Value);
            _ = offer.Targets!.Allows([targets[0].ObjectId]);
        }
        Assert.Equal(before, Snapshot(world));
        var actual = game.Resolve(new Decision(offer.Id, targets.Select(card => card.ObjectId).Reverse().ToArray()));
        var expected = controlGame.Resolve(new Decision(Assert.Single(controlGame.Pending!.Affordances).Id,
            controlTargets.Select(card => card.ObjectId).ToArray()));
        Assert.Equal(Snapshot(control), Snapshot(world));
        Assert.Equal(JsonSerializer.Serialize(expected.Events), JsonSerializer.Serialize(actual.Events));
    }

    [Fact]
    [Rule("rr:damage.step.6")]
    public void EveryPlacementPrecedesSuspendedDefeatAndLaterDrawRunsOnce()
    {
        // Damage step 6 opens "would be defeated" after step 5 places damage.
        var runner = DamageContinuationRunner(Effect);
        Card? source = null;
        Card? first = null;
        Card? second = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            InPlay(board, "01092");
            first = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            first.TakeDamage(Damage.Health(board, board.Facts, first) - 3);
            second = board.CreateCard("01182", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        int hand = world.Seats[0].Hand.Cards.Count;
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            item => item.AnchorId == source!.ObjectId).Id));
        game.Resolve(new Decision(Assert.Single(game.Pending!.Affordances).Id, [first!.ObjectId, second!.ObjectId]));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(3, second.Damage);
        Assert.Equal(hand, world.Seats[0].Hand.Cards.Count);
        game.Resolve(Decision.Decline);
        Assert.Equal(DeckType.EncounterDiscardPile, first.Area.Type);
        Assert.Equal(3, second.Damage);
        Assert.Equal(hand + 1, world.Seats[0].Hand.Cards.Count);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("\"three\"")]
    public void LoweringRejectsInvalidRecipientBounds(string maximum)
    {
        var error = Assert.Throws<AbilityException>(() => Runner("01006", "Action",
            Effect.Replace("\"maximum\":3", $"\"maximum\":{maximum}", StringComparison.Ordinal)));
        Assert.Contains("maximum", error.Message);
    }

    [Theory]
    [InlineData("\"amount\":0")]
    [InlineData("\"amount\":{\"query\":\"cardsDiscarded\"}")]
    [InlineData("\"amount\":3,\"attack\":1")]
    public void LoweringRejectsUnsupportedGroupedDamageShapes(string amount)
    {
        Assert.Throws<AbilityException>(() => Runner("01006", "Action",
            Effect.Replace("\"amount\":3", amount, StringComparison.Ordinal)));
    }

    private static (Game Game, World World, Card Source, Card[] Targets) Board()
    {
        var runner = Runner("01006", "Action", Effect);
        Card? source = null;
        Card[] targets = [];
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            targets = [board.CreateCard("01182", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))),
                board.CreateCard("01182", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))),
                board.CreateCard("01182", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)))];
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        return (game, world, source!, targets);
    }

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Cards = world.Digest().Canonical(), Rng = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, Current = world.Agenda.Current,
        Pending = world.Agenda.Outstanding,
    });
}
