using System.Text.Json;
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

    [Rule("rr:printed.1")]
    [Rule("rr:text-box.1.1")]
    [Fact]
    public void PrintedTextBoxResourceIconsAreExplicitAbilityData()
    {
        // Icons printed within a text box are abilities within that box, and
        // only those printed icons may pay a “printed resources” cost. The
        // generated value alone is insufficient evidence: Pepper Potts also
        // generates resources, but copies them from another card at runtime.
        var peter = Assert.Single(AuthoredCards.Book.On("01001b"));
        var pepper = Assert.Single(AuthoredCards.Book.On("01033"));

        Assert.Equal("B", peter.PrintedResources);
        Assert.Empty(pepper.PrintedResources);
    }

    [Rule("rr:uses-x-type")]
    [Fact]
    public void StartingCounterPoolsAreExplicitAbilityData()
    {
        // "Each card with this keyword also has an ability that references the
        // type of use established by the keyword as part of the cost." Uses
        // supplies both entry counters and discard-at-zero. Hawkeye's printed
        // sentence supplies only entry counters, so the boolean is a behavior
        // distinction rather than a spelling distinction.
        var expected = new Dictionary<string, CardCounterPool>(StringComparer.Ordinal)
        {
            ["01008"] = new("web", 3, Uses: true),
            ["01056"] = new("attack", 3, Uses: true),
            ["01064"] = new("snoop", 3, Uses: true),
            ["01066"] = new("arrow", 4, Uses: false),
            ["01080"] = new("medical", 3, Uses: true),
        };

        Assert.Equal(
            expected.OrderBy(pair => pair.Key, StringComparer.Ordinal),
            AuthoredCards.Book.CounterPools!.OrderBy(
                pair => pair.Key, StringComparer.Ordinal));
    }

    [Fact]
    public void PrintedTextBoxResourceDataMustMatchTheResourceAbility()
    {
        var refused = Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "01001b", "abilities": [ {
              "trigger": { "event": "WhenActionTriggered", "timing": "Resource",
                           "subject": "this" },
              "printedResources": "B",
              "effect": { "generate": "Y" }
            } ] } ] }
            """));

        Assert.Contains("matching fixed resource ability", refused.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void OneAbilityCannotDeclareSeveralMaximumPeriods()
    {
        var refused = Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "01003", "abilities": [ {
              "trigger": { "event": "WhenActionTriggered", "timing": "Action",
                           "subject": "game" },
              "maxPerRound": 1,
              "maxPerGame": 1,
              "effect": { "draw": { "player": "you", "count": 1 } }
            } ] } ] }
            """));

        Assert.Contains("several maxima", refused.Message, StringComparison.Ordinal);
    }

    [Rule("rr:ability.14")]
    [Fact]
    public void AQuotedTimingTriggerIsAuthoredOnlyAsAReference()
    {
        // Enhanced Spider-Sense says to cancel "When Revealed" effects. The
        // quotation marks refer to abilities on the treachery; they do not
        // give Spider-Sense a second When Revealed ability of its own.
        var written = AuthoredCards.Book.On("01004").ToList();

        var ability = Assert.Single(written);
        Assert.Equal(AbilityType.Interrupt, ability.Trigger.Timing);
        Assert.Equal(Steps.CardRevealed, ability.Trigger.Event);
        Assert.DoesNotContain(
            written, candidate => candidate.Trigger.Timing == AbilityType.WhenRevealed);
    }

    [Rule("rr:ability.5")]
    [Fact]
    public void EveryTriggerNamesAConditionTheEngineActuallyProduces()
    {
        // The failure this exists for: an ability whose trigger is spelled
        // `WhenEnemyAttack` sits in the dataset, parses, validates, and never
        // fires. Nothing else in the suite would notice, because "a card that
        // does nothing" and "a card that was never reached" look identical.
        foreach (var ability in AuthoredCards.Book.Abilities)
        {
            // `rr:ability.5` splits abilities in two by whether they are
            // "prefaced by a bold timing trigger", and a constant ability is the
            // half that is not -- so it names no condition, and a condition on
            // one would be an author having believed it triggers on something.
            // `rr:ability.5` splits abilities by whether they are prefaced by
            // a bold timing trigger, and a constant is the half that is not.
            // `rr:setup-triggered-ability.2` is the other eventless case and a
            // different reason: it *is* triggered, but to a step of setup
            // rather than to something happening in the game.
            if (ability.Trigger.Timing is AbilityType.Constant or AbilityType.Setup)
            {
                Assert.Null(ability.Trigger.Event);
                continue;
            }

            // Resource and Special abilities are invoked by their dedicated
            // payment and Special-resolution APIs. Activation completion is
            // likewise delivered as the result of a suspended activation.
            // Their event labels identify those direct calls; none is an
            // occurrence-window condition in Steps.EveryCondition.
            if (ability.Trigger.Timing is AbilityType.Resource or AbilityType.Special
                || string.Equals(
                    ability.Trigger.Event, "WhenActivationCompleted",
                    StringComparison.Ordinal))
            {
                Assert.NotNull(ability.Trigger.Event);
                continue;
            }

            Assert.NotNull(ability.Trigger.Event);
            Assert.True(
                Steps.EveryCondition.Contains(ability.Trigger.Event),
                $"'{ability.Card}' triggers on '{ability.Trigger.Event}', which no step "
                + $"produces. The engine's conditions are: "
                + string.Join(", ", Steps.EveryCondition.Order(StringComparer.Ordinal)));

            // The second condition of a `rr:triggering-condition.2` pair, held
            // against the same set for the same reason. A card gated on a
            // condition nothing produces never fires at all, which is worse
            // than one that fires too often and just as invisible.
            if (ability.Trigger.Also is { } also)
            {
                Assert.True(
                    Steps.EveryCondition.Contains(also),
                    $"'{ability.Card}' also requires '{also}', which no step produces");
            }
        }
    }

    [Rule("rr:triggering-condition.2")]
    [Fact]
    public void ASecondConditionNothingProducesIsRefusedWhenTheDatasetIsRead()
    {
        // Refused where a typo is cheapest to find. The two fields speak one
        // vocabulary -- the engine's own spelling of a triggering condition --
        // and neither of them gets an escape hatch.
        var refused = Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "01105", "abilities": [ {
                "trigger": { "event": "WhenCardDefeated", "alsoHappened": "WhenUnusAttacks",
                             "timing": "ForcedResponse", "subject": "this" },
                "effect": { "discard": "this" }
            } ] } ] }
            """));

        Assert.Contains("'WhenUnusAttacks'", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryWordTheDatasetUsesIsWrittenDownInTheDslDocument()
    {
        // `docs/card-dsl.md` is the document that says what this vocabulary is,
        // and a vocabulary nobody wrote down is one nobody can author against.
        // The failure this exists for is quiet: a node added to the interpreter
        // for one card works, the card ships, and the table that claims to list
        // what is implemented silently stops being true. Measured when this was
        // written, fourteen words were already only in the code.
        //
        // Held against the *whole* document rather than the slice table alone,
        // because a word may be introduced by the prose or by one of the cards
        // written out in full — and either of those is somebody having written
        // it down.
        var doc = File.ReadAllText(RepositoryPaths.Repository("docs", "card-dsl.md"));
        var used = new SortedSet<string>(StringComparer.Ordinal);

        using var json = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")));

        foreach (var card in json.RootElement.GetProperty("cards").EnumerateArray())
        {
            // Only the parts of a row that are vocabulary. `card`, `name` and
            // `note` are a printed id and prose about it, and the same word
            // `card` is a *field* inside a node — which is why this picks the
            // rows apart here rather than skipping a name wherever it appears.
            if (card.TryGetProperty("attachTo", out var attach))
            {
                Words(attach, used);
            }

            if (card.TryGetProperty("controlledBy", out _))
            {
                used.Add("controlledBy");
            }

            if (card.TryGetProperty("startingCounters", out var counters))
            {
                used.Add("startingCounters");
                Words(counters, used);
            }

            if (!card.TryGetProperty("abilities", out var cardAbilities))
            {
                continue;
            }

            foreach (var ability in cardAbilities.EnumerateArray())
            {
                foreach (var part in ability.EnumerateObject())
                {
                    if (part.Name == "name")
                    {
                        continue;
                    }

                    used.Add(part.Name);
                    Words(part.Value, used);
                }
            }
        }

        // What was collected, before what is missing from it. A walk that
        // quietly gathered nothing, or gathered `query` instead of the query's
        // name, would find nothing missing and pass — so the two things this
        // has to read are named.
        Assert.Contains("placeThreat", used);
        Assert.Contains("alliesYouControl", used);
        Assert.DoesNotContain("query", used);

        var missing = used.Where(word => !doc.Contains(word, StringComparison.Ordinal)).ToList();

        Assert.True(
            missing.Count == 0,
            $"{missing.Count} word(s) the dataset uses appear nowhere in docs/card-dsl.md: "
            + string.Join(", ", missing));
    }

    /// <summary>
    /// Every name an ability tree uses: the keys, and a query's value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A query is the one place the vocabulary is in the value rather than the
    /// key — <c>{ "query": "alliesYouControl" }</c> — because a query names a
    /// set of cards and the node names the act of asking.
    /// </para>
    /// <para>
    /// Only the keys, and a query's value. A word in any other value position
    /// is a card id, a keyword the engine already reads, or a trait — all held
    /// against something else already.
    /// </para>
    /// </remarks>
    private static void Words(JsonElement element, SortedSet<string> found)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var field in element.EnumerateObject())
                {
                    if (field.Name == "query" && field.Value.ValueKind == JsonValueKind.String)
                    {
                        found.Add(field.Value.GetString()!);
                        continue;
                    }

                    found.Add(field.Name);
                    Words(field.Value, found);
                }

                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Words(item, found);
                }

                break;

            default:
                break;
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

    [Rule("rr:player-turn.5")]
    [Rule("rr:resource-ability.1")]
    [Fact]
    public void EveryAbilityReachesTheBoardSomehow()
    {
        // A timing that reaches the board through none of these routes is an
        // ability nothing ever offers: `AbilityWindow` would drop it, no
        // occurrence would run it, and no turn would list it.
        //
        // **An action is not in a window.** `rr:player-turn.5` makes it one of
        // the six things a turn offers rather than something timed around an
        // occurrence, which is why `AbilityTypes.PriorityOf` refuses to give it
        // a tier. So the reachable routes are three, not two.
        foreach (var ability in AuthoredCards.Book.Abilities)
        {
            var timing = ability.Trigger.Timing;
            Assert.True(
                AbilityTypes.IsInterrupt(timing)
                || AbilityTypes.IsResponse(timing)
                || AbilityTypes.PriorityOf(timing) == TimingPriority.Occurrence
                || timing is AbilityType.Action or AbilityType.ForcedAction

                // The fourth route, and the newest. `rr:resource-ability.1`
                // makes one triggerable "anytime the player who controls the
                // ability is generating resources to pay a cost" -- so it is
                // neither timed around an occurrence nor a turn option, it is
                // reached while a cost is being paid.
                || timing == AbilityType.Resource

                // `rr:setup-triggered-ability` -- resolved during setup, so it
                // is neither offered nor timed to an occurrence. The deal asks
                // for it by name at `rr:appendix-ii-setup.step.12`.
                || timing == AbilityType.Setup

                // The fifth, and the one that is not an offer at all.
                // `rr:ability` makes a constant ability active "as soon as its
                // card enters play"; nothing triggers it, so it reaches the
                // board by being read off it -- `ICardAbilities.Constant`,
                // asked whenever anything looks at the continuous effects.
                || timing == AbilityType.Constant

                // Wakanda Forever explicitly schedules each printed Special
                // ability through ResolveSpecial. It is neither an occurrence
                // window nor a general player-turn action.
                || timing == AbilityType.Special,
                $"'{ability.Card}' has timing '{timing}', which nothing would offer");
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
        // Every core face is now authored. Norman Osborn is printed in the
        // Green Goblin pack and deliberately remains outside this slice, so he
        // keeps the fail-closed contract observable without making a core game
        // stop on missing data.
        var card = world.CreateCard("02001a", world.AreaOf(DeckType.RevealingArea));

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
    [InlineData("""{"cards":[{"card":"01099","abilities":[{"trigger":{"event":"WhenAttackInitiated","timing":"Interrupt","actor":"nobody"},"effect":{"seq":[]}}]}]}""", "nobody")]
    [InlineData("""{"cards":[{"card":"01099","abilities":[{"trigger":{"event":"WhenAttackInitiated","timing":"Shouting","actor":"this"},"effect":{"seq":[]}}]}]}""", "Shouting")]
    [InlineData("""{"cards":[{"card":"01099","abilities":[{"trigger":{"event":"WhenAttackInitiated","timing":"Interrupt","actor":"this"}}]}]}""", "no 'effect'")]
    [InlineData("""{"cards":[{"card":"01099","abilities":[{"trigger":{"event":"WhenAttackInitiated","timing":"Interrupt","actor":"this"},"anyPlayer":"yes","effect":{"seq":[]}}]}]}""", "non-boolean")]
    [InlineData("""{"cards":[{"card":"01099","controlledBy":"lastPlayer"}]}""", "other than 'firstPlayer'")]
    [InlineData("""{"cards":[{"card":"01099","startingCounters":{"type":"web","count":3,"uses":true,"extra":1}}]}""", "extra")]
    [InlineData("""{"cards":[{"card":"01099","startingCounters":{"type":"web","count":0,"uses":true}}]}""", "positive integer")]
    [InlineData("""{"cards":[{"card":"01099","startingCounters":{"type":"web","count":3}}]}""", "boolean 'uses'")]
    [InlineData("""{"cards":[{"card":"01099"}]}""", "neither abilities nor placement")]
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
              "trigger":{"event":"WhenAttackInitiated","timing":"Interrupt","actor":"this"},
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
        var hero = world.CreateCard("01001a", world.Seats[0].Hero);
        world.Seats[0].IdentityCard = hero;
        var card = world.CreateCard("01105", world.AreaOf(DeckType.RevealingArea));

        var book = AbilityCatalog.Parse(
            """
            {"cards":[{"card":"01105","abilities":[
              {"trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
               "effect":{"giveStatus":{"card":"yourHero","status":"tough"}}},
              {"trigger":{"event":"WhenCardRevealed","timing":"Interrupt","subject":"this"},
               "effect":{"giveStatus":{"card":"yourHero","status":"stunned"}}}]}]}
            """);

        new Marvel.Cards.Run.AbilityRunner(book).WhenRevealed(world, card, 0);

        Assert.True(Statuses.Has(world, hero, "tough"));
        Assert.False(Statuses.Has(world, hero, "stunned"));
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
              {"card":"01094","abilities":[{"trigger":{"event":"WhenAttackInitiated",
                "timing":"Interrupt","actor":"this"},"effect":{"seq":[]}}]},
              {"card":"01001a","abilities":[{"trigger":{"event":"WhenAttackInitiated",
                "timing":"Interrupt","actor":"this"},"effect":{"seq":[]}}]}]}
            """);
        var runner = new Marvel.Cards.Run.AbilityRunner(book);

        var onEncounter = Occurrence.ForAttack(
            1, [Steps.AttackInitiated], world, Printed,
            villain.ObjectId, identity.ObjectId, player: 0);
        var onPlayerCard = Occurrence.ForAttack(
            2, [Steps.AttackInitiated], world, Printed,
            identity.ObjectId, villain.ObjectId);

        Assert.Equal(
            World.Scenario,
            Assert.Single(runner.Waiting(world, onEncounter, WindowKind.Interrupt)).Player);
        Assert.Equal(
            1,
            Assert.Single(runner.Waiting(world, onPlayerCard, WindowKind.Interrupt)).Player);
    }

    [Rule("rr:friendly")]
    [Fact]
    public void TriggersMatchNamedRolesWithoutSourceSpecificEvents()
    {
        var world = new World(Printed, players: 1);
        world.CreateSeat("p0");
        var hero = world.CreateCard("01001a", world.Seats[0].Hero);
        var ally = world.CreateCard(
            "01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var minion = world.CreateCard(
            "01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        var runner = new Marvel.Cards.Run.AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [
              { "card": "01001a", "abilities": [ {
                "trigger": { "event": "WhenAttackInitiated", "timing": "Interrupt",
                             "actor": "friendly", "target": "friendly" },
                "effect": { "seq": [] }
              } ] },
              { "card": "01094", "abilities": [ {
                "trigger": { "event": "WhenAttackInitiated", "timing": "Interrupt",
                             "actor": "enemy", "target": "enemy" },
                "effect": { "seq": [] }
              } ] }
            ] }
            """));

        var friendly = Occurrence.ForAttack(
            1, [Steps.AttackInitiated], world, Printed, hero.ObjectId, ally.ObjectId);
        var enemy = Occurrence.ForAttack(
            2, [Steps.AttackInitiated], world, Printed, villain.ObjectId, minion.ObjectId);

        Assert.Equal(
            hero.ObjectId,
            Assert.Single(runner.Waiting(world, friendly, WindowKind.Interrupt)).Card);
        Assert.Equal(
            villain.ObjectId,
            Assert.Single(runner.Waiting(world, enemy, WindowKind.Interrupt)).Card);
    }

    [Fact]
    public void EveryTraitACardNamesIsATraitSomePrintedCardCarries()
    {
        // The same failure as a misspelled trigger, one field along. A card
        // asking for `Criminal` when the engine stores `CRIMINAL` parses,
        // validates, and quietly matches nothing -- and "the query found no
        // enemies" is a board a real game reaches, so nothing downstream can
        // tell the typo from the empty table.
        //
        // The dataset names traits as the engine spells them, which is the rule
        // `AbilityTrigger.Event` states for conditions: a translation table
        // between the printed word and the stored one is a second vocabulary,
        // and a second vocabulary drifts.
        var real = new HashSet<string>(
            AuthoredCards.EveryPrintedTrait(), StringComparer.Ordinal);

        foreach (var ability in AuthoredCards.Book.Abilities)
        {
            foreach (string named in Traits(ability.Effect))
            {
                Assert.True(
                    real.Contains(named),
                    $"'{ability.Card}' names the trait '{named}', which no printed card "
                    + "carries. Traits are stored upper-case with spaces underscored -- "
                    + "`MASTERS_OF_EVIL`, not `Masters of Evil`.");
            }
        }
    }

    /// <summary>Every trait one effect tree names, however deep.</summary>
    private static IEnumerable<string> Traits(AbilityNode node) => Traits(node.Argument, node.Kind);

    private static IEnumerable<string> Traits(AbilityValue value, string kind)
    {
        switch (value)
        {
            case AbilityValue.Word word when string.Equals(
                kind, "enemiesWithTrait", StringComparison.Ordinal):
                yield return word.Value;
                break;

            case AbilityValue.List list:
                foreach (string found in list.Values.SelectMany(each => Traits(each, kind)))
                {
                    yield return found;
                }

                break;

            case AbilityValue.Map map:
                foreach (var (name, entry) in map.Entries)
                {
                    foreach (string found in Traits(entry, name))
                    {
                        yield return found;
                    }
                }

                break;

            default:
                break;
        }
    }

    [Fact]
    public void TheAuthoredCardsAreTheOnesTheTestsName()
    {
        // The runtime book is the Core Set product boundary. The complete
        // generated card catalog remains available for printed facts, but no
        // later product may acquire executable text accidentally.
        using var cards = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var core = cards.RootElement.GetProperty("cards").EnumerateArray()
            .Where(card => string.Equals(
                card.GetProperty("pack").GetString(), "core", StringComparison.Ordinal))
            .Select(card => card.GetProperty("card_id").GetString()!)
            .ToList();
        Assert.Equal(
            core.Order(StringComparer.Ordinal),
            AuthoredCards.Book.Authored.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ACardReadAndFoundEmptyIsNotACardNobodyRead()
    {
        // The distinction the dataset exists to be able to make. Several of
        // the Rhino scenario's cards carry no ability at all -- a keyword the
        // engine already reads, a printed icon, or a rule restated on the card
        // -- and each is a row saying so.
        //
        // Revealing one resolves to silence, which is correct. Revealing a card
        // nobody has read throws, which is also correct. A dataset that could
        // not tell them apart would have to pick one, and either choice is
        // wrong for half the pool.
        var world = new World(Printed, players: 1);
        world.CreateSeat("p0");
        var runner = AuthoredCards.Runner();

        foreach (string faceId in AuthoredCards.ReadAndSilent)
        {
            var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
            Assert.Empty(runner.WhenRevealed(world, card, 0));
        }
    }

}
