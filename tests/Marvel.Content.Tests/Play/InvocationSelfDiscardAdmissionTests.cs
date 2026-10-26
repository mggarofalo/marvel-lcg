using static Marvel.Content.Tests.Play.SpellMasteryFixture;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Nonpublic Invocation target admission with actual Images and synthetic controls.</summary>
public sealed class InvocationSelfDiscardAdmissionTests
{
    private const string Synthetic = """
        {"seq":[{"heal":{"card":"you","amount":1}},{"discardInvocation":"this"}]}
        """;
    private const string Images = """
        {"seq":[
          {"giveStatus":{"card":{"query":"villain"},"status":"confused"}},
          {"removeThreat":{"scheme":{"query":"mainScheme"},"amount":4}},
          {"discardInvocation":"this"}
        ]}
        """;

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("ruling:8c9c9d30f4c01189")]
    public void SingleSchemeImagesEffectsSelfDiscardThroughBothArrowsWhenMainEffectsCannotChangeState(bool master, bool last)
    {
        // "Each Invocation card always has a valid target" includes its own discard.
        // Test-local 09033 follows its printed confuse, remove four, discard instructions.
        // This board has one scheme, so its direct main-scheme binding needs no target choice.
        var (world, game, deck, _) = BoardWithSpecial(Images, last, "09033", master ? 1 : 0);
        var hero = world.Seats[1].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.CreateCard(Statuses.Confused, world.AreaOf(DeckType.StatusArea, host: villain.ObjectId));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("k_threat"));
        var original = deck.PublicTop!;
        var physical = deck.Deck.Cards.Concat(deck.Discard.Cards).Select(card => card.ObjectId).Order().ToArray();
        long random = world.Random.Generator.WordsConsumed;
        var result = game.Resolve(Payment(world, game, hero, original, master));

        Assert.Equal(1, Statuses.Count(world, villain, Statuses.Confused));
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("k_threat"));
        var events = result.Events.ToList();
        int discarded = events.FindIndex(happened => happened is CardsMoved move && move.Verb == "Discard"
            && move.Cards.Any(card => card.Card == original.ObjectId));
        int revealed = events.FindIndex(discarded + 1, happened => happened is CardsFlipped flip && flip.Verb == "Reveal");
        Assert.True(discarded >= 0);
        Assert.True(revealed > discarded);
        Assert.Equal(last ? 1 : 0, events.OfType<AreaReordered>().Count());
        Assert.Equal(last, world.Random.Generator.WordsConsumed > random);
        if (master)
        {
            int returned = events.FindIndex(happened => happened is CardsMoved move && move.Verb == "Return"
                && move.Cards.Any(card => card.Card == original.ObjectId));
            Assert.True(returned > revealed);
            Assert.Same(original, deck.PublicTop);
        }
        else if (!last) Assert.Same(deck.Discard, original.Area);
        Assert.Equal(master, hero.Ready);
        Assert.Equal(physical, deck.Deck.Cards.Concat(deck.Discard.Cards).Select(card => card.ObjectId).Order());
        Assert.Single(deck.Deck.Cards, card => card.FaceUp);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static Decision Payment(World world, Game game, Card hero, Card original, bool master)
    {
        var offer = master ? Assert.Single(game.Pending!.Affordances, item =>
            item.AnchorId == world.Cards.Single(card => card.FaceId == "09005").ObjectId) : Mastery(game, hero);
        int[] sources = [world.Seats[1].Hand.Cards.First(card => card.FaceId == "01088").ObjectId];
        var allocations = ResourcePayment.Allocate(Assert.Single(offer.CostOptions), sources)!;
        return new Decision(offer.Id, [original.ObjectId], sources, Allocations: allocations);
    }

    [Theory]
    [InlineData("top")]
    [InlineData("deeper")]
    [InlineData("discarded")]
    [InlineData("unbound")]
    [InlineData("foreign")]
    public void SyntheticSelfDiscardRequiresTheResolvingPlayersBoundPhysicalTop(string sourceKind)
    {
        var (world, _, deck, runner) = BoardWithSpecial(Synthetic, false);
        var source = deck.PublicTop!;
        int player = 1;
        switch (sourceKind)
        {
            case "deeper": World.MoveToTop(deck.Deck.Cards[0], deck.Deck); break;
            case "discarded": deck.DiscardTop(source, "test", []); break;
            case "unbound": source = world.CreateCard("09036", deck.Deck); break;
            case "foreign": player = 0; break;
        }
        string before = Snapshot(world);
        Assert.Equal(sourceKind == "top", deck.CanDiscardTop(source) && player == deck.Player);

        var result = runner.ExecuteSpecial(world, source, player, finalStep: false);

        if (sourceKind == "top")
        {
            Assert.Same(deck.Discard, source.Area);
            Assert.Contains(result.Events.OfType<CardsMoved>(), moved => moved.Verb == "Discard"
                && moved.Cards.Any(card => card.Card == source.ObjectId));
        }
        else
        {
            Assert.Empty(result.Events);
            Assert.Equal(before, Snapshot(world));
        }
    }
}
