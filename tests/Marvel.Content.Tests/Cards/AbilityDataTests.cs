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
            if (ability.Trigger.Timing == AbilityType.Constant)
            {
                Assert.Null(ability.Trigger.Event);
                continue;
            }

            Assert.NotNull(ability.Trigger.Event);
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

    [Rule("rr:player-turn.5")]
    [Rule("rr:resource-ability.1")]
    [Fact]
    public void EveryAbilityReachesTheBoardSomehow()
    {
        // A timing that reaches the board through none of these routes is an
        // ability nothing ever offers: `AbilityWindow` would drop it, no
        // occurrence would run it, and no turn would list it.
        //
        // **The routes past the first two are newer.** `rr:player-turn.5`
        // makes an "Action" one of the six things a turn offers rather than
        // something timed around an occurrence, and `AbilityTypes.PriorityOf`
        // has always refused to give it a tier for exactly that reason. Until
        // an action could be triggered, this test read as "in a window or the
        // occurrence", which was true because nothing else could be authored.
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

                // The fifth, and the one that is not an offer at all.
                // `rr:ability` makes a constant ability active "as soon as its
                // card enters play"; nothing triggers it, so it reaches the
                // board by being read off it -- `ICardAbilities.Constant`,
                // asked whenever anything looks at the continuous effects.
                || timing == AbilityType.Constant,
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
        // `01130` Whirlwind, a Masters of Evil minion: no scenario these tests
        // build reaches it, so it stays unauthored. It used to be `01100`
        // Enhanced Ivory Horn, which stopped being an example of an unread card
        // the moment somebody read it.
        var card = world.CreateCard("01130", world.AreaOf(DeckType.RevealingArea));

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
        // Stated as a set so that a card added to the dataset is a deliberate
        // act with a test behind it, not something that accumulates. The rule
        // this file is under: a card is authored when something reaches it.
        string[] named =
        [
            AuthoredCards.SpiderMan, AuthoredCards.PeterParker, AuthoredCards.AuntMay,
            AuthoredCards.ArmoredSuit, AuthoredCards.Charge, AuthoredCards.IvoryHorn,
            AuthoredCards.Shocker,
            AuthoredCards.HardToKeepDown, AuthoredCards.ImTough,
            AuthoredCards.BreakinAndTakin, AuthoredCards.BombScare,
            AuthoredCards.Explosion, AuthoredCards.HydraBomber, AuthoredCards.FalseAlarm,
            AuthoredCards.CaughtOffGuard, AuthoredCards.RhinoTwo,
            AuthoredCards.Stampede, AuthoredCards.EvictionNotice, AuthoredCards.HighwayRobbery,
            AuthoredCards.SweepingSwoop, AuthoredCards.VulturesPlans,
            AuthoredCards.Advance, AuthoredCards.Assault, AuthoredCards.GangUp,
            AuthoredCards.ShadowOfThePast, AuthoredCards.Exhaustion,
            AuthoredCards.Masterplan, AuthoredCards.UnderFire, AuthoredCards.RhinoThree,
            AuthoredCards.Boomerang, AuthoredCards.Beetle, AuthoredCards.WhiteRabbit, AuthoredCards.SinisterOnslaught, AuthoredCards.CrimePays, AuthoredCards.SyndicateShocker, AuthoredCards.SpeedDemon,
            .. AuthoredCards.Unus,
            .. AuthoredCards.ReadAndSilent,
        ];

        Assert.Equal(
            named.Order(StringComparer.Ordinal),
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
