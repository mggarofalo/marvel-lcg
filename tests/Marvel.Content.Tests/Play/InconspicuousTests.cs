using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Events;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class InconspicuousTests
{
    [Theory]
    [InlineData(3, 0)]
    [InlineData(2, 1)]
    [InlineData(1, 2)]
    [InlineData(0, 3)]
    [Rule("rr:thwart")]
    public void AllocateThePrintedTotalAmongSchemes(int mainShare, int sideShare)
    {
        // 04038: "Remove a total of 3 threat from among schemes in play."
        var (game, world, card, main, side) = Begin();
        Play(game, card);
        var offer = Assert.Single(game.Pending!.Affordances);
        Assert.True(offer.Targets!.AllowRepeated);
        var targets = Enumerable.Repeat(main.ObjectId, mainShare)
            .Concat(Enumerable.Repeat(side.ObjectId, sideShare)).ToArray();
        var resolution = game.Resolve(Decision.Take(offer.Id, targets, []));
        Assert.Equal(targets.Distinct().Count(), resolution.Events.OfType<FieldSet>().Count(item => item.Field == "k_threat"));
        Assert.Equal(6 - mainShare, main.Tokens["k_threat"]);
        Assert.Equal(6 - sideShare, side.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.2")]
    public void AvailableThreatCapsTheAllocationAndRejectsOverAssignment()
    {
        // "Check that it has at least one valid target"; printed total 3 does not require 3 threat to exist.
        var (game, world, card, main, side) = Begin(board =>
        {
            board.AreaOf(DeckType.MainSchemesArea).Cards[0].PlaceTokens("k_threat", -5);
            board.AreaOf(DeckType.SideSchemesArea).Cards[0].PlaceTokens("k_threat", -5);
        });
        Play(game, card);
        var offer = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(2, offer.Targets!.Min);
        string before = world.Digest().Canonical();
        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(offer.Id, [main.ObjectId, main.ObjectId], [])));
        Assert.Equal(before, world.Digest().Canonical());
        game.Resolve(Decision.Take(offer.Id, [main.ObjectId, side.ObjectId], []));
        Assert.Equal(0, main.Tokens["k_threat"]);
        Assert.Equal(DeckType.EncounterDiscardPile, side.Area.Type);
    }

    [Fact]
    [Rule("rr:crisis-icon.1")]
    public void CrisisExcludesMainSchemeEvenWhenItsRemovalWouldBeEarlier()
    {
        // "Player cards cannot remove threat from the main scheme."
        var (game, _, card, main, _) = Begin(board =>
        {
            var crisis = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            crisis.PlaceTokens("k_threat", 1);
        });
        Play(game, card);
        Assert.DoesNotContain(main.ObjectId, Assert.Single(game.Pending!.Affordances).Targets!.Legal);
    }

    [Fact]
    [Rule("rr:confuse-confused.1")]
    public void ConfusedCancelsTheWholeThwart()
    {
        // A confused character discards Confused instead of thwarting.
        var (game, world, card, main, side) = Begin(board =>
            Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Confused));
        Play(game, card);
        var offer = Assert.Single(game.Pending!.Affordances);
        Assert.Contains("Confused cancels", offer.Description);
        game.Resolve(Decision.Take(offer.Id, [main.ObjectId, main.ObjectId, side.ObjectId], []));
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
        Assert.Equal(6, main.Tokens["k_threat"]);
        Assert.Equal(6, side.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
    }

    [Fact]
    [Rule("rr:modifiers")]
    public void EventModifierIncreasesTheTotalOnce()
    {
        // Recalculate from "the unmodified base value and all active modifiers."
        var (game, _, card, main, side) = Begin(board =>
        {
            var source = board.Seats[0].Hand.Cards.Single(item => item.FaceId == "04038");
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                "eventThreatRemoval", Amount: 2, Card: source.ObjectId, Affects: source.ObjectId));
        });
        Play(game, card);
        var offer = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(5, offer.Targets!.Min);
        game.Resolve(Decision.Take(offer.Id, [main.ObjectId, main.ObjectId, side.ObjectId, side.ObjectId, side.ObjectId], []));
        Assert.Equal(4, main.Tokens["k_threat"]);
        Assert.Equal(3, side.Tokens["k_threat"]);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.2")]
    public void NoRemovalTargetDoesNotOfferTheEvent()
    {
        // "If the card or ability does not have at least one valid target, it cannot be played or initiated."
        var (game, _, card, _, _) = Begin(board =>
        {
            board.AreaOf(DeckType.MainSchemesArea).Cards[0].PlaceTokens("k_threat", -6);
            board.AreaOf(DeckType.SideSchemesArea).Cards[0].PlaceTokens("k_threat", -6);
        });
        Assert.DoesNotContain(game.Pending!.Affordances, item => item.AnchorId == card.ObjectId);
    }

    private static void Play(Game game, Card card)
    {
        var offer = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == card.ObjectId);
        var source = offer.CostOptions[0].Sources![0];
        var allocation = ResourcePayment.Allocate(offer.CostOptions[0], [source.Effect]);
        Assert.NotNull(allocation);
        game.Resolve(Decision.Take(offer.Id, [], [source.Effect], new Dictionary<string, long>(), allocation));
    }

    private static (Game Game, World World, Card Event, Card Main, Card Side) Begin(Action<World>? prepare = null)
    {
        Card? card = null;
        Card? side = null;
        var runner = AuthoredCards.Runner();
        var (game, world) = Playing(board =>
        {
            card = board.CreateCard("04038", board.Seats[0].Hand);
            side = board.CreateCard("01107", board.AreaOf(DeckType.SideSchemesArea));
            side.PlaceTokens("k_threat", 6);
            board.AreaOf(DeckType.MainSchemesArea).Cards[0].PlaceTokens("k_threat", 6);
            prepare?.Invoke(board);
        }, hero: true, abilities: runner);
        return (game, world, card!, world.AreaOf(DeckType.MainSchemesArea).Cards[0], side!);
    }

}

