using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>The “Each Time” alteration on You Dare Oppose Me?.</summary>
public sealed class YouDareOpposeMeTests
{
    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:alteration-effect")]
    [Fact]
    public void EachMatchingDiscardIsDealtBeforeTheOriginalDiscardContinues()
    {
        // “When a condition is met … the resolution of the preceding ability
        // halts, the each-time effect resolves in its entirety, then the
        // preceding ability continues resolving.” The two Kree cards leave the
        // discard pile before the next discard. When the third discard empties
        // the deck, the immediate reset can move it, but the exact binding
        // survives and deals it, leaving only the non-Kree card behind.
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var removed = world.AreaOf(DeckType.RemovedArea);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }

        var bottomKree = world.CreateCard("90002", deck);
        var nonKree = world.CreateCard(AuthoredCards.ImTough, deck);
        var topKree = world.CreateCard("90001", deck);
        var source = world.CreateCard(
            AuthoredCards.YouDareOpposeMe,
            world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        runner.WhenRevealed(world, source, player: 0);

        var queue = world.AreaOf(
            DeckType.DealtEncounterCardsDeck, PlayArea.Of(0));
        Assert.Equal(
            [topKree.ObjectId, bottomKree.ObjectId],
            queue.Cards.Select(card => card.ObjectId));
        Assert.All(queue.Cards, card => Assert.False(card.FaceUp));
        Assert.Equal([nonKree.ObjectId], deck.Cards.Select(card => card.ObjectId));
        Assert.Empty(world.AreaOf(DeckType.EncounterDiscardPile).Cards);
    }

