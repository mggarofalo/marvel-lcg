using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Cards;

/// <summary>
/// The ability dataset, held against the engine it is written for.
/// </summary>
/// <remarks>
/// <para>
/// A card is data now, and data has a failure mode compiled code does not: it
/// can be wrong in a way that produces no error and no behaviour. A trigger
/// naming a condition nothing fires, or a printed id that is a typo, sits in the
/// file looking implemented for ever.
/// </para>
/// <para>
/// So the dataset is held against two things it cannot contradict — the
/// conditions the engine's steps actually produce, and the printed cards that
/// actually exist.
/// </para>
/// </remarks>
public sealed class AbilityDataTests
{
    private static readonly CardCatalog Printed =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void EveryTriggerNamesAConditionTheEngineActuallyProduces()
    {
        // The failure this exists for: an ability whose trigger is spelled
        // `WhenEnemyAttack` sits in the dataset, parses, validates, and never
        // fires. Nothing else in the suite would notice, because "a card that
        // does nothing" and "a card that was never reached" look identical.
        foreach (var ability in AuthoredCards.Book.Abilities)
        {
            Assert.True(
                Steps.EveryCondition.Contains(ability.Trigger.Event),
                $"'{ability.Card}' triggers on '{ability.Trigger.Event}', which no step "
                + $"produces. The engine's conditions are: "
                + string.Join(", ", Steps.EveryCondition.Order(StringComparer.Ordinal)));
        }
    }

