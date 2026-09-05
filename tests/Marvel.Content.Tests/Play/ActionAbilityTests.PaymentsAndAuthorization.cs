using Marvel.Cards.Dsl;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Rule("rr:initiating-abilities.step.5")]
    [Theory]
    [InlineData("dealDamage")]
    [InlineData("takeDamage")]
    public void ACompiledDamageCostResumesItsPostArrowEffectExactlyOnce(string damageCost)
    {
        // rr:initiating-abilities.step.5: "Pay the cost(s)." An interrupt during the
        // damage cost must finish before the post-arrow draw, without paying
        // the cost again when the ability resumes.
        var runner = new Marvel.Cards.Run.AbilityRunner(AbilityCatalog.Parse($$$"""
            {"cards":[
              {"card":"01030","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"seq":[{"exhaust":"this"},{"{{{damageCost}}}":{"cards":"this","amount":2}}]},
                "effect":{"draw":{"player":"you","count":1}}
              }]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}
              }]}
            ]}
            """));
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("01030",
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            source.TakeDamage(2);
            InPlay(board, "01092");
        }, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.False(source!.Ready);
        Assert.Equal(4, source.Damage);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);

        game.Resolve(Decision.Decline);

        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void PaymentChoicesAndValidationUseTheCompiledCostSnapshot()
    {
        var parsed = AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """);
        var fields = new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
        {
            ["from"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
            {
                ["query"] = new AbilityValue.Word("alliesYouControl"),
            }),
            ["count"] = new AbilityValue.Number(1),
        };
        var ability = parsed.Abilities[0] with
        {
            Cost = new AbilityNode("exhaustChosen", new AbilityValue.Map(fields)),
        };
        var runner = new Marvel.Cards.Run.AbilityRunner(new AbilityBook([ability], parsed.Authored));
        Card? source = null;
        Card? ally = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            ally = board.CreateCard(AuthoredCards.BlackCat,
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        }, abilities: runner);

        // The executable program is a snapshot. Changing the caller-owned
        // syntax cannot change either the offered count or the accepted answer.
        fields["count"] = new AbilityValue.Number(99);
        var pending = Assert.Single(runner.Actions(world, 0), option => option.Card == source!.ObjectId);
        var target = Assert.IsType<TargetRequest>(runner.Describe(world, pending).Targets);
        Assert.Equal(1, target.Min);
        Assert.Equal(1, target.Max);
        Assert.Equal([ally!.ObjectId], target.Legal);

        runner.Act(world, pending, [], [ally.ObjectId]);

        Assert.False(ally.Ready);
    }

    [Fact]
    public void PaymentExecutionUsesTheCompiledCostSnapshot()
    {
        var parsed = AbilityCatalog.Parse("""
            {"cards":[{"card":"01030","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """);
        var fields = new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
        {
            ["card"] = new AbilityValue.Word("this"),
            ["amount"] = new AbilityValue.Number(1),
        };
        var ability = parsed.Abilities[0] with
        {
            Cost = new AbilityNode("heal", new AbilityValue.Map(fields)),
        };
        var runner = new Marvel.Cards.Run.AbilityRunner(new AbilityBook([ability], parsed.Authored));
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("01030",
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            source.TakeDamage(2);
        }, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;

        // Engine choice: compilation snapshots authored syntax. Payment must
        // use that program even if its caller later edits the input dictionary.
        fields["amount"] = new AbilityValue.Number(2);
        var action = Assert.Single(game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(1, source!.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void RuntimeCardMetadataUsesTheCompiledSnapshot()
    {
        var authored = new HashSet<string>(StringComparer.Ordinal) { "01080" };
        var firstPlayer = new HashSet<string>(StringComparer.Ordinal) { "01080" };
        var placementOnly = new HashSet<string>(StringComparer.Ordinal);
        var pools = new Dictionary<string, CardCounterPool>(StringComparer.Ordinal)
        {
            ["01080"] = new("medical", 3, Uses: true),
        };
        var runner = new Marvel.Cards.Run.AbilityRunner(new AbilityBook([], authored,
            ControlledByFirstPlayer: firstPlayer, PlacementOnly: placementOnly, CounterPools: pools));
        Card? source = null;
        var (_, world) = Playing(board => source = InPlay(board, "01080"));

        // Engine choice: caller-owned syntax cannot alter a compiled program.
        authored.Clear();
        firstPlayer.Clear();
        placementOnly.Add("01080");
        pools["01080"] = new("medical", 9, Uses: false);

        Assert.Contains("01080", runner.Authored);
        Assert.Equal(world.FirstPlayer, runner.SetupController(world, source!));
        Assert.Equal(new CardCounterPool("medical", 3, Uses: true), runner.CounterPool(world, source!));
        Assert.Empty(runner.WhenRevealed(world, source!, 0));
        runner.ValidateForPlay(world);
    }

    [Rule("rr:printed.1")]
    [Rule("rr:text-box.1.1")]
    [Fact]
    public void APrintedTextBoxResourceAbilityPaysAPrintedResourceCost()
    {
        // “They may use resources generated by a card's ability, so long as
        // the icon for the resource that card generates is printed in its text
        // box.” Peter Parker's Scientist icon is marked from printed card data
        // and appears beside printed hand icons in this narrower generator menu.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "spendPrinted": "B" }""",
            includeAuthored: true);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                source = InPlay(board, AuthoredCards.AuntMay);
            },
            abilities: runner);
        int peter = world.Seats[0].IdentityCard.ObjectId;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId
                && option.CostOptions.Any(cost =>
                    cost.ResourceCosts.Any(component => component.Printed)));
        var price = Assert.Single(action.CostOptions);

        Assert.Contains(price.Generators, generator => generator.Effect == peter);
        Assert.True(Assert.Single(price.ResourceCosts).Printed);

        game.Resolve(Decision.Take(action.Id, [], [peter]));

        Assert.DoesNotContain(
            runner.ResourceAbilities(world, 0), generator => generator.Effect == peter);
    }

    [Rule("rr:printed.1.1")]
    [Fact]
    public void APrintedWildCannotSubstituteForAPrintedPhysicalCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "spendPrinted": "R" }""",
            includeAuthored: true);
        Card? source = null;
        Card? wild = null;
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                source = InPlay(board, AuthoredCards.AuntMay);
                wild = board.CreateCard("01011", board.Seats[0].Hand);
            },
            abilities: runner);

        foreach (var other in world.Seats[0].Hand.Cards
                     .Where(card => card != wild)
                     .ToList())
        {
            World.MoveToTop(other, world.Seats[0].Deck);
        }

        Assert.DoesNotContain(
            runner.Actions(world, 0),
            ability => ability.Card == source!.ObjectId && ability.Ordinal == 1);
        Assert.Same(world.Seats[0].Hand, wild!.Area);
    }

    [Rule("rr:cost.5")]
    [Rule("rr:printed.1")]
    [Fact]
    public void SimultaneousPrintedResourceCostsShareOneExactPayment()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "seq": [ { "spendPrinted": "B" }, { "spendPrinted": "Y" } ] }""",
            includeAuthored: true);
        Card? source = null;
        Card? energy = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                energy = board.CreateCard("01093", board.Seats[0].Hand);
            },
            abilities: runner);
        foreach (var other in world.Seats[0].Hand.Cards
                     .Where(card => card != energy)
                     .ToList())
        {
            World.MoveToTop(other, world.Seats[0].Deck);
        }
        int peter = world.Seats[0].IdentityCard.ObjectId;
        var ability = Assert.Single(
            runner.Actions(world, 0),
            pending => pending.Card == source!.ObjectId && pending.Ordinal == 1);
        var price = Assert.Single(runner.Describe(world, ability).CostOptions);

        Assert.Equal(2, price.ResourceCosts.Count);
        Assert.All(price.ResourceCosts, component => Assert.True(component.Printed));

        runner.Act(world, ability, [peter, energy!.ObjectId], []);

        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.DoesNotContain(
            runner.ResourceAbilities(world, 0), generator => generator.Effect == peter);
    }

    [Rule("rr:cost.7")]
    [Rule("rr:cost.8")]
    [Fact]
    public void AnOutOfPlayCostCanUseOnlyThePayingPlayersArea()
    {
        // A hand is out of play, and a payer "may only use game elements that
        // are in their own out-of-play areas." The prompt therefore offers
        // only the payer's hand, and forging another player's card is rejected
        // before either card moves.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "discardFromHand": 1 }""");
        Card? source = null;
        Card? otherPlayersCard = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                otherPlayersCard = board.CreateCard(
                    Physicals, board.Seats[1].Hand);
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var ability = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);
        var targets = Assert.IsType<TargetRequest>(
            runner.Describe(world, ability).Targets);

        Assert.DoesNotContain(otherPlayersCard!.ObjectId, targets.Legal);
        Assert.Throws<RulesNotImplementedException>(() => runner.Act(
            world, ability, [], [otherPlayersCard.ObjectId]));
        Assert.Same(world.Seats[1].Hand, otherPlayersCard.Area);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Rule("rr:cost.7.1")]
    [Rule("rr:cost.7.2")]
    [Fact]
    public void AChosenFriendlyCostCanUseAnotherPlayersCard()
    {
        // A cost that "uses the word 'choose'" or targets a "friendly" card
        // may choose cards the payer does not control.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost:
            """
            { "exhaustChosen": {
              "from": { "query": "heroesAndAllies" },
              "count": 1
            } }
            """);
        Card? source = null;
        Card? friendly = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                friendly = board.CreateCard(
                    AuthoredCards.BlackCat,
                    board.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var ability = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);
        var targets = Assert.IsType<TargetRequest>(
            runner.Describe(world, ability).Targets);

        Assert.Contains(friendly!.ObjectId, targets.Legal);
        Assert.DoesNotContain(world.Seats[1].IdentityCard.ObjectId, targets.Legal);
        runner.Act(world, ability, [], [friendly.ObjectId]);

        Assert.False(friendly.Ready);
    }

    [Rule("rr:cost.9")]
    [Theory]
    [InlineData("{ \"discardUpToFromHand\": 3 }")]
    [InlineData("{ \"discardAnyFromHand\": \"yourHand\" }")]
    public void UpToAndAnyNumberCostsRequireAtLeastOne(string cost)
    {
        // A cost requiring "any number" or "up to" a number "requires a
        // minimum of one such game element."
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: cost);
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var ability = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);
        var targets = Assert.IsType<TargetRequest>(
            runner.Describe(world, ability).Targets);
        int held = world.Seats[0].Hand.Cards.Count;

        Assert.Equal(1, targets.Min);
        Assert.True(targets.Max >= 1);
        Assert.Throws<RulesNotImplementedException>(() =>
            runner.Act(world, ability, [], []));
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);

        int paid = targets.Legal[0];
        runner.Act(world, ability, [], [paid]);
        Assert.Equal(DeckType.DiscardPile, world.Cards[paid].Area.Type);
    }

    [Rule("rr:cost.5")]
    [Rule("rr:cost.10")]
    [Fact]
    public void AnUnpayableSimultaneousCostChangesNoState()
    {
        // The resource half is invalid, so the exhaust half is not paid first.
        // The forged action is rejected with the source still ready.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "seq": [ { "exhaust": "this" }, { "spend": "B" } ] }""");
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                Hand(board, Physicals, 1);
            },
            abilities: runner);
        var forged = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);

        Assert.Throws<RulesNotImplementedException>(
            () => runner.Act(world, forged, [world.Seats[0].Hand.Cards[0].ObjectId], []));
        Assert.True(source!.Ready);
    }

    [Rule("rr:player-turn.5")]
    [Rule("rr:player-turn.6")]
    [Fact]
    public void ASharedEncounterActionIsOfferedForEveryEligiblePlayer()
    {
        // The request is implied, but the acting seat is material: that player
        // supplies the resources and resolves every reference to "you".
        Card? horn = null;
        var (game, _) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
                board.Seats[1].IdentityCard.TurnTo("01010a");
                horn = board.CreateCard(
                    AuthoredCards.IvoryHorn, board.AreaOf(DeckType.RevealingArea));
                board.Abilities = AuthoredCards.Runner();
                Reveal.Resolve(board, Cards, horn, 0, []);

                foreach (var card in board.Seats[0].Hand.Cards.ToList())
                {
                    World.MoveToTop(card, board.Seats[0].Deck);
                }

                Hand(board, player: 1, Physicals, count: 3);
            },
            heroes: ["spider_man", "captain_marvel"]);

        var actions = game.Pending!.Affordances
            .Where(option => option.Verb == Game.ActionVerb
                && option.AnchorId == horn!.ObjectId)
            .ToList();
        Assert.Equal([0, 1], actions.Select(action => action.AnchorPlayer));
        Assert.Equal(2, actions.Select(action => action.Id).Distinct().Count());
    }

    [Rule("rr:ability.8.2")]
    [Rule("rr:action.1.1")]
    [Rule("rr:obligation.6")]
    [Fact]
    public void OnlyThePlayerHoldingAnObligationMayTriggerItsAction()
    {
        // "Only the player with the obligation in their play area can trigger
        // abilities or pay costs on that obligation." It remains an encounter
        // card, but this is the exception to the general permission to use
        // encounter-card actions.
        // The second player's turn can request another player's ordinary
        // action; it cannot request the obligation sitting in player zero's
        // play area.
        var runner = Runner(
            AuthoredCards.EvictionNotice,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""");
        Card? obligation = null;
        var (_, world) = Playing(
            board => obligation = board.CreateCard(
                AuthoredCards.EvictionNotice,
                board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0))),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(runner.Actions(world, 0), action => action.Card == obligation!.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 1), action => action.Card == obligation!.ObjectId);
    }

    [Rule("rr:ability.8.1")]
    [Rule("rr:attachment.2")]
    [Rule("rr:attachment.2.1")]
    [Fact]
    public void OnlyTheHostControllerMayUseAnAttachmentAbilityThatSaysYou()
    {
        // "You" on an attachment refers to "the attached player card's
        // controller", and "only" that player can trigger its abilities or
        // pay its costs. The attachment itself belongs to the scenario.
        var runner = Runner(
            AuthoredCards.Charge,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""");
        Card? attachment = null;
        var (_, world) = Playing(
            board => attachment = board.CreateCard(
                AuthoredCards.Charge,
                board.AreaOf(
                    DeckType.UpgradesArea,
                    PlayArea.Of(0),
                    host: board.Seats[0].IdentityCard.ObjectId)),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(runner.Actions(world, 0), action => action.Card == attachment!.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 1), action => action.Card == attachment!.ObjectId);
    }

    [Rule("rr:ability.8.1")]
    [Theory]
    [InlineData("""{ "exhaust": "you" }""", false)]
    [InlineData("""{ "removeCounters": { "card": "this", "counter": "yourMarker", "count": 1 } }""", true)]
    public void AttachmentCostAuthorizationUsesBindingsNotCounterNames(string cost, bool anotherPlayerMayAct)
    {
        // rr:ability.8.1 restricts an attachment that "uses the word “you” or
        // “your”". A counter's engine-chosen name is not a printed
        // player binding, even when its spelling includes "your".
        var runner = Runner(AuthoredCards.Charge, "Action", """{ "discard": "this" }""", cost: cost);
        Card? attachment = null;
        var (_, world) = Playing(board =>
        {
            attachment = board.CreateCard(AuthoredCards.Charge,
                board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0),
                    host: board.Seats[0].IdentityCard.ObjectId));
            attachment.PlaceTokens("c_yourMarker", 1);
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);

        Assert.Contains(runner.Actions(world, 0), action => action.Card == attachment!.ObjectId);
        Assert.Equal(anotherPlayerMayAct,
            runner.Actions(world, 1).Any(action => action.Card == attachment!.ObjectId));
    }

    [Rule("rr:ability.8.1")]
    [Fact]
    public void YourInTheAttachmentInstructionRestrictsEveryAbilityOnTheCard()
    {
        // All Tied Up says “Attach to your identity card,” while its Action is
        // only “spend resources → discard this card.” The permission belongs
        // to the attachment's whole printed text, not only the selected
        // ability, so another player cannot trigger that Action.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                """
                { "cards": [ { "card": "02048", "attachTo": "you", "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "effect": { "discard": "this" }
                } ] } ] }
                """));
        Card? attachment = null;
        var (_, world) = Playing(
            board => attachment = board.CreateCard(
                "02048",
                board.AreaOf(
                    DeckType.UpgradesArea,
                    PlayArea.Of(0),
                    host: board.Seats[0].IdentityCard.ObjectId)),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(runner.Actions(world, 0), action => action.Card == attachment!.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 1), action => action.Card == attachment!.ObjectId);
    }

    [Rule("rr:ability.8.1")]
    [Rule("rr:the-golden-rules")]
    [Fact]
    public void AnExplicitAnyPlayerPermissionOverridesTheAttachmentRestriction()
    {
        // Obedience Potion says “Attach to your identity,” then its Hero
        // Action ends “Any player can do this.” The printed exception lets the
        // other player initiate the ability and pay from their own hand.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                """
                { "cards": [ { "card": "16123", "attachTo": "you", "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game", "form": "hero" },
                    "anyPlayer": true,
                    "cost": { "spend": "BB" },
                    "effect": { "discard": "this" }
                } ] } ] }
                """));
        Card? attachment = null;
        Card[] payment = [];
        var (_, world) = Playing(
            board =>
            {
                board.Seats[1].IdentityCard.TurnTo("01010a");
                attachment = board.CreateCard(
                    "16123",
                    board.AreaOf(
                        DeckType.UpgradesArea,
                        PlayArea.Of(0),
                        host: board.Seats[0].IdentityCard.ObjectId));
                Hand(board, player: 1, Mentals, count: 2);
                payment = [.. board.Seats[1].Hand.Cards.Take(2)];
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        var action = Assert.Single(
            runner.Actions(world, 1), option => option.Card == attachment!.ObjectId);
        runner.Act(world, action, [.. payment.Select(card => card.ObjectId)], []);

        Assert.Equal(DeckType.EncounterDiscardPile, attachment!.Area.Type);
        Assert.All(payment, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
    }

    [Rule("rr:player-turn.5")]
    [Fact]
    public void AnyPlayerMayUseAPlayerCardThatPrintsThatPermission()
    {
        // Player-turn option 5.c is exactly Plot Convenience's last line:
        // “Any player may trigger this ability.” The permission makes another
        // player's support visible, that player initiates it, and its printed
        // exhaust cost is paid before the effect resolves for that player.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                """
                { "cards": [ { "card": "44050", "abilities": [
                  {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "anyPlayer": true,
                    "cost": { "exhaust": "this" },
                    "effect": { "draw": { "player": "you", "count": 1 } }
                  },
                  {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "effect": { "draw": { "player": "you", "count": 2 } }
                  }
                ] } ] }
                """));
        Card? support = null;
        var (_, world) = Playing(
            board => support = InPlay(board, "44050"),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        int held = world.Seats[1].Hand.Cards.Count;

        var action = Assert.Single(
            runner.Actions(world, 1), option => option.Card == support!.ObjectId);
        runner.Act(world, action, [], []);

        Assert.False(support!.Ready);
        Assert.Equal(held + 1, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:interrupt.1")]
    [Rule("rr:response.1")]
    [Fact]
    public void AnyPlayerWindowAbilitiesAreEvaluatedForEachPlayer()
    {
        var runner = Runner(
            "44050",
            "Response",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "spend": "B" }""",
            eventName: Steps.DamageDealt,
            anyPlayer: true);
        Card? support = null;
        Card[] payment = [];
        var (_, world) = Playing(
            board =>
            {
                support = InPlay(board, "44050");
                Hand(board, player: 0, Physicals, count: 0);
                Hand(board, player: 1, Mentals, count: 1);
                payment = [.. board.Seats[1].Hand.Cards];
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        int held = world.Seats[1].Hand.Cards.Count;

        var response = Assert.Single(
            runner.Waiting(
                world,
                new Occurrence(1, [Steps.DamageDealt], Player: 0),
                WindowKind.Response),
            option => option.Player == 1);

        Assert.Equal(1, response.Player);
        runner.Act(world, response, [payment[0].ObjectId], []);
        Assert.Equal(DeckType.DiscardPile, payment[0].Area.Type);
        Assert.Equal(held, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(support!.ObjectId, response.Card);
    }

    [Rule("rr:in-player-order.1")]
    [Rule("rr:response.1")]
    [Fact]
    public void AnyPlayerWindowAnswerResumesForThePlayerWhoAcceptedIt()
    {
        var runner = Runner(
            "44050",
            "Response",
            """{ "draw": { "player": "you", "count": 1 } }""",
            eventName: Steps.DamageDealt,
            anyPlayer: true);
        var (_, world) = Playing(
            board => InPlay(board, "44050"),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var occurrence = new Occurrence(1, [Steps.DamageDealt], Player: 0);
        var events = new List<GameEvent>();
        int firstHeld = world.Seats[0].Hand.Cards.Count;
        int secondHeld = world.Seats[1].Hand.Cards.Count;

        var first = Offering.Work(
            world, runner, occurrence, WindowKind.Response, events)!;
        Assert.Equal(0, first.Player);
        Sequence.Answer(world, Cards, runner, first, Decision.Decline, events);

        var second = Offering.Work(
            world, runner, occurrence, WindowKind.Response, events)!;
        Assert.Equal(1, second.Player);
        Sequence.Answer(
            world, Cards, runner, second,
            Decision.Take(Assert.Single(second.Affordances).Id), events);

        Assert.Equal(firstHeld, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(secondHeld + 1, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:ability.8")]
    [Fact]
    public void TriggerPlayerStillNarrowsAnAnyPlayerWindow()
    {
        var runner = Runner(
            "44050",
            "Response",
            """{ "draw": { "player": "you", "count": 1 } }""",
            eventName: Steps.DamageDealt,
            player: "trigger.player",
            anyPlayer: true);
        var (_, world) = Playing(
            board => InPlay(board, "44050"),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        var waiting = runner.Waiting(
            world,
            new Occurrence(1, [Steps.DamageDealt], Player: 0),
            WindowKind.Response);

        Assert.Equal(0, Assert.Single(waiting).Player);
    }

    [Rule("rr:ability.8.1")]
    [Fact]
    public void AnAttachmentsYouTriggerMatchesOnlyItsHostController()
    {
        var runner = Runner(
            AuthoredCards.Charge,
            "Response",
            """{ "draw": { "player": "you", "count": 1 } }""",
            eventName: "WhenAttacked",
            player: "you");
        Card? attachment = null;
        var (_, world) = Playing(
            board => attachment = board.CreateCard(
                AuthoredCards.Charge,
                board.AreaOf(
                    DeckType.UpgradesArea,
                    PlayArea.Of(0),
                    host: board.Seats[0].IdentityCard.ObjectId)),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        var ours = runner.Waiting(
            world, new Occurrence(1, ["WhenAttacked"], Player: 0), WindowKind.Response);
        var theirs = runner.Waiting(
            world, new Occurrence(2, ["WhenAttacked"], Player: 1), WindowKind.Response);

        Assert.Equal(0, Assert.Single(ours).Player);
        Assert.Empty(theirs);
    }

    [Rule("rr:ability.8.1")]
    [Rule("rr:ability.8.2")]
    [Fact]
    public void RestrictedResourceAbilitiesBelongOnlyToTheirPermittedPlayer()
    {
        var obligationRunner = Runner(
            AuthoredCards.EvictionNotice,
            "Resource",
            """{ "generate": "Y" }""");
        Card? obligation = null;
        var (_, obligationWorld) = Playing(
            board => obligation = board.CreateCard(
                AuthoredCards.EvictionNotice,
                board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0))),
            heroes: ["spider_man", "captain_marvel"],
            abilities: obligationRunner);

        Assert.Contains(
            obligationRunner.ResourceAbilities(obligationWorld, 0),
            source => source.Effect == obligation!.ObjectId);
        Assert.DoesNotContain(
            obligationRunner.ResourceAbilities(obligationWorld, 1),
            source => source.Effect == obligation!.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => obligationRunner.UseResource(
            obligationWorld, 1, obligation!.ObjectId, []));

        // Compound bindings are still printed “you/your”: this query means
        // allies controlled by the resolving player and restricts a
        // player-hosted attachment just as the bare word “you” does.
        var attachmentRunner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [ { "card": "{{AuthoredCards.Charge}}", "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Resource", "subject": "game" },
                    "when": { "exists": { "query": "alliesYouControl" } },
                    "effect": { "generate": "B" }
                } ] } ] }
                """));
        Card? attachment = null;
        var (_, attachmentWorld) = Playing(
            board =>
            {
                board.CreateCard(
                    "01002",
                    board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                attachment = board.CreateCard(
                    AuthoredCards.Charge,
                    board.AreaOf(
                        DeckType.UpgradesArea,
                        PlayArea.Of(0),
                        host: board.Seats[0].IdentityCard.ObjectId));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: attachmentRunner);

        Assert.Contains(
            attachmentRunner.ResourceAbilities(attachmentWorld, 0),
            source => source.Effect == attachment!.ObjectId);
        Assert.DoesNotContain(
            attachmentRunner.ResourceAbilities(attachmentWorld, 1),
            source => source.Effect == attachment!.ObjectId);

        // Player-relative semantics can also be the node name rather than a
        // word value. Test it in a real response occurrence whose target
        // makes `isYourIdentity` true for the host controller and false for
        // the other player.
        var kindRunner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [ { "card": "{{AuthoredCards.Charge}}", "abilities": [ {
                    "trigger": { "event": "WhenDamageWouldBeDealt", "timing": "Response", "subject": "game" },
                    "when": { "isYourIdentity": "trigger.target" },
                    "effect": { "draw": { "player": "you", "count": 1 } }
                } ] } ] }
                """));
        Card? kindAttachment = null;
        var (_, kindWorld) = Playing(
            board => kindAttachment = board.CreateCard(
                AuthoredCards.Charge,
                board.AreaOf(
                    DeckType.UpgradesArea,
                    PlayArea.Of(0),
                    host: board.Seats[0].IdentityCard.ObjectId)),
            heroes: ["spider_man", "captain_marvel"],
            abilities: kindRunner);

        var ours = kindRunner.Waiting(
            kindWorld,
            new Occurrence(
                1,
                ["WhenDamageWouldBeDealt"],
                Player: 0,
                Target: kindWorld.Seats[0].IdentityCard.ObjectId),
            WindowKind.Response);
        var theirs = kindRunner.Waiting(
            kindWorld,
            new Occurrence(
                2,
                ["WhenDamageWouldBeDealt"],
                Player: 1,
                Target: kindWorld.Seats[1].IdentityCard.ObjectId),
            WindowKind.Response);

        Assert.Equal(kindAttachment!.ObjectId, Assert.Single(ours).Card);
        Assert.Empty(theirs);
    }

    [Rule("rr:interrupt.1.1")]
    [Rule("rr:response.1.1")]
    [Theory]
    [InlineData("Interrupt")]
    [InlineData("Response")]
    public void AnotherPlayersObligationIsExcludedFromAbilityWindows(string timing)
    {
        var runner = Runner(
            AuthoredCards.EvictionNotice,
            timing,
            """{ "draw": { "player": "you", "count": 1 } }""",
            eventName: "WhenAttacked");
        var (_, world) = Playing(
            board => board.CreateCard(
                AuthoredCards.EvictionNotice,
                board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0))),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        world.FirstPlayer = 1;
        var occurrence = new Occurrence(1, ["WhenAttacked"], Player: 1);

        var prompt = Offering.Work(
            world,
            runner,
            occurrence,
            timing == "Interrupt" ? WindowKind.Interrupt : WindowKind.Response,
            []);

        Assert.NotNull(prompt);
        Assert.Equal(0, prompt.Player);
    }

    [Rule("rr:action.2")]
    [Rule("rr:action.2.1")]
    [Rule("rr:forced.2")]
    [Fact]
    public void ALegalForcedActionMustResolveBeforeThePlayerPhaseEnds()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "ForcedAction",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);

        // It may be used at any ordinary action opportunity.
        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);

        // At the boundary it is no longer optional: the phase stays in the
        // player turn and the only answer is to resolve the Forced Action.
        game.Resolve(Decision.Decline);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.True(game.IsForcedResolutionPrompt);
        Assert.False(game.Pending!.Cancellable);
        var forced = Assert.Single(game.Pending.Affordances);

        int held = world.Seats[0].Hand.Cards.Count;
        game.Resolve(Decision.Take(forced.Id));

        Assert.False(source!.Ready);
        Assert.False(game.IsForcedResolutionPrompt);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);

        // Its exhaust cost is now unpayable, so resolution continues directly
        // to the ordinary end phase. It must not reopen a normal turn.
        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.Verb is Game.ChangeForm or Game.ActionVerb);
    }

    [Rule("rr:action.2")]
    [Rule("rr:ability.8.2")]
    [Fact]
    public void APlayersForcedActionAsksThatPlayerToChooseItsPayment()
    {
        var runner = Runner(
            AuthoredCards.EvictionNotice,
            "ForcedAction",
            """{ "discard": "this" }""",
            cost: """{ "discardFromHand": 1 }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = board.CreateCard(
                AuthoredCards.EvictionNotice,
                board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1))),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);

        Assert.Equal(1, game.Pending!.Player);
        var forced = Assert.Single(game.Pending.Affordances);
        Assert.NotNull(forced.Targets);
        var targets = forced.Targets;
        Assert.All(
            targets.Legal,
            id => Assert.Contains(world.Cards[id], world.Seats[1].Hand.Cards));
        int p0Held = world.Seats[0].Hand.Cards.Count;
        var paid = world.Cards[targets.Legal[0]];

        game.Resolve(Decision.Take(forced.Id, [paid.ObjectId], []));

        Assert.Equal(p0Held, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, paid.Area.Type);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }

    [Rule("rr:action.2")]
    [Fact]
    public void ACostlessForcedActionNeedOnlyResolveOnceBeforeThePhaseEnds()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "ForcedAction",
            """{ "draw": { "player": "you", "count": 1 } }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);

        game.Resolve(Decision.Decline);
        var forced = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);
        int held = world.Seats[0].Hand.Cards.Count;

        game.Resolve(Decision.Take(forced.Id));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }

    [Rule("rr:action.2")]
    [Rule("rr:player-elimination.step.5")]
    [Fact]
    public void EliminatingTheFirstPlayerDuringTheGateMovesTheEndPhaseToTheSurvivor()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "ForcedAction",
            """{ "dealDamage": { "cards": "you", "amount": 99 } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);
        var forced = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(forced.Id));

        Assert.True(world.Seats[0].Eliminated);
        Assert.Equal(1, world.FirstPlayer);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.Equal(1, game.Pending!.Player);
    }

    [Rule("rr:action.2")]
    [Fact]
    public void ForcedActionsOnTwoFacesOfOneIdentityAreDistinctAbilities()
    {
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "{{AuthoredCards.SpiderMan}}", "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "ForcedAction", "subject": "game" },
                    "limitPerRound": 1,
                    "effect": { "changeForm": { "player": "you", "to": "alter-ego" } }
                  } ] },
                  { "card": "01001b", "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "ForcedAction", "subject": "game" },
                    "limitPerRound": 1,
                    "effect": { "draw": { "player": "you", "count": 1 } }
                  } ] }
                ] }
                """));
        var (game, world) = Playing(_ => { }, hero: true, abilities: runner);

        game.Resolve(Decision.Decline);
        var heroAction = Assert.Single(game.Pending!.Affordances);
        game.Resolve(Decision.Take(heroAction.Id));

        Assert.Equal("01001b", world.Seats[0].IdentityCard.FaceId);
        var alterEgoAction = Assert.Single(game.Pending!.Affordances);
        int held = world.Seats[0].Hand.Cards.Count;
        game.Resolve(Decision.Take(alterEgoAction.Id));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }

    [Rule("rr:action.2")]
    [Rule("rr:leaves-play.1")]
    [Fact]
    public void AForcedActionOnACardThatLeavesAndReturnsBelongsToTheNewCopy()
    {
        var runner = Runner(
            "01101",
            "ForcedAction",
            """
            { "seq": [
              { "discard": "this" },
              { "putIntoPlay": { "card": "this", "where": "engagedWithYou" } },
              { "discard": "this" }
            ] }
            """,
            limit: 1);
        Card? source = null;
        var (game, _) = Playing(
            board => source = board.CreateCard(
                "01101",
                board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))),
            abilities: runner);
        int firstCopy = source!.Incarnation;

        game.Resolve(Decision.Decline);
        var forced = Assert.Single(game.Pending!.Affordances);
        game.Resolve(Decision.Take(forced.Id));

        Assert.True(source.Incarnation > firstCopy);
        Assert.Equal(DeckType.EngagedEnemiesArea, source.Area.Type);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.False(game.Pending!.Cancellable);
        Assert.Contains(game.Pending.Affordances, option => option.AnchorId == source.ObjectId);
    }

    [Rule("rr:limit")]
    [Fact]
    public void LimitedAbilitiesOnOneCardHaveIndependentUses()
    {
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [ { "card": "{{AuthoredCards.AuntMay}}", "abilities": [
                  {
                    "trigger": { "event": "WhenActionTriggered", "timing": "ForcedAction", "subject": "game" },
                    "limitPerRound": 1,
                    "effect": { "draw": { "player": "you", "count": 1 } }
                  },
                  {
                    "trigger": { "event": "WhenActionTriggered", "timing": "ForcedAction", "subject": "game" },
                    "limitPerRound": 1,
                    "effect": { "draw": { "player": "you", "count": 1 } }
                  }
                ] } ] }
                """));
        var (game, world) = Playing(
            board => InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;

        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Take(game.Pending!.Affordances[0].Id));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.False(game.Pending!.Cancellable);
        game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances).Id));

        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }

    [Rule("rr:event.1")]
    [Fact]
    public void PlayingAnEventChoosesOneOfItsTriggeredAbilities()
    {
        // Both actions belong to the same event. Their affordance ids retain
        // the printed ordinal, so choosing the second resolves only its two-card
        // draw and does not also resolve the first ability.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                """
                { "cards": [ { "card": "01003", "abilities": [
                  { "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "effect": { "draw": { "player": "you", "count": 1 } } },
                  { "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "effect": { "draw": { "player": "you", "count": 2 } } }
                ] } ] }
                """));
        Card? eventCard = null;
        var (game, world) = Playing(
            board =>
            {
                Hand(board, AuthoredCards.Backflip, 0);
                eventCard = board.CreateCard(AuthoredCards.Backflip, board.Seats[0].Hand);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var choices = game.Pending!.Affordances
            .Where(option => option.AnchorId == eventCard!.ObjectId)
            .ToList();

        Assert.Equal(2, choices.Count);
        game.Resolve(Decision.Take(choices[1].Id));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, eventCard!.Area.Type);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:choose-game-element.2")]
    [Rule("rr:event.3")]
    [Fact]
    public void AnEventWithNoValidTargetCannotBeOfferedOrForged()
    {
        // If a player-card ability requires targets and "there are no valid
        // targets for any part of the ability, the ability cannot be
        // initiated." The same check runs again at execution, before the event
        // leaves the hand or a payment source can be spent.
        var runner = Runner(
            AuthoredCards.Backflip,
            "Action",
            """{ "chooseCard": { "from": { "query": "minions" }, "effect": { "discard": "chosen" } } }""");
        Card? card = null;
        var (_, world) = Playing(
            board =>
            {
                Hand(board, AuthoredCards.Backflip, 0);
                card = board.CreateCard(AuthoredCards.Backflip, board.Seats[0].Hand);
            },
            hero: true,
            abilities: runner);

        Assert.DoesNotContain(
            runner.Actions(world, 0), action => action.Card == card!.ObjectId);

        var forged = new PendingAbility(card!.ObjectId, AbilityType.Action, 0);
        Assert.Throws<RulesNotImplementedException>(
            () => runner.Act(world, forged, [], []));
        Assert.Same(world.Seats[0].Hand, card.Area);
    }

    [Rule("rr:limit.1")]
    [Fact]
    public void ACancelledLimitedAttackStillUsesItsLimit()
    {
        var runner = Runner(
            "01017",
            "Action",
            """{ "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": { "attack": { "target": "chosen", "effect": { "dealAttackDamage": { "cards": "chosen", "amount": 1 } } } } } }""",
            limit: 1);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
                Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Stunned);
            },
            hero: true,
            abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(villain.ObjectId));

        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(0, villain.Damage);
        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:max-maximum")]
    [Rule("rr:max-maximum.1")]
    [Fact]
    public void APeriodMaximumIsSharedByTitleAcrossPlayersAndExpires()
    {
        // “Max 1 per round” is across all copies by title for all players,
        // unlike a Limit, which belongs to each instance of an ability.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            maximum: "Round");
        Card? first = null;
        Card? second = null;
        var (_, world) = Playing(
            board =>
            {
                first = InPlay(board, AuthoredCards.AuntMay);
                second = board.CreateCard(
                    AuthoredCards.AuntMay,
                    board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == first!.ObjectId);

        runner.Act(world, action, [], []);

        Assert.DoesNotContain(
            runner.Actions(world, 1), pending => pending.Card == second!.ObjectId);
        world.Effects.Expire(TimingPoints.EndOfRound);
        Assert.Contains(
            runner.Actions(world, 1), pending => pending.Card == second!.ObjectId);
    }

    [Rule("rr:max-maximum.1")]
    [Fact]
    public void APhaseMaximumExpiresAtTheEndOfEitherPhase()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            maximum: "Phase");
        Card? first = null;
        Card? second = null;
        var (_, world) = Playing(
            board =>
            {
                first = InPlay(board, AuthoredCards.AuntMay);
                second = InPlay(board, AuthoredCards.AuntMay);
            },
            abilities: runner);
        runner.Act(
            world,
            Assert.Single(runner.Actions(world, 0), pending =>
                pending.Card == first!.ObjectId),
            [],
            []);

        Assert.DoesNotContain(
            runner.Actions(world, 0), pending => pending.Card == second!.ObjectId);
        world.Effects.Expire(TimingPoints.EndOfPhase);
        Assert.Contains(
            runner.Actions(world, 0), pending => pending.Card == second!.ObjectId);
    }

    [Rule("rr:max-maximum.1")]
    [Fact]
    public void AGameMaximumSurvivesPhaseAndRoundBoundaries()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            maximum: "Game");
        Card? first = null;
        Card? second = null;
        var (_, world) = Playing(
            board =>
            {
                first = InPlay(board, AuthoredCards.AuntMay);
                second = InPlay(board, AuthoredCards.AuntMay);
            },
            abilities: runner);
        runner.Act(
            world,
            Assert.Single(runner.Actions(world, 0), pending =>
                pending.Card == first!.ObjectId),
            [],
            []);

        world.Effects.Expire(TimingPoints.EndOfPhase);
        world.Effects.Expire(TimingPoints.EndOfRound);

        Assert.DoesNotContain(
            runner.Actions(world, 0), pending => pending.Card == second!.ObjectId);
    }

    [Rule("rr:max-maximum.1.1")]
    [Fact]
    public void ACanceledUseStillCountsTowardACardMaximum()
    {
        var runner = Runner(
            "01017",
            "Action",
            """{ "attack": { "target": { "query": "villain" }, "effect": { "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } } } }""",
            maximum: "Round");
        Card? first = null;
        Card? second = null;
        var (_, world) = Playing(
            board =>
            {
                first = board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
                second = board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
                Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Stunned);
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == first!.ObjectId);

        runner.Act(world, action, [], []);

        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.DoesNotContain(
            runner.Actions(world, 0), pending => pending.Card == second!.ObjectId);
    }

    [Rule("rr:max-maximum.5")]
    [Fact]
    public void APerInstanceMaximumIsSharedAcrossCopiesForOneOccurrence()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Response",
            """{ "draw": { "player": "you", "count": 1 } }""",
            eventName: "WhenDamageWouldBeDealt",
            maximum: "Instance");
        Card? first = null;
        Card? second = null;
        var (_, world) = Playing(
            board =>
            {
                first = InPlay(board, AuthoredCards.AuntMay);
                second = InPlay(board, AuthoredCards.AuntMay);
            },
            abilities: runner);
        var occurrence = new Occurrence(
            91,
            ["WhenDamageWouldBeDealt"],
            Player: 0,
            Target: world.Seats[0].IdentityCard.ObjectId);
        var offered = runner.Waiting(world, occurrence, WindowKind.Response);

        Assert.Equal(2, offered.Count);
        runner.Resolve(world, occurrence, offered.Single(pending =>
            pending.Card == first!.ObjectId), [], []);

        Assert.Empty(runner.Waiting(world, occurrence, WindowKind.Response));
        Assert.Equal(
            2,
            runner.Waiting(
                world,
                new Occurrence(
                    92,
                    ["WhenDamageWouldBeDealt"],
                    Player: 0,
                    Target: world.Seats[0].IdentityCard.ObjectId),
                WindowKind.Response).Count);
        Assert.NotNull(second);
    }

    [Rule("rr:max-maximum.6")]
    [Fact]
    public void AMaximumWithinAnAbilityCapsOnlyThatResolution()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "dealDamage": {
              "cards": { "query": "villain" },
              "amount": { "min": [ 20, 10 ] }
            } }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);

        runner.Act(world, action, [], []);

        Assert.Equal(10, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:labeled-ability.5")]
    [Rule("rr:labeled-ability.6")]
    [Rule("rr:labeled-ability.6.2")]
    [Fact]
    public void MultiLabeledAbilityCancelsOnceAfterCostsAndBeforeAnyEffect()
    {
        // Crosscounter's attack/defense/thwart labels are one ability. A stun
        // or confusion cancels the whole post-arrow effect, removes every
        // matching status, and leaves the already-paid exhaustion cost paid.
        var runner = Runner(
            "01017",
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "exhaust": "this" }""",
            limit: 1,
            labels: "[ \"attack\", \"defense\", \"thwart\" ]");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
                Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Stunned);
                Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Confused);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(source!.Ready);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        Assert.False(Statuses.Has(
            world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.False(Statuses.Has(
            world, world.Seats[0].IdentityCard, Statuses.Confused));
    }

    [Rule("rr:labeled-ability.2")]
    [Rule("rr:labeled-ability.6")]
    [Rule("rr:retaliate-x.1")]
    [Fact]
    public void LabeledPowerDoesNotBeginAgainDuringItsEffect()
    {
        // A labeled ability is canceled "when the player initiates" it. The
        // stun gained after initiation therefore remains in play and cannot
        // retroactively cancel the attack child of the already-running ability.
        var runner = Runner(
            "01017",
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "giveStatus": { "card": "you", "status": "stunned" } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """,
            labels: "[ \"attack\" ]");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                board.Effects.Register(new ContinuousEffect(
                    EffectSource.LastingEffect,
                    Kind: "retaliate",
                    Amount: 1,
                    Card: villain.ObjectId,
                    Affects: villain.ObjectId));
            },
            hero: true,
            abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.True(Statuses.Has(
            world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(1, villain.Damage);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:labeled-ability.1")]
    [Rule("rr:upgrade.4")]
    [Rule("rr:piercing.1")]
    [Fact]
    public void LabeledPerformerSurvivesAChoiceContinuation()
    {
        // An upgrade attached to "another friendly character" attributes its
        // labeled ability to that character. The chosen ally remains the
        // performer after the prompt, so its Piercing discards each Tough card.
        var runner = Runner(
            "01017",
            "Action",
            """
            { "chooseCard": {
              "from": { "query": "attackableEnemies" },
              "effect": { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            } }
            """,
            labels: "[ \"attack\" ]");
        Card? source = null;
        Card? ally = null;
        var (game, world) = Playing(
            board =>
            {
                ally = board.CreateCard(
                    AuthoredCards.BlackCat,
                    board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                source = board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), ally.ObjectId, cardOwner: 0));
                board.Effects.Register(new ContinuousEffect(
                    EffectSource.LastingEffect,
                    Kind: Keywords.Piercing,
                    Card: ally.ObjectId,
                    Affects: ally.ObjectId));
            },
            hero: true,
            abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, villain, Statuses.Tough);
        var action = Assert.Single(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(villain.ObjectId));

        Assert.False(Statuses.Has(world, villain, Statuses.Tough));
        Assert.Equal(1, villain.Damage);
    }

    [Rule("rr:labeled-ability.2")]
    [Fact]
    public void AttackEnvelopeWithoutAPowerLifecycleFailsBeforeCosts()
    {
        // The whole labeled ability "is considered to be an attack". A raw
        // damage effect has no saveable attack occurrence for interrupts,
        // responses, or Retaliate, so this unsupported shape is refused before
        // its exhaust cost instead of resolving as plausible non-attack damage.
        var runner = Runner(
            "01017",
            "Action",
            """{ "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } }""",
            cost: """{ "exhaust": "this" }""",
            labels: "[ \"attack\" ]");
        Card? source = null;
        var (_, world) = Playing(
            board => source = board.CreateCard(
                "01017",
                board.AreaOf(
                    DeckType.UpgradesArea, PlayArea.Of(0),
                    board.Seats[0].IdentityCard.ObjectId, cardOwner: 0)),
            hero: true);
        var forged = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => runner.Act(world, forged, [], []));

        Assert.Contains("saveable attack power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source.Ready);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:labeled-ability.2")]
    [Fact]
    public void AutomaticAttackEnvelopeCannotBypassLifecyclePreflight()
    {
        // Automatic entry points use the same envelope gate as Actions. A When
        // Revealed ability with raw attack damage therefore raises before the
        // damage instead of bypassing the attack occurrence and Retaliate.
        var runner = Runner(
            "01017",
            "WhenRevealed",
            """{ "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } }""",
            eventName: Steps.CardRevealed,
            labels: "[ \"attack\" ]");
        Card? source = null;
        var (_, world) = Playing(
            board => source = board.CreateCard(
                "01017", board.AreaOf(DeckType.RevealingArea)),
            hero: true);

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => runner.WhenRevealed(world, source!, 0));

        Assert.Contains("saveable attack power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:labeled-ability.2")]
    [Fact]
    public void EveryAttackEnvelopeBranchMustEnterTheLifecycle()
    {
        // An inactive branch cannot lend its attack node to the active branch.
        // Here the hero-form path only draws, so the envelope would not be an
        // attack on that path and is rejected before the exhaust cost.
        var runner = Runner(
            "01017",
            "Action",
            """
            { "if": {
              "test": { "inForm": { "player": "you", "form": "hero" } },
              "then": { "draw": { "player": "you", "count": 1 } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              } }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            labels: "[ \"attack\" ]");
        Card? source = null;
        var (_, world) = Playing(
            board => source = board.CreateCard(
                "01017",
                board.AreaOf(
                    DeckType.UpgradesArea, PlayArea.Of(0),
                    board.Seats[0].IdentityCard.ObjectId, cardOwner: 0)),
            hero: true);
        int held = world.Seats[0].Hand.Cards.Count;
        var forged = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);

        Assert.Throws<RulesNotImplementedException>(
            () => runner.Act(world, forged, [], []));

        Assert.True(source.Ready);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:labeled-ability.5")]
    [Rule("rr:labeled-ability.6")]
    [Fact]
    public void EnvelopeCannotHideAnUndeclaredPower()
    {
        // The envelope's labels are the whole set. An attack-only ability may
        // not append a thwart that skips Confused merely because the attack
        // already persisted its performer into the continuation.
        var runner = Runner(
            "01017",
            "Action",
            """
            { "seq": [
              { "attack": {
                "target": { "query": "villain" },
                "effect": { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              } },
              { "thwart": {
                "target": { "query": "mainScheme" },
                "effect": { "removeThreat": {
                  "scheme": { "query": "mainScheme" }, "amount": 1
                } }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""",
            labels: "[ \"attack\" ]");
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
                Statuses.Give(
                    board, board.Seats[0].IdentityCard, Statuses.Confused);
            },
            hero: true);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long threat = scheme.Tokens.GetValueOrDefault("k_threat");
        var forged = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => runner.Act(world, forged, [], []));

        Assert.Contains("absent from its ability labels", thrown.Message);
        Assert.True(source.Ready);
        Assert.True(Statuses.Has(
            world, world.Seats[0].IdentityCard, Statuses.Confused));
        Assert.Equal(0, villain.Damage);
        Assert.Equal(threat, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:lasting-effects.6")]
    [Fact]
    public void AnUntilEndOfAttackEffectCannotBeginOutsideAnAttack()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "grantUntil": { "card": "this", "keyword": "attack", "amount": 1, "until": "EndOfAttack" } }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:action.2.2")]
    [Rule("rr:forced.3")]
    [Rule("rr:forced.3.1")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnIllegalForcedActionDoesNotPreventPhaseCompletion(bool targetless)
    {
        string effect = targetless
            ? """{ "chooseCard": { "from": { "query": "minions" }, "effect": { "draw": { "player": "you", "count": 1 } } } }"""
            : """{ "draw": { "player": "you", "count": 1 } }""";
        string? cost = targetless ? null : """{ "spend": "BBBBBBBBBBBB" }""";
        var runner = Runner(AuthoredCards.AuntMay, "ForcedAction", effect, cost: cost);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Decline);

        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.True(source!.Ready);
    }

}
