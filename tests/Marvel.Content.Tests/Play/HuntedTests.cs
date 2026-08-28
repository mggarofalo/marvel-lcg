using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Hunted, and the first cost in this dataset that is not resources.
/// </summary>
/// <remarks>
/// <para>
/// "<b>Alter-Ego Action</b>: Discard a card from your hand → discard this
/// card." An obligation, so <c>rr:reveal.4</c> puts it into the revealing
/// player's play area and it stays there. Its printed hazard icon is a field
/// the engine already reads, and this one sentence is the whole way out.
/// </para>
/// <para>
/// <b>Discarding a card is not spending a resource.</b> <c>rr:cost.3</c> spends
/// resources "by discarding cards from their hand to generate the resource or
/// resources indicated at the bottom-left corner of the card" — the letters are
/// what is spent and the discard is how. This cost reads no letters at all: a
/// card printing no <c>RES</c> pays it, and a card printing two does not pay
/// twice. So the payment is a <i>card the player chose</i> rather than a number
/// they met, which is why it travels as a target and not in
/// <c>Decision.Resources</c>. <c>rr:initiating-abilities</c> keeps step 2's
/// choosing and step 5's paying in different steps, and the answer carries them
/// separately for the same reason.
/// </para>
/// </remarks>
public sealed class HuntedTests
{
    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:player-turn.5")]
    [Rule("rr:player-s-play-area.4")]
    [Rule("rr:villain-s-play-area.4")]
    [Fact]
    public void TheObligationIsOfferedAndAsksWhichCardPaysForIt()
    {
        // An obligation given to a player is in that player's play area and
        // not in the villain's play area, even though the scenario owns it.
        // A cost with a choice in it, described rather than decided: the
        // affordance carries the hand and a count of exactly one, so the client
        // can put the question and the engine never picks.
        var (game, world) = Playing(out var hunted);
        Assert.Equal(PlayArea.Of(0), hunted.Area.PlayArea);

        var action = Assert.Single(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
        var asking = Assert.IsType<TargetRequest>(action.Targets);

        Assert.Equal(1, asking.Min);
        Assert.Equal(1, asking.Max);
        Assert.Equal(
            [.. world.Seats[0].Hand.Cards.Select(card => card.ObjectId).Order()],
            asking.Legal.Order());

        // Not a resource cost, and the affordance says so by carrying no price.
        Assert.Null(action.Costs);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:cost-arrow-icon.1")]
    [Fact]
    public void PayingDiscardsTheChosenCardAndThenTheObligation()
    {
        // `rr:cost-arrow-icon`: the text before the arrow "must be paid and/or
        // resolved in full before the text after the cost arrow icon can be
        // resolved". Both cards end in a discard pile, and they are not the
        // same discard pile — the obligation is an encounter card.
        var (game, world) = Playing(out var hunted);
        var paid = world.Seats[0].Hand.Cards[0];

        var action = Assert.Single(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
        game.Resolve(Decision.Take(action.Id, [paid.ObjectId], []));

        Assert.Equal(DeckType.DiscardPile, paid.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, hunted.Area.Type);
    }

    [Rule("rr:cost.3")]
    [Fact]
    public void ACardThatGeneratesNoResourceAtAllStillPaysIt()
    {
        // The distinction the whole cost turns on. An identity card generates
        // nothing — it has no printed `RES` — so a resource cost of one could
        // not be paid with it and this one can, because what is spent is the
        // card and not what the card would have made.
        var (game, world) = Playing(out var hunted);
        var paid = world.CreateCard(NoResource, world.Seats[0].Hand);
        Assert.Equal(string.Empty, Resources.GeneratedBy(paid.FaceId, Cards));

        var action = Assert.Single(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
        game.Resolve(Decision.Take(action.Id, [paid.ObjectId], []));

        Assert.Equal(DeckType.EncounterDiscardPile, hunted.Area.Type);
    }

    [Rule("rr:initiating-abilities.step.3")]
    [Fact]
    public void AnEmptyHandIsNotOfferedIt()
    {
        // Step 3 asks the cost and the player's ability to pay it together, and
        // only "if both conditions are met" do the later steps happen. A player
        // holding nothing has no way out of this card until they draw one.
        var (_, world) = Playing(out var hunted);
        foreach (var held in world.Seats[0].Hand.Cards.ToList())
        {
            World.MoveToTop(
                held,
                world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        }

        Assert.DoesNotContain(
            AuthoredCards.Runner().Actions(world, 0), ability => ability.Card == hunted.ObjectId);
    }

    [Rule("rr:player-turn.5.1")]
    [Fact]
    public void AHeroIsNotOfferedAnAlterEgoAction()
    {
        // "If the action ability is preceded by **Hero** or **Alter-Ego**, the
        // player must be in the specified form in order to trigger the
        // ability." The obligation is in the same place either way; only the
        // identity card is turned over.
        var (game, _) = Playing(out _, hero: true);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Theory]
    // Nothing chosen, and one card asked for.
    [InlineData(0)]
    // Two chosen, and one card asked for. `rr:cost.4` permits generating beyond
    // a *resource* cost; a cost of one card is a cost of one card.
    [InlineData(2)]
    public void APaymentThatIsNotTheCostIsRefusedRatherThanTrimmed(int cards)
    {
        var (game, world) = Playing(out _);
        var action = Assert.Single(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);

        var refused = Assert.Throws<RulesNotImplementedException>(() => game.Resolve(
            Decision.Take(
                action.Id,
                [.. world.Seats[0].Hand.Cards.Take(cards).Select(card => card.ObjectId)],
                [])));

        Assert.Contains("costs 1 card(s) from hand", refused.Message, StringComparison.Ordinal);
    }

    [Rule("rr:cost.3")]
    [Fact]
    public void ACardThatIsNotInTheHandCannotPayIt()
    {
        // "Discard a card **from your hand**." A card in play is a card, and
        // the phrase is about where it is — so naming one is refused rather
        // than quietly discarding it from where it sits.
        var (game, world) = Playing(out _);
        var action = Assert.Single(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);

        var refused = Assert.Throws<RulesNotImplementedException>(() => game.Resolve(
            Decision.Take(action.Id, [world.Seats[0].IdentityCard.ObjectId], [])));

        Assert.Contains("cannot be discarded from it", refused.Message, StringComparison.Ordinal);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void TheSameCostInAWindowIsPaidFromTheSameAnswer()
    {
        // Hunted's is an action, and an action and a window are two ways into
        // `Pay`. The seam is `Sequence.Answer`, which passes the answer's chosen
        // cards on exactly as it passes the resources — so a response that costs
        // a card is paid for, and this is the ability that says so before a
        // printed card needs it.
        var world = Dealt(hero: true);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var abilities = new Marvel.Cards.Run.AbilityRunner(Marvel.Cards.Dsl.AbilityCatalog.Parse(
            $$"""
            { "cards": [ { "card": "{{villain.FaceId}}", "abilities": [ {
                "trigger": { "event": "WhenAttackInitiated", "timing": "Response",
                             "actor": "you", "target": "this" },
                "cost": { "discardFromHand": 1 },
                "effect": { "placeThreat": { "scheme": { "titled": "Gene Pool" },
                                             "amount": 1 } }
            } ] } ] }
            """));

        world.Abilities = abilities;
        var pool = world.Cards.First(card => card.FaceId == AuthoredCards.GenePool);
        long before = pool.Tokens.GetValueOrDefault("k_threat");
        var paid = world.Seats[0].Hand.Cards[0];

        var events = new List<Marvel.Rules.Events.GameEvent>();
        BasicPowers.BasicAttack(world, Cards, 0, villain, events);
        var asked = Sequence.Work(world, Cards, abilities, events);

        Assert.NotNull(asked);
        var offer = Assert.Single(asked.Affordances);
        Assert.Equal([paid.ObjectId], offer.Targets!.Legal.Where(id => id == paid.ObjectId));

        Sequence.Answer(
            world, Cards, abilities, asked, Decision.Take(offer.Id, [paid.ObjectId], []), events);
        Sequence.Finish(world, Cards, abilities, events);

        Assert.Equal(DeckType.DiscardPile, paid.Area.Type);
        Assert.Equal(before + 1, pool.Tokens.GetValueOrDefault("k_threat"));
    }

    /// <summary>Spider-Man's identity card, which prints no <c>RES</c>.</summary>
    private const string NoResource = AuthoredCards.SpiderMan;

    /// <summary>
    /// A game past the mulligan, on the first player's turn, with Hunted in
    /// their play area.
    /// </summary>
    /// <remarks>
    /// The obligation is put where <c>rr:reveal.4</c> puts it rather than
    /// revealed, because what is being tested is the ability on a card already
    /// in play — the reveal has its own tests.
    /// </remarks>
    private static (Game Game, World World) Playing(
        out Card hunted, bool hero = false, Action<World>? prepare = null)
    {
        var world = Dealt(hero);

        hunted = world.CreateCard(
            AuthoredCards.Hunted, world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)));

        prepare?.Invoke(world);

        var game = Game.Begin(world, Cards, AuthoredCards.Runner());
        while (game.Pending is { } asked
            && asked.Affordances.Any(option => option.Verb == Game.ResolveMulligans))
        {
            game.Resolve(Decision.Decline);
        }

        return (game, world);
    }

    /// <summary>The dealt Unus board, before the game begins.</summary>
    private static World Dealt(bool hero)
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "unus", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345,
            AuthoredCards.Runner());

        if (hero)
        {
            world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        }

        return world;
    }
}