    [Rule("rr:stalwart.1")]
    [Fact]
    public void ACardGivingAStatusCannotRouteRoundTheStatusRules()
    {
        // A card's ability is data, and the interpreter runs it -- but it runs
        // it *through* the rules. `rr:stalwart.1` says a stalwart character
        // "cannot have confused or stunned status cards", and an ability
        // reaching straight at `Statuses.Give` would put one there anyway.
        //
        // `01094` Rhino is not stalwart in the printed data, so the target here
        // is given the keyword on the board rather than in the dataset -- what
        // is being tested is the interpreter's route, not a card.
        var book = AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "01105", "name": "test", "abilities": [ {
                "name": "test",
                "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
                "effect": { "giveStatus": { "card": { "query": "villain" },
                                        "status": "stunned" } }
            } ] } ] }
            """);

        var world = new World(Printed, players: 1);
        world.CreateSeat("p0");
        var rhino = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var card = world.CreateCard(AuthoredCards.ImTough, world.AreaOf(DeckType.RevealingArea));
        var runner = new Marvel.Cards.Run.AbilityRunner(book);

        runner.WhenRevealed(world, card, 0);
        Assert.Equal(1, Statuses.Count(world, rhino, Statuses.Stunned));

        // Stalwart, granted the way a card ability grants a keyword. The stun
        // already there stays -- `rr:stalwart.2` removes existing cards and is
        // a separate clause -- but no second one lands.
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, Kind: "stalwart", Amount: 1,
            Card: rhino.ObjectId, Affects: rhino.ObjectId));

        foreach (var existing in Statuses.On(world, rhino, Statuses.Stunned).ToList())
        {
            Discard.Card(world, existing, "test", []);
        }

        runner.WhenRevealed(world, card, 0);

        Assert.Equal(0, Statuses.Count(world, rhino, Statuses.Stunned));
    }

    [Fact]
    public void EveryAuthoredCardIsAPrintedCard()
    {
        // The other typo. `01O99` parses as happily as `01099`.
        foreach (string card in AuthoredCards.Book.Authored)
        {
            Assert.True(
                Printed.Kind(card) != CardKind.Unknown,
                $"'{card}' is authored and is not a printed card id");
        }
    }

    [Fact]
    public void EveryAbilityIsWaitingInAWindowOrIsTheOccurrence()
    {
        // A timing that is neither an interrupt, a response, nor the occurrence
        // itself would put the ability in no tier at all -- `AbilityWindow`
        // would drop it and nothing would ever offer it.
        foreach (var ability in AuthoredCards.Book.Abilities)
        {
            var timing = ability.Trigger.Timing;
            Assert.True(
                AbilityTypes.IsInterrupt(timing)
                || AbilityTypes.IsResponse(timing)
                || AbilityTypes.PriorityOf(timing) == TimingPriority.Occurrence,
                $"'{ability.Card}' has timing '{timing}', which sits in no window");
        }
    }

    [Fact]
    public void AnUnauthoredCardSaysSoRatherThanDoingNothing()
    {
        // The property that makes an incomplete card pool safe. A revealed card
        // nobody has read must not resolve to silence, because a silent encounter
        // card produces a board that is plausible and wrong.
        var world = new World(Printed, players: 1);
        world.CreateSeat("p0");
        var card = world.CreateCard("01100", world.AreaOf(DeckType.RevealingArea));

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => AuthoredCards.Runner().WhenRevealed(world, card, 0));
        Assert.Contains("no ability data", thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    // Everything unknown is refused rather than ignored. A lenient reader
    // accepts a card and does three quarters of what it says, and nothing
    // downstream can tell.
    [InlineData("""{"cards":[{"card":"01099","wibble":1}]}""", "wibble")]
    [InlineData("""{"cards":[{"card":"01099"},{"card":"01099"}]}""", "twice")]
    [InlineData("""{"nope":[]}""", "no 'cards' array")]
    [InlineData("""{"cards":[{"card":"01099","abilities":[{"effect":{"seq":[]}}]}]}""", "no 'trigger'")]
    [InlineData("""{"cards":[{"card":"01099","abilities":[{"trigger":{"event":"WhenEnemyAttacks","timing":"Interrupt","subject":"nobody"},"effect":{"seq":[]}}]}]}""", "nobody")]
    [InlineData("""{"cards":[{"card":"01099","abilities":[{"trigger":{"event":"WhenEnemyAttacks","timing":"Shouting","subject":"this"},"effect":{"seq":[]}}]}]}""", "Shouting")]
    [InlineData("""{"cards":[{"card":"01099","abilities":[{"trigger":{"event":"WhenEnemyAttacks","timing":"Interrupt","subject":"this"}}]}]}""", "no 'effect'")]
    public void TheReaderRefusesWhatItDoesNotUnderstand(string json, string says)
    {
        var thrown = Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(json));
        Assert.Contains(says, thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ANodeWithTwoKindsIsRefused()
    {
        // The one-key rule is what makes a node's kind unambiguous. Two keys
        // would make the second one's fate arbitrary, and a card that quietly
        // lost half an effect is exactly the failure data has and code does not.
        var thrown = Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(
            """
            {"cards":[{"card":"01099","abilities":[{
              "trigger":{"event":"WhenEnemyAttacks","timing":"Interrupt","subject":"this"},
              "effect":{"discard":"this","draw":1}}]}]}
            """));
        Assert.Contains("is not a node", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnEffectNodeNothingImplementsThrowsNamingTheNode()
    {
        // How the vocabulary grows: a card names a node, the engine says which
        // node it has not got, somebody implements that one node. That is a
        // different activity from adding a card, and it should read differently
        // in a stack trace.
        var world = new World(Printed, players: 1);
        world.CreateSeat("p0");
        var card = world.CreateCard("01105", world.AreaOf(DeckType.RevealingArea));

        var book = AbilityCatalog.Parse(
            """
            {"cards":[{"card":"01105","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"summonCthulhu":1}}]}]}
            """);

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => new Marvel.Cards.Run.AbilityRunner(book).WhenRevealed(world, card, 0));
        Assert.Contains("summonCthulhu", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:ability.step.3")]
    [Fact]
    public void RevealingACardRunsItsWhenRevealedAndNotItsInterrupts()
    {
        // "When Revealed" *is* the occurrence, not a window around it. A card
        // may carry both — an interrupt to a reveal is a different ability at a
        // different tier — and matching on the condition alone would run it
        // here as well as in the window that is meant to offer it.
        var world = new World(Printed, players: 1);
        world.CreateSeat("p0");
        var card = world.CreateCard("01105", world.AreaOf(DeckType.RevealingArea));

        var book = AbilityCatalog.Parse(
            """
            {"cards":[{"card":"01105","abilities":[
              {"trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
               "effect":{"giveStatus":{"card":"this","status":"tough"}}},
              {"trigger":{"event":"WhenCardRevealed","timing":"Interrupt","subject":"this"},
               "effect":{"giveStatus":{"card":"this","status":"stunned"}}}]}]}
            """);

        new Marvel.Cards.Run.AbilityRunner(book).WhenRevealed(world, card, 0);

        Assert.True(Statuses.Has(world, card, "tough"));
        Assert.False(Statuses.Has(world, card, "stunned"));
    }

    [Rule("rr:ability.8")]
    [Rule("rr:interrupt.1")]
    [Fact]
    public void WhoControlsAnAbilityIsWhoOwnsTheCard()
    {
        // "Players can only trigger interrupt abilities on cards they control
        // or on encounter cards", and any player may use the latter. So an
        // ability on a scenario-owned card has no controller, and one on a
        // player's card belongs to that player — neither is something the card
        // data says, and neither is the seat that happens to be first.
        var world = new World(Printed, players: 2);
        world.CreateSeat("p0");
        world.CreateSeat("p1");
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var identity = world.CreateCard("01001a", world.Seats[1].Hero);

        var book = AbilityCatalog.Parse(
            """
            {"cards":[
              {"card":"01094","abilities":[{"trigger":{"event":"WhenEnemyAttacks",
                "timing":"Interrupt","subject":"this"},"effect":{"seq":[]}}]},
              {"card":"01001a","abilities":[{"trigger":{"event":"WhenEnemyAttacks",
                "timing":"Interrupt","subject":"this"},"effect":{"seq":[]}}]}]}
            """);
        var runner = new Marvel.Cards.Run.AbilityRunner(book);

        var onEncounter = new Occurrence(
            1, [Steps.EnemyAttacks], Subject: villain.ObjectId, Player: 0);
        var onPlayerCard = new Occurrence(
            2, [Steps.EnemyAttacks], Subject: identity.ObjectId, Player: 0);

        Assert.Equal(
            World.Scenario,
            Assert.Single(runner.Waiting(world, onEncounter, WindowKind.Interrupt)).Player);
        Assert.Equal(
            1,
            Assert.Single(runner.Waiting(world, onPlayerCard, WindowKind.Interrupt)).Player);
    }

    [Fact]
    public void TheAuthoredCardsAreTheOnesTheTestsName()
    {
        // Stated as a set so that a card added to the dataset is a deliberate
        // act with a test behind it, not something that accumulates. The rule
        // this file is under: a card is authored when something reaches it.
        Assert.Equal(
            [
                AuthoredCards.SpiderMan, AuthoredCards.Charge, AuthoredCards.Shocker,
                AuthoredCards.HardToKeepDown, AuthoredCards.ImTough,
                AuthoredCards.BreakinAndTakin, AuthoredCards.BombScare, AuthoredCards.FalseAlarm,
                AuthoredCards.Advance, AuthoredCards.Assault, AuthoredCards.GangUp,
            ],
            AuthoredCards.Book.Authored.Order(StringComparer.Ordinal));
    }
}
