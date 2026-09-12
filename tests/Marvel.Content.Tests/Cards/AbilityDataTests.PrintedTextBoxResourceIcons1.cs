using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Cards;
public sealed class AbilityDataPrintedTextBoxResourceIconsTests : AbilityDataTestBase
{
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
        Assert.Equal(expected.OrderBy(pair => pair.Key, StringComparer.Ordinal), AuthoredCards.Book.CounterPools!.OrderBy(pair => pair.Key, StringComparer.Ordinal));
    }

    [Fact]
    public void PrintedTextBoxResourceDataMustMatchTheResourceAbility()
    {
        var refused = Assert.Throws<AbilityException>(() => AbilityCatalog.Parse("""
            { "cards": [ { "card": "01001b", "abilities": [ {
              "trigger": { "event": "WhenActionTriggered", "timing": "Resource",
                           "subject": "this" },
              "printedResources": "B",
              "effect": { "generate": "Y" }
            } ] } ] }
            """));
        Assert.Contains("matching fixed resource ability", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OneAbilityCannotDeclareSeveralMaximumPeriods()
    {
        var refused = Assert.Throws<AbilityException>(() => AbilityCatalog.Parse("""
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
        Assert.DoesNotContain(written, candidate => candidate.Trigger.Timing == AbilityType.WhenRevealed);
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
            if (ability.Trigger.Timing is AbilityType.Resource or AbilityType.Special || string.Equals(ability.Trigger.Event, "WhenActivationCompleted", StringComparison.Ordinal))
            {
                Assert.NotNull(ability.Trigger.Event);
                continue;
            }

            Assert.NotNull(ability.Trigger.Event);
            Assert.True(Steps.EveryCondition.Contains(ability.Trigger.Event), $"'{ability.Card}' triggers on '{ability.Trigger.Event}', which no step " + $"produces. The engine's conditions are: " + string.Join(", ", Steps.EveryCondition.Order(StringComparer.Ordinal)));
            // The second condition of a `rr:triggering-condition.2` pair, held
            // against the same set for the same reason. A card gated on a
            // condition nothing produces never fires at all, which is worse
            // than one that fires too often and just as invisible.
            if (ability.Trigger.Also is { } also)
            {
                Assert.True(Steps.EveryCondition.Contains(also), $"'{ability.Card}' also requires '{also}', which no step produces");
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
        var refused = Assert.Throws<AbilityException>(() => AbilityCatalog.Parse("""
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
        using var json = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")));
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
        Assert.True(missing.Count == 0, $"{missing.Count} word(s) the dataset uses appear nowhere in docs/card-dsl.md: " + string.Join(", ", missing));
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
        var book = AbilityCatalog.Parse("""
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
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "stalwart", Amount: 1, Card: rhino.ObjectId, Affects: rhino.ObjectId));
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
            Assert.True(Printed.Kind(card) != CardKind.Unknown, $"'{card}' is authored and is not a printed card id");
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
            Assert.True(AbilityTypes.IsInterrupt(timing) || AbilityTypes.IsResponse(timing) || AbilityTypes.PriorityOf(timing) == TimingPriority.Occurrence || timing is AbilityType.Action or AbilityType.ForcedAction // The fourth route, and the newest. `rr:resource-ability.1`
            // makes one triggerable "anytime the player who controls the
            // ability is generating resources to pay a cost" -- so it is
            // neither timed around an occurrence nor a turn option, it is
            // reached while a cost is being paid.
            || timing == AbilityType.Resource // `rr:setup-triggered-ability` -- resolved during setup, so it
            // is neither offered nor timed to an occurrence. The deal asks
            // for it by name at `rr:appendix-ii-setup.step.12`.
            || timing == AbilityType.Setup // The fifth, and the one that is not an offer at all.
            // `rr:ability` makes a constant ability active "as soon as its
            // card enters play"; nothing triggers it, so it reaches the
            // board by being read off it -- `ICardAbilities.Constant`,
            // asked whenever anything looks at the continuous effects.
            || timing == AbilityType.Constant // Wakanda Forever explicitly schedules each printed Special
            // ability through ResolveSpecial. It is neither an occurrence
            // window nor a general player-turn action.
            || timing == AbilityType.Special, $"'{ability.Card}' has timing '{timing}', which nothing would offer");
        }
    }
}
