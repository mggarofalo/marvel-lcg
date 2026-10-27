using static Marvel.Content.Tests.Play.SpellMasteryFixture;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic review controls; these do not admit an Invocation ability.</summary>
public sealed class StatusReplacementIndependentReviewTests
{
    [Theory]
    [InlineData("tough", "stunned")]
    [InlineData("tough", "confused")]
    [InlineData("stunned", "tough")]
    [InlineData("stunned", "confused")]
    [InlineData("confused", "tough")]
    [InlineData("confused", "stunned")]
    [Rule("rr:status-cards.1")]
    public void EveryDirectedReplacementKeepsItsChosenHostAndCompletesTheContinuation(
        string oldKind, string newKind)
    {
        // "A character cannot have more than one status card of each type at a time."
        // A different Invocation face deliberately carries this synthetic body.
        string special = """
            {"seq":[{"chooseCard":{"from":{"query":"statusCards"},"effect":{
              "replaceStatus":{"card":"chosen","status":"NEW"}}}},
              {"discardInvocation":"this"}]}
            """.Replace("NEW", newKind, StringComparison.Ordinal);
        var (world, game, deck, _) = BoardWithSpecial(special, false, "09036", 0);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var untouched = Statuses.Give(world, villain, oldKind);
        var host = world.Seats[0].IdentityCard;
        var selected = Statuses.Give(world, host, oldKind);
        int held = world.Seats[1].Hand.Cards.Count;
        long words = world.Random.Generator.WordsConsumed;
        var original = deck.PublicTop!;
        var action = Mastery(game, world.Seats[1].IdentityCard);
        game.Resolve(new Decision(action.Id, [original.ObjectId], []));
        Assert.Equal(1, game.Pending!.Player);
        Assert.Equal(held, world.Seats[1].Hand.Cards.Count);
        var choice = Assert.Single(game.Pending.Affordances,
            offer => offer.AnchorId == selected.ObjectId);
        var result = game.Resolve(Decision.Take(choice.Id));
        Assert.Equal(DeckType.RemovedArea, selected.Area.Type);
        Assert.Equal(DeckType.StatusArea, untouched.Area.Type);
        Assert.Equal(0, Statuses.Count(world, host, oldKind));
        Assert.Equal(1, Statuses.Count(world, host, newKind));
        Assert.Equal(0, Statuses.Count(world, villain, newKind));
        Assert.Equal(held, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        var replacement = result.Events.Where(item => item.Verb == "Replace_Status").ToArray();
        var moved = Assert.Single(replacement.OfType<CardsMoved>());
        Assert.Equal(selected.ObjectId, Assert.Single(moved.Cards).Card);
        var created = Assert.Single(Assert.Single(replacement.OfType<CardsCreated>()).Cards);
        var attached = Assert.Single(replacement.OfType<CardAttached>());
        Assert.Equal(created.Id, attached.Card);
        Assert.Equal(newKind, created.Card);
        Assert.Equal(host.ObjectId, attached.Host);
        Assert.All(replacement, item => Assert.Equal(moved.Trigger, item.Trigger));
        Assert.DoesNotContain(result.Events, item => item.Verb == "Give_Status");
        Assert.Equal(DeckType.AdditionalDiscardPile, original.Area.Type);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void UnsupportedProjectionDoesNotFabricateReplacementAfterCommittedMasteryCost()
    {
        const string special = """
            {"seq":[{"chooseCard":{"from":{"query":"statusCards"},"effect":{
              "then":{"effect":{"replaceStatus":{"card":"chosen","status":"tough"}},
                "then":{"draw":{"player":"you","count":1}}}}}},
              {"discardInvocation":"this"}]}
            """;
        var (world, game, deck, _) = BoardWithSpecial(special, false, "09036", 0);
        var old = Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Stunned);
        var hero = world.Seats[1].IdentityCard;
        var action = Mastery(game, hero);
        var original = deck.PublicTop!;
        int held = world.Seats[1].Hand.Cards.Count;
        var error = Assert.Throws<RulesNotImplementedException>(() =>
            game.Resolve(new Decision(action.Id, [original.ObjectId], [])));
        Assert.Contains("projected resolution", error.Message, StringComparison.Ordinal);
        Assert.False(hero.Ready);
        Assert.Same(original, deck.PublicTop);
        Assert.Equal(DeckType.StatusArea, old.Area.Type);
        Assert.Equal(0, Statuses.Count(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.Equal(held, world.Seats[1].Hand.Cards.Count);
    }

    [Fact]
    [Rule("rr:cannot")]
    public void IndependentlyEmptySentenceSkipsOnlyReplacementOnAnotherSyntheticFace()
    {
        // "Cannot ... is absolute and cannot be countermanded by other abilities or effects."
        const string special = """
            {"seq":[{"chooseCard":{"from":{"query":"statusCards"},"effect":{"choose":{
              "options":[{"replaceStatus":{"card":"chosen","status":"tough"}},
                {"replaceStatus":{"card":"chosen","status":"stunned"}},
                {"replaceStatus":{"card":"chosen","status":"confused"}}]}}}},
              {"discardInvocation":"this"}]}
            """;
        var (world, game, deck, _) = BoardWithSpecial(special, false, "09036", 0);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var selected = Statuses.Give(world, villain, Statuses.Tough);
        foreach (string kind in new[] { Statuses.Stunned, Statuses.Confused })
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                "cannot_be_" + kind, Amount: 1, Affects: villain.ObjectId));
        var original = deck.PublicTop!;
        var action = Mastery(game, world.Seats[1].IdentityCard);
        var result = game.Resolve(new Decision(action.Id, [original.ObjectId], []));
        Assert.Equal(DeckType.StatusArea, selected.Area.Type);
        Assert.DoesNotContain(result.Events, item => item.Verb is "Replace_Status" or "Give_Status");
        Assert.Equal(DeckType.AdditionalDiscardPile, original.Area.Type);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