    [Rule("rr:alteration-effect")]
    [Fact]
    public void AChoiceInsideEachTimeFinishesBeforeTheNextDiscard()
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var removed = world.AreaOf(DeckType.RemovedArea);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }

        var second = world.CreateCard("90002", deck);
        world.CreateCard(AuthoredCards.ImTough, deck);
        var first = world.CreateCard("90001", deck);
        var source = world.CreateCard(
            AuthoredCards.YouDareOpposeMe,
            world.AreaOf(DeckType.RevealingArea));
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "90005", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
              "effect": { "eachTime": {
                "effect": { "discardTop": { "from": "encounterDeck", "count": 3 } },
                "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                "then": { "choose": { "options": [
                  { "dealEncounterCard": { "card": "that", "player": "you" } },
                  { "discard": "that" }
                ] } }
              } }
            } ] } ] }
            """));
        world.Abilities = runner;
        var events = runner.WhenRevealed(world, source, 0).ToList();

        var firstQuestion = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(first.ObjectId, world.AreaOf(DeckType.EncounterDiscardPile).Cards[^1].ObjectId);
        Sequence.Answer(world, Cards, runner, firstQuestion, Decision.Take(0), events);

        var secondQuestion = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(DeckType.EncounterDeck, second.Area.Type);
        Sequence.Answer(world, Cards, runner, secondQuestion, Decision.Take(0), events);
        Sequence.Finish(world, Cards, runner, events);

        Assert.Equal(
            [first.ObjectId, second.ObjectId],
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards
                .Select(card => card.ObjectId));
    }

    [Rule("rr:alteration-effect")]
    [Fact]
    public void EachPlayerResolutionRetainsTheInterruptedCardBinding()
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var removed = world.AreaOf(DeckType.RemovedArea);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }

        var kree = world.CreateCard("90001", deck);
        var source = world.CreateCard(
            AuthoredCards.YouDareOpposeMe,
            world.AreaOf(DeckType.RevealingArea));
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "90005", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
              "effect": { "eachTime": {
                "effect": { "discardTop": { "from": "encounterDeck", "count": 1 } },
                "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                "then": { "eachPlayer": { "effect": {
                  "dealEncounterCard": { "card": "that", "player": "you" }
                } } }
              } }
            } ] } ] }
            """));
        world.Abilities = runner;
        var events = runner.WhenRevealed(world, source, 0).ToList();

        Sequence.Finish(world, Cards, runner, events);

        Assert.Equal(
            [kree.ObjectId],
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards
                .Select(card => card.ObjectId));
    }

    [Rule("rr:alteration-effect")]
    [Fact]
    public void IndirectDamagePromptReadsTheInterruptedCardBinding()
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var removed = world.AreaOf(DeckType.RemovedArea);
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }

        world.CreateCard("90001", deck); // Ronan prints ATK 3.
        world.CreateCard(
            AuthoredCards.BlackCat,
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        var source = world.CreateCard(
            AuthoredCards.YouDareOpposeMe,
            world.AreaOf(DeckType.RevealingArea));
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "90005", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
              "effect": { "eachTime": {
                "effect": { "discardTop": { "from": "encounterDeck", "count": 1 } },
                "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                "then": { "indirectDamage": {
                  "amount": { "modified": { "card": "that", "field": "attack" } },
                  "among": { "query": "heroesAndAllies" }
                } }
              } }
            } ] } ] }
            """));
        world.Abilities = runner;
        var events = runner.WhenRevealed(world, source, 0).ToList();

        var waiting = Assert.Single(world.Agenda.Outstanding);
        var question = runner.Choosing(
            world, source, 0, waiting.Index, waiting.Tier)!;
        var request = Assert.Single(question.Affordances).Targets!;
        Assert.Equal(3, request.Min);
        Assert.Equal(3, request.Max);
        int identity = world.Seats[0].IdentityCard.ObjectId;
        events.AddRange(runner.Chose(
            world, source, 0, waiting.Index,
            Decision.Take(source.ObjectId, [identity, identity, identity], []), waiting.Tier));

        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:alteration-effect")]
    [Fact]
    public void ALabelledAttackRetainsTheInterruptedCardBinding()
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var removed = world.AreaOf(DeckType.RemovedArea);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }

        world.CreateCard("90001", deck);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var source = world.CreateCard(
            AuthoredCards.YouDareOpposeMe,
            world.AreaOf(DeckType.RevealingArea));
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "90005", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
              "effect": { "eachTime": {
                "effect": { "discardTop": { "from": "encounterDeck", "count": 1 } },
                "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                "then": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "if": {
                    "test": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                    "then": { "dealAttackDamage": { "cards": "chosen", "amount": 1 } },
                    "else": { "dealAttackDamage": { "cards": "chosen", "amount": 2 } }
                  } }
                } }
              } }
            } ] } ] }
            """));
        world.Abilities = runner;
        var events = runner.WhenRevealed(world, source, 0).ToList();

        Sequence.Finish(world, Cards, runner, events);

        Assert.Equal(1, villain.Damage);
    }

    [Rule("rr:alteration-effect")]
    [Fact]
    public void AnAfterActivationEffectRetainsTheInterruptedCardBinding()
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var removed = world.AreaOf(DeckType.RemovedArea);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }

        var kree = world.CreateCard("90001", deck);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        const int activationId = 77;
        world.Activation = new EnemyActivation(
            villain.ObjectId, 0, Attacking: true, Id: activationId);
        var source = world.CreateCard(
            AuthoredCards.YouDareOpposeMe,
            world.AreaOf(DeckType.RevealingArea));
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "90005", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
              "effect": { "eachTime": {
                "effect": { "discardTop": { "from": "encounterDeck", "count": 1 } },
                "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                "then": { "afterActivation": { "effect": {
                  "dealEncounterCard": { "card": "that", "player": "you" }
                } } }
              } }
            } ] } ] }
            """));
        world.Abilities = runner;

        runner.WhenRevealed(world, source, 0);
        Assert.Empty(world.AreaOf(
            DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);

        runner.ActivationCompleted(world, new EnemyActivation(
            villain.ObjectId, 0, Attacking: true, Id: activationId));

        Assert.Equal(
            [kree.ObjectId],
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards
                .Select(card => card.ObjectId));
    }

    [Rule("rr:alteration-effect")]
    [Fact]
    public void ADelayedChoiceFailsBeforeThePrecedingDiscardMutatesTheBoard()
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var removed = world.AreaOf(DeckType.RemovedArea);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }

        var kree = world.CreateCard("90001", deck);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Activation = new EnemyActivation(
            villain.ObjectId, 0, Attacking: true, Id: 77);
        var source = world.CreateCard(
            AuthoredCards.YouDareOpposeMe,
            world.AreaOf(DeckType.RevealingArea));
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "90005", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
              "effect": { "eachTime": {
                "effect": { "discardTop": { "from": "encounterDeck", "count": 1 } },
                "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                "then": { "afterActivation": { "effect": { "choose": { "options": [
                  { "dealEncounterCard": { "card": "that", "player": "you" } },
                  { "discard": "that" }
                ] } } } }
              } }
            } ] } ] }
            """));
        world.Abilities = runner;

        var refused = Assert.Throws<RulesNotImplementedException>(
            () => runner.WhenRevealed(world, source, 0));

        Assert.Contains("after-activation", refused.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.EncounterDeck, kree.Area.Type);
        Assert.Single(deck.Cards);
    }

    [Rule("rr:alteration-effect")]
    [Fact]
    public void ADelayedEachPlayerFrameAlsoFailsBeforeTheDiscard()
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var removed = world.AreaOf(DeckType.RemovedArea);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }

        var kree = world.CreateCard("90001", deck);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Activation = new EnemyActivation(
            villain.ObjectId, 0, Attacking: true, Id: 77);
        var source = world.CreateCard(
            AuthoredCards.YouDareOpposeMe,
            world.AreaOf(DeckType.RevealingArea));
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "90005", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
              "effect": { "eachTime": {
                "effect": { "discardTop": { "from": "encounterDeck", "count": 1 } },
                "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                "then": { "afterActivation": { "effect": { "eachPlayer": { "effect": {
                  "dealEncounterCard": { "card": "that", "player": "you" }
                } } } } }
              } }
            } ] } ] }
            """));
        world.Abilities = runner;

        var refused = Assert.Throws<RulesNotImplementedException>(
            () => runner.WhenRevealed(world, source, 0));

        Assert.Contains("after-activation", refused.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.EncounterDeck, kree.Area.Type);
        Assert.Single(deck.Cards);
    }

    [Rule("rr:alteration-effect")]
    [Fact]
    public void DelayedSuspensionPreflightPrecedesEarlierSequenceMutation()
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var removed = world.AreaOf(DeckType.RemovedArea);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }

        var bottom = world.CreateCard("90001", deck);
        var top = world.CreateCard(AuthoredCards.ImTough, deck);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Activation = new EnemyActivation(
            villain.ObjectId, 0, Attacking: true, Id: 77);
        var source = world.CreateCard(
            AuthoredCards.YouDareOpposeMe,
            world.AreaOf(DeckType.RevealingArea));
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "90005", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
              "effect": { "seq": [
                { "discardTop": { "from": "encounterDeck", "count": 1 } },
                { "eachTime": {
                  "effect": { "discardTop": { "from": "encounterDeck", "count": 1 } },
                  "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                  "then": { "afterActivation": { "effect": { "choose": { "options": [
                    { "dealEncounterCard": { "card": "that", "player": "you" } },
                    { "discard": "that" }
                  ] } } } }
                } }
              ] }
            } ] } ] }
            """));
        world.Abilities = runner;

        Assert.Throws<RulesNotImplementedException>(
            () => runner.WhenRevealed(world, source, 0));

        Assert.Equal([bottom.ObjectId, top.ObjectId], deck.Cards.Select(card => card.ObjectId));
        Assert.Empty(world.AreaOf(DeckType.EncounterDiscardPile).Cards);
    }

    [Rule("rr:alteration-effect")]
    [Fact]
    public void ATerminalDelayedThreatPlacementNeedsNoContinuationAddress()
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var removed = world.AreaOf(DeckType.RemovedArea);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }

        world.CreateCard("90001", deck);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        const int activationId = 77;
        world.Activation = new EnemyActivation(
            villain.ObjectId, 0, Attacking: true, Id: activationId);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = main.Tokens.GetValueOrDefault("k_threat");
        var source = world.CreateCard(
            AuthoredCards.YouDareOpposeMe,
            world.AreaOf(DeckType.RevealingArea));
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "90005", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
              "effect": { "eachTime": {
                "effect": { "discardTop": { "from": "encounterDeck", "count": 1 } },
                "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                "then": { "afterActivation": { "effect": {
                  "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 }
                } } }
              } }
            } ] } ] }
            """));
        world.Abilities = runner;
        var events = runner.WhenRevealed(world, source, 0).ToList();

        events.AddRange(runner.ActivationCompleted(world, new EnemyActivation(
            villain.ObjectId, 0, Attacking: true, Id: activationId)));
        Sequence.Finish(world, Cards, runner, events);

        Assert.Equal(before + 1, main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:alteration-effect")]
    [Fact]
    public void ADelayedSchedulerWithLaterWorkFailsBeforeTheDiscard()
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var removed = world.AreaOf(DeckType.RemovedArea);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }

        var kree = world.CreateCard("90001", deck);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Activation = new EnemyActivation(
            villain.ObjectId, 0, Attacking: true, Id: 77);
        var source = world.CreateCard(
            AuthoredCards.YouDareOpposeMe,
            world.AreaOf(DeckType.RevealingArea));
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "90005", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
              "effect": { "eachTime": {
                "effect": { "discardTop": { "from": "encounterDeck", "count": 1 } },
                "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                "then": { "afterActivation": { "effect": { "seq": [
                  { "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } },
                  { "dealEncounterCard": { "card": "that", "player": "you" } }
                ] } } }
              } }
            } ] } ] }
            """));
        world.Abilities = runner;

        Assert.Throws<RulesNotImplementedException>(
            () => runner.WhenRevealed(world, source, 0));

        Assert.Equal(DeckType.EncounterDeck, kree.Area.Type);
        Assert.Single(deck.Cards);
    }

    [Rule("rr:alteration-effect")]
    [Theory]
    [InlineData("{ \"modified\": { \"card\": \"that\", \"field\": \"attack\" } }")]
    [InlineData("{ \"result\": \"activationDamage\" }")]
    public void ADynamicDelayedRepeatFailsBeforeItsCountCanIncrease(string count)
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var removed = world.AreaOf(DeckType.RemovedArea);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }

        var kree = world.CreateCard("90001", deck);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Activation = new EnemyActivation(
            villain.ObjectId, 0, Attacking: true, Id: 77);
        var source = world.CreateCard(
            AuthoredCards.YouDareOpposeMe,
            world.AreaOf(DeckType.RevealingArea));
        string json =
            """
            { "cards": [ { "card": "90005", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
              "effect": { "eachTime": {
                "effect": { "discardTop": { "from": "encounterDeck", "count": 1 } },
                "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                "then": { "afterActivation": { "effect": { "forEach": {
                  "count": COUNT,
                  "effect": {
                    "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 }
                  }
                } } } }
              } }
            } ] } ] }
            """.Replace("COUNT", count, StringComparison.Ordinal);
        var runner = new AbilityRunner(AbilityCatalog.Parse(json));
        world.Abilities = runner;

        Assert.Throws<RulesNotImplementedException>(
            () => runner.WhenRevealed(world, source, 0));

        Assert.Equal(DeckType.EncounterDeck, kree.Area.Type);
        Assert.Single(deck.Cards);
    }

    [Rule("rr:alteration-effect")]
    [Theory]
    [InlineData("2", false)]
    [InlineData("-1", true)]
    [InlineData("{ \"result\": \"activationDamage\" }", false)]
    public void AnUnsafeNestedDelayedAlterationFailsBeforeEitherDiscardMutatesTheBoard(
        string count, bool invalidCount)
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var removed = world.AreaOf(DeckType.RemovedArea);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }

        var innerSecond = world.CreateCard("90002", deck);
        var innerFirst = world.CreateCard("90001", deck);
        var outer = world.CreateCard("90001", deck);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Activation = new EnemyActivation(
            villain.ObjectId, 0, Attacking: true, Id: 77);
        var source = world.CreateCard(
            AuthoredCards.YouDareOpposeMe,
            world.AreaOf(DeckType.RevealingArea));
        string json =
            """
            { "cards": [ { "card": "90005", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
              "effect": { "eachTime": {
                "effect": { "discardTop": { "from": "encounterDeck", "count": 1 } },
                "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                "then": { "afterActivation": { "effect": { "eachTime": {
                  "effect": { "discardTop": { "from": "encounterDeck", "count": COUNT } },
                  "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                  "then": {
                    "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 }
                  }
                } } } }
              } }
            } ] } ] }
            """.Replace("COUNT", count, StringComparison.Ordinal);
        var runner = new AbilityRunner(AbilityCatalog.Parse(json));
        world.Abilities = runner;

        var refused = Record.Exception(() => runner.WhenRevealed(world, source, 0));
        if (invalidCount)
        {
            Assert.IsType<AbilityException>(refused);
        }
        else
        {
            Assert.IsType<RulesNotImplementedException>(refused);
        }

        Assert.Equal(
            [innerSecond.ObjectId, innerFirst.ObjectId, outer.ObjectId],
            deck.Cards.Select(card => card.ObjectId));
    }

    [Rule("rr:alteration-effect")]
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void ANestedAfterActivationIsRejectedOnlyWhenEachTimeCanReachIt(
        int count, bool rejected)
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var removed = world.AreaOf(DeckType.RemovedArea);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }

        var remaining = world.CreateCard("90002", deck);
        var outer = world.CreateCard("90001", deck);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        const int activationId = 77;
        world.Activation = new EnemyActivation(
            villain.ObjectId, 0, Attacking: true, Id: activationId);
        var source = world.CreateCard(
            AuthoredCards.YouDareOpposeMe,
            world.AreaOf(DeckType.RevealingArea));
        string json =
            """
            { "cards": [ { "card": "90005", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
              "effect": { "eachTime": {
                "effect": { "discardTop": { "from": "encounterDeck", "count": 1 } },
                "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                "then": { "afterActivation": { "effect": { "eachTime": {
                  "effect": { "discardTop": { "from": "encounterDeck", "count": COUNT } },
                  "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                  "then": { "afterActivation": { "effect": {
                    "draw": { "player": "you", "count": 1 }
                  } } }
                } } } }
              } }
            } ] } ] }
            """.Replace("COUNT", count.ToString(), StringComparison.Ordinal);
        var runner = new AbilityRunner(AbilityCatalog.Parse(json));
        world.Abilities = runner;

        if (rejected)
        {
            Assert.Throws<RulesNotImplementedException>(
                () => runner.WhenRevealed(world, source, 0));
            Assert.Equal(
                [remaining.ObjectId, outer.ObjectId],
                deck.Cards.Select(card => card.ObjectId));
        }
        else
        {
            runner.WhenRevealed(world, source, 0);
            runner.ActivationCompleted(world, new EnemyActivation(
                villain.ObjectId, 0, Attacking: true, Id: activationId));

            Assert.Equal([remaining.ObjectId], deck.Cards.Select(card => card.ObjectId));
            Assert.Equal(
                [outer.ObjectId],
                world.AreaOf(DeckType.EncounterDiscardPile).Cards
                    .Select(card => card.ObjectId));
        }
    }

    [Rule("rr:alteration-effect")]
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void TheBoostModifierBindsOnlyToAnAttackActivation(
        bool attacking, bool expectedOverkill)
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        var identity = world.Seats[0].IdentityCard;
        world.Activation = new EnemyActivation(enemy.ObjectId, 0, attacking);
        world.Attack = attacking
            ? new EnemyAttack(enemy.ObjectId, 0, identity.ObjectId)
            : null;
        var source = world.CreateCard(
            AuthoredCards.YouDareOpposeMe,
            world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        runner.Boost(world, source, player: 0);

        Assert.Equal(
            expectedOverkill,
            Keywords.Has(world, enemy, Keywords.Overkill, Cards));
    }
}
