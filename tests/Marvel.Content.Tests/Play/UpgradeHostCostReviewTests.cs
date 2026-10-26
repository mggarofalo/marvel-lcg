using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nonpublic pricing probes: only Iron Man's constant and attachment metadata are authored here.
public sealed class UpgradeHostCostReviewTests
{
    [Fact]
    [Rule("rr:initiating-abilities.step.3")]
    public void OnlyTheDiscountedHostIsOfferedWhenTheOrdinaryHostCannotBePaid()
    {
        // "Determine ... the player's ability to pay them, taking modifiers into account."
        Card? iron = null, ordinary = null, upgrade = null;
        var (game, world) = Playing(board =>
        {
            EmptyHand(board);
            iron = Ally(board, "09039", 1);
            ordinary = Ally(board, "01067", 0);
            upgrade = board.CreateCard("01074", board.Seats[0].Hand);
        }, heroes: ["captain_marvel", "spider_man"], abilities: Runner());
        // Setup may refill a short prepared hand. Establish affordability on the live board.
        foreach (var card in world.Seats[0].Hand.Cards.Where(card => card != upgrade).ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        game.Resolve(new Decision(Assert.Single(game.Pending!.Affordances,
            option => option.Verb == Game.ChangeForm).Id, [], []));
        var offer = Assert.Single(game.Pending!.Affordances,
            option => option.Verb == CardPlay.Verb && option.AnchorId == upgrade!.ObjectId);
        Assert.Equal([iron!.ObjectId], offer.Targets!.Legal);
        var cost = Assert.Single(offer.CostOptions);
        Assert.Equal(iron.ObjectId, cost.Target);
        Assert.Equal("0", cost.Cost);
        Assert.Null(CardPayment.Price(world, world.Facts, world.Seats[0], upgrade!, ordinary!.ObjectId));
        game.Resolve(new Decision(offer.Id, [iron.ObjectId], []));
        Assert.Equal(iron.ObjectId, upgrade!.Area.Host);
        Assert.Equal(1, upgrade.Area.PlayArea.Player);
        Assert.Equal(0, upgrade.Owner);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrdinaryAttachmentKeepsItsAnchorPriceAndPaysTheChosenPhysicalHost(bool secondHost)
    {
        Card? first = null, second = null, upgrade = null, resource = null;
        var (game, world) = Playing(board =>
        {
            EmptyHand(board);
            first = Ally(board, "01067", 0);
            second = Ally(board, "01070", 1);
            upgrade = board.CreateCard("01074", board.Seats[0].Hand);
            resource = board.CreateCard("01089", board.Seats[0].Hand);
        }, heroes: ["captain_marvel", "spider_man"], abilities: Runner());
        var offer = Assert.Single(game.Pending!.Affordances,
            option => option.Verb == CardPlay.Verb && option.AnchorId == upgrade!.ObjectId);
        var cost = Assert.Single(offer.CostOptions);
        Assert.Equal(upgrade!.ObjectId, cost.Target);
        Assert.Equal("1", cost.Cost);
        Assert.Equal([first!.ObjectId, second!.ObjectId], offer.Targets!.Legal);
        int host = secondHost ? second.ObjectId : first.ObjectId;
        Assert.False(DecisionAssessment.Assess(game.Pending, new Decision(offer.Id, [host], [])).IsAccepted);
        var allocation = ResourcePayment.Allocate(cost, [resource!.ObjectId]);
        Assert.NotNull(allocation);
        game.Resolve(Decision.Take(offer.Id, [host], [resource.ObjectId], new Dictionary<string, long>(), allocation));
        Assert.Equal(host, upgrade.Area.Host);
        Assert.Equal(secondHost ? 1 : 0, upgrade.Area.PlayArea.Player);
        Assert.Equal(DeckType.DiscardPile, resource.Area.Type);
        Assert.Equal(0, resource.Area.PlayArea.Player);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void AnUnpaidLiveCostChangeRejectsEvenWhenThePublishedHostRemainsEligible()
    {
        // "abort this process without paying any costs."
        Card? iron = null, upgrade = null;
        var (game, world) = Playing(board =>
        {
            EmptyHand(board);
            iron = Ally(board, "09039", 0);
            upgrade = board.CreateCard("01074", board.Seats[0].Hand);
        }, heroes: ["captain_marvel"], abilities: Runner());
        var prompt = game.Pending!;
        var offer = Assert.Single(prompt.Affordances, option => option.AnchorId == upgrade!.ObjectId);
        var answer = new Decision(offer.Id, [iron!.ObjectId], []);
        Assert.True(DecisionAssessment.Assess(prompt, answer).IsAccepted);
        // Synthetic direct blank exercises the live-price boundary; no playable ally-blanking card is claimed.
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, PrintedText.Blanked,
            Card: iron.ObjectId, Affects: iron.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfPhase))
        { TargetIncarnation = iron.Incarnation });
        Assert.Contains(iron.ObjectId, CardPlayLegality.LegalAttachmentTargets(
            world, world.Facts, world.Seats[0], upgrade!, world.CardPlayAbilities)!);
        Assert.Equal(1, CardPayment.CostOf(world, world.Facts, world.Seats[0], upgrade!, iron.ObjectId).Amount);
        string digest = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var agenda = world.Agenda.Outstanding.ToArray();
        var effects = world.Effects.Registered.ToArray();
        Assert.Throws<ArgumentException>(() => game.Resolve(answer));
        Assert.Same(prompt, game.Pending);
        Assert.Same(world.Seats[0].Hand, upgrade!.Area);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(agenda, world.Agenda.Outstanding);
        Assert.Equal(effects, world.Effects.Registered);
    }

    private static void EmptyHand(World world)
    {
        foreach (var card in world.Seats[0].Hand.Cards.ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
    }

    private static Card Ally(World world, string face, int player) =>
        world.CreateCard(face, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player));

    private static AbilityRunner Runner() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"09039","abilities":[{
          "trigger":{"timing":"Constant","subject":"this"},"effect":{"reduceUpgradeCostOnThis":1}
        }]},{"card":"01074","attachTo":{"query":"allies"},"abilities":[]},
        {"card":"01067","abilities":[]},{"card":"01070","abilities":[]}]}
        """));
}
