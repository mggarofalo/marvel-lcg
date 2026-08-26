using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// The engine, against the first three recorded steps of the milestone game.
/// </summary>
/// <remarks>
/// <para>
/// <c>OpeningBoardTests</c> proves the board <c>rhino / spider_man / 12345</c>
/// is dealt from. This proves the loop runs on it: three recorded steps produced
/// as the output of resolving two answers, rather than one board produced by
/// dealing.
/// </para>
/// <para>
/// <b>Why three steps is more than it sounds and less than it reads.</b> The
/// fixture asks for twenty steps and the recording holds seven, because the
/// sampling policy declines every decision and the game is lost in round three.
/// Those seven are only three distinct boards — steps 0, 1 and 2 are one board,
/// 3 and 4 another, 5 and 6 a third — because a declining player moves nothing
/// and the board only changes in the villain phase. So this covers three of the
/// seven recorded digests, and the two remaining transitions both need card
/// abilities.
/// </para>
/// <para>
/// <b>Both halves of the return value are checked, from two fixtures.</b> A port
/// can reproduce all three boards while asking entirely the wrong questions:
/// declining a prompt that should never have been offered leaves exactly the
/// same board as declining the right one. <c>datasets/digest/vectors.json</c>
/// holds the boards, <c>datasets/digest/prompts.json</c> holds the questions,
/// and step <i>n</i> of one is step <i>n</i> of the other.
/// </para>
/// <para>
/// <b>What is deliberately not compared.</b> Affordance ids. They are session
/// handles — the Python engine allocates effect object ids and MARVEL-164
/// measured them drifting across sessions — so <c>(AnchorId, Verb)</c> is the
/// durable key and is what these compare on. See the remarks on
/// <c>Affordance.Id</c>.
/// </para>
/// </remarks>
public sealed class PlayerPhaseTests
{
    private const string Campaign = "rhino";
    private const uint Seed = 12345;
    private static readonly string[] Heroes = ["spider_man"];

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    private static Game Begin() => Begin(out _);

    /// <summary>Begins a game, and hands back the board it is playing.</summary>
    /// <remarks>
    /// <c>Game</c> does not expose its world; a <c>Resolution</c> carries one,
    /// and a test that wants to look at the board before answering anything
    /// needs it earlier than that.
    /// </remarks>
    private static Game Begin(out World world)
    {
        world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, Heroes), Cards),
            [.. Heroes.Select(hero => Setup.Hero(hero).Name)],
            Seed);
        return Game.Begin(world, Cards, AuthoredCards.Runner());
    }

    [Fact]
    public void TheFirstThreeRecordedBoardsAreProducedByFolding()
    {
        var recorded = RecordedDigests();
        var game = Begin();

        // Step 0: dealt, nothing resolved yet.
        Assert.Equal(recorded[0], game.State.Digest().Canonical());

        // Steps 1 and 2: two declines.
        for (int step = 1; step <= 2; step++)
        {
            var result = game.Resolve(Decision.Decline);
            Assert.Same(game.State, result.State);
            Assert.Equal(recorded[step], result.State.Digest().Canonical());
        }
    }

    [Fact]
    public void DecliningChangesNothingAndSaysSo()
    {
        // The event list is the engine's account of what changed. A decline that
        // emitted events would be claiming a change the digest denies, and a
        // digest that moved under an empty event list would be the same lie the
        // other way round. Both are checked together because either alone
        // passes while the pair disagrees.
        var game = Begin();
        string before = game.State.Digest().Canonical();

        for (int step = 1; step <= 2; step++)
        {
            var result = game.Resolve(Decision.Decline);
            Assert.Empty(result.Events);
            Assert.Equal(before, result.State.Digest().Canonical());
        }
    }

    [Fact]
    public void TheRecordingReallyDoesHoldOneBoardForThreeSteps()
    {
        // The premise of the two tests above. If the fixture is ever
        // regenerated against an engine where a decline moves the board, they
        // would both keep passing for the wrong reason -- comparing an
        // unchanged C# board against three recorded boards that also happen to
        // be equal. This fails instead, and names the assumption.
        var recorded = RecordedDigests();
        Assert.Equal(recorded[0], recorded[1]);
        Assert.Equal(recorded[1], recorded[2]);
        Assert.NotEqual(recorded[2], recorded[3]);
    }

    [Fact]
    public void EachStepAsksTheRecordedQuestion()
    {
        var recorded = RecordedPrompts();
        var game = Begin();

        for (int step = 0; step <= 2; step++)
        {
            var expected = recorded[step];
            var actual = game.Pending;
            Assert.NotNull(actual);

            string where = $"step {step}";
            Assert.Equal(expected.GetProperty("player").GetInt32(), actual.Player);
            Assert.Equal(expected.GetProperty("kind").GetString(), RecordedKind(actual));
            Assert.Equal(expected.GetProperty("trigger").GetString(), actual.Trigger);
            Assert.Equal(expected.GetProperty("label").GetString(), actual.Label);
            Assert.Equal(expected.GetProperty("cancellable").GetBoolean(), actual.Cancellable);
            Assert.NotEmpty(actual.Affordances);

            AssertAffordances(expected, actual, where);

            if (step < 2)
            {
                game.Resolve(Decision.Decline);
            }
        }
    }

    [Fact]
    public void TheOnlyVerbNotYetDerivedIsPlay()
    {
        // The coverage claim, stated as a set rather than as a count so that a
        // verb appearing in the recording that nothing here builds fails the
        // build instead of being silently skipped by the comparison above.
        var offered = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var prompt in MilestoneCase().GetProperty("prompts").EnumerateArray())
        {
            foreach (var affordance in prompt.GetProperty("affordances").EnumerateArray())
            {
                offered.Add(affordance.GetProperty("verb").GetString()!);
            }
        }

        Assert.Equal(["Change_Form", "End Phase", "Play", "Resolve Mulligans"], offered);
        Assert.Equal(["Play"], offered.Except(Game.DerivedVerbs));
    }

    [Fact]
    public void EveryRecordedBoardIsProducedByFolding()
    {
        // MARVEL-173's acceptance criterion, the other way round from the one
        // MARVEL-176 met: produce the recorded `step_digests` as *output*
        // rather than reproducing them as input.
        var recorded = RecordedDigests();
        var game = Begin();

        for (int step = 0; step < recorded.Count; step++)
        {
            if (step > 0)
            {
                game.Resolve(Decision.Decline);
            }

            string produced = game.State.Digest().Canonical();
            if (recorded[step] != produced)
            {
                Assert.Fail($"step {step}: {DigestDiff.Describe(recorded[step], produced)}");
            }
        }
    }

    [Fact]
    public void TheWholeTraceHashesToTheRecordedValue()
    {
        // One value covering all seven steps in order, which is what the
        // fixture carries it for: a port can fail fast before working out which
        // step diverged. It is also the check that a per-step comparison cannot
        // make -- that the steps came out in the recorded *order*.
        var game = Begin();
        var produced = new List<string> { game.State.Digest().Canonical() };
        while (game.Pending is not null)
        {
            var result = game.Resolve(Decision.Decline);
            if (result.Prompt is not null)
            {
                produced.Add(result.State.Digest().Canonical());
            }
        }

        using var vectors = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("digest", "vectors.json")));
        var board = vectors.RootElement.GetProperty("cases")[0];

        string expected = board.GetProperty("trace_sha256").GetString()!;
        byte[] hashed = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(string.Join("\n", produced)));
        Assert.Equal(expected, Convert.ToHexString(hashed).ToLowerInvariant());
    }

    [Fact]
    public void TheGameEndsAfterExactlyTheRecordedNumberOfDecisions()
    {
        // The fixture asks for twenty steps and holds seven, because the
        // recorded game *ends* -- the headless run reports `game_over`. So the
        // step count is a claim about the trace and not a budget that happened
        // to be hit, and an engine that stopped early or ran on would produce
        // every recorded digest and still be wrong.
        var game = Begin();
        int prompts = 1;
        while (game.Resolve(Decision.Decline).Prompt is not null)
        {
            prompts++;
        }

        Assert.Equal(RecordedDigests().Count, prompts);
        Assert.Equal(GamePhase.Over, game.Phase);
        Assert.True(game.State.IsOver);
    }

    [Rule("rr:main-scheme-main-scheme-deck.2")]
    [Rule("rr:main-scheme-main-scheme-deck.2.1")]
    [Fact]
    public void TheVillainWinsByCompletingTheMainScheme()
    {
        // The Rhino deck holds one stage, so completing it is completing the
        // final stage, and the villain wins outright -- which is why
        // the recording stops at seven steps of a twenty-step request.
        var game = Begin();
        while (game.Pending is not null)
        {
            game.Resolve(Decision.Decline);
        }

        var scheme = game.State.TheCardIn(DeckType.MainSchemesArea)!;
        Assert.True(scheme.Tokens["k_threat"]
                    >= Cards.PrintedValue(scheme.FaceId, "TargetThreat", 1));
        Assert.Equal(1, scheme.Tokens["is_completed"]);
    }

    [Fact]
    public void TheVillainPhaseIsTheFirstThingToMoveTheBoard()
    {
        // Steps 0-2 are one board and 3-4 another, so the villain phase is
        // where the game first changes anything -- and where four things that
        // setup cannot exercise first happen at once.
        var game = Begin();
        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);

        int before = game.State.Cards.Count;
        var result = game.Resolve(Decision.Decline);

        // A card made mid-game, which is the append-only id contract tested by
        // something other than dealing. The Tough is id 81 on a board of 81.
        Assert.Equal(before + 1, result.State.Cards.Count);
        var tough = result.State.Cards[^1];
        Assert.Equal(before, tough.ObjectId);
        Assert.Equal(Statuses.Tough, tough.FaceId);

        // The first card with a host. `host` is -1 on all 81 cards at setup, so
        // checklist step 6 of the digest spec is untested until right here.
        var villain = result.State.TheCardIn(DeckType.VillainArea)!;
        Assert.Equal(villain.ObjectId, tough.Area.Host);
        Assert.True(Statuses.Has(result.State, villain, Statuses.Tough));

        // And a zone nothing reached before. `EncounterDiscardPile` is the case
        // that shaped the face-down predicate: a deck that is nonetheless face
        // up.
        var discard = result.State.AreaOf(DeckType.EncounterDiscardPile);
        Assert.Equal(2, discard.Cards.Count);
        Assert.All(discard.Cards, card => Assert.True(card.FaceUp));
    }

    [Rule("rr:scheme-enemy-activation.step.2.d")]
    [Fact]
    public void TheBoostCardIsDiscardedBeforeTheEncounterCard()
    {
        // The recorded discard pile holds the boost card at index 0 and the
        // revealed encounter card at index 1, which is the whole order of the
        // phase in one observable: the villain activates before cards are
        // dealt. Drawing them the other way round shifts every card left in the
        // encounter deck and every board after this one.
        var game = Begin();
        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);
        var result = game.Resolve(Decision.Decline);

        var discard = result.State.AreaOf(DeckType.EncounterDiscardPile);
        Assert.Equal(["01186", "01105"], discard.Cards.Select(card => card.FaceId));
    }

    [Rule("rr:villain-phase.step.1")]
    [Rule("rr:scheme-enemy-activation.step.3")]
    [Fact]
    public void ThreatComesFromTwoPlacesAndBothAreCounted()
    {
        // `k_threat` 0 -> 2 is not one rule. One is the main scheme's own
        // escalation (`rr:villain-phase.step.1`, `1*` at one player) and one
        // is Rhino scheming (`rr:scheme-enemy-activation.step.3`, SCH 1 plus a
        // boost card worth nothing). Either alone gives 1, which is why the
        // total is checked against the parts.
        var game = Begin();
        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);
        var result = game.Resolve(Decision.Decline);

        var scheme = result.State.TheCardIn(DeckType.MainSchemesArea)!;
        Assert.Equal(2, scheme.Tokens["k_threat"]);

        var placements = result.Events.OfType<FieldSet>()
            .Where(e => e.Field == "k_threat").ToList();
        Assert.Equal(2, placements.Count);
        Assert.Equal([(0L, 1L), (1L, 2L)],
                     placements.Select(e => (e.From!.Value, e.To!.Value)));
    }

    [Rule("rr:surge")]
    [Fact]
    public void ImToughSurgesWhenTheVillainIsAlreadyTough()
    {
        // The other branch of the card. The recorded game never takes it --
        // `01105` is revealed once, and Rhino is not Tough yet -- so without
        // this the branch is unexecuted code that reads as if it works.
        //
        // `rr:surge.1`: "**When Revealed**: deal yourself 1 facedown encounter
        // card." Dealt, not revealed: `.2` finishes the original card first,
        // and the villain phase's reveal queue is what makes that happen.
        var game = Begin();
        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);

        var world = game.State;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Assert.True(Statuses.Has(world, villain, Statuses.Tough));

        var queue = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0));
        int before = queue.Cards.Count;

        AuthoredCards.Runner().WhenRevealed(
            world, world.Cards.First(card => card.FaceId == AuthoredCards.ImTough), 0);

        // One card dealt, and Rhino is no more Tough than he already was --
        // `rr:status-cards.1` caps it at one and the card's own text says the
        // same thing by surging instead.
        Assert.Equal(before + 1, queue.Cards.Count);
        Assert.Equal(1, Statuses.Count(world, villain, Statuses.Tough));
    }

    [Fact]
    public void ResolvingPastTheEndIsRefusedRatherThanIgnored()
    {
        var game = Begin();
        while (game.Pending is not null)
        {
            game.Resolve(Decision.Decline);
        }

        Assert.Throws<InvalidOperationException>(() => game.Resolve(Decision.Decline));
    }

    [Rule("rr:villain-phase.step.5")]
    [Fact]
    public void TheFirstPlayerTokenIsPassedEvenWithOnePlayer()
    {
        // At one player the token comes back to the same seat, so the modulo
        // is doing the work and an implementation that only
        // incremented would put the token on a seat that does not exist -- and
        // `k_first_player_token` would vanish from the digest.
        var game = Begin();
        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);
        var result = game.Resolve(Decision.Decline);

        Assert.Equal(0, result.State.FirstPlayer);
        Assert.Contains("\"k_first_player_token\":1", result.State.Digest().Canonical());
    }

    [Fact]
    public void TakingAnAffordanceNobodyOfferedNamesWhatWasTaken()
    {
        // The message contract, which is the point: an answer the engine
        // cannot resolve says *what was taken* rather than "not implemented".
        // The difference is a one-line diagnosis and a debugging session.
        //
        // An affordance nobody offered is the reachable case now that every
        // verb the prompts carry is implemented. It used to be the mulligan,
        // which was offered and could not be taken -- MARVEL-229.
        var game = Begin();

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => game.Resolve(Decision.Take(999)));
        Assert.Contains("999", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:appendix-ii-setup.step.15")]
    [Fact]
    public void AMulliganDiscardsWhatWasNamedAndDrawsBackUpToHandSize()
    {
        // "Each player may discard any number of cards from hand, and then
        // draw **up to their starting hand size**." Not draw that many: a
        // player who discarded three draws back to their hand size.
        var game = Begin(out var world);
        var hand = world.Seats[0].Hand;
        int size = hand.Cards.Count;
        var thrownAway = hand.Cards.Take(3).Select(card => card.ObjectId).ToList();

        game.Resolve(Decision.Take(game.Pending!.Affordances[0].Id, thrownAway, []));

        Assert.Equal(size, hand.Cards.Count);
        Assert.DoesNotContain(hand.Cards, card => thrownAway.Contains(card.ObjectId));
    }

    [Rule("rr:appendix-ii-setup.step.15")]
    [Fact]
    public void MulliganedCardsGoToTheDiscardPileAndNotBackIntoTheDeck()
    {
        // "*(Do not shuffle these discarded cards back into their decks at
        // this time.)*" The parenthesis is the whole of the difference between
        // this and a deck-bottom mulligan, and it is observable: the cards are
        // in the discard pile, where `rr:player-deck.4` can shuffle them into a
        // new deck later and where a card that reads a discard pile can see
        // them.
        var game = Begin(out var world);
        var thrownAway = world.Seats[0].Hand.Cards.Take(2)
            .Select(card => card.ObjectId).ToList();

        game.Resolve(Decision.Take(game.Pending!.Affordances[0].Id, thrownAway, []));

        foreach (int id in thrownAway)
        {
            Assert.Equal(DeckType.DiscardPile, world.Cards[id].Area.Type);
        }
    }

    [Rule("rr:appendix-ii-setup.step.15")]
    [Fact]
    public void DecliningAMulliganKeepsTheHandExactlyAsItWas()
    {
        // "**May** discard." Declining and taking it with an empty list are
        // the same answer, which is why the prompt is not cancellable and why
        // this is the same code path.
        var game = Begin(out var world);
        var before = world.Seats[0].Hand.Cards.Select(card => card.ObjectId).ToList();

        game.Resolve(Decision.Decline);

        Assert.Equal(before, world.Seats[0].Hand.Cards.Select(card => card.ObjectId));
    }

    [Fact]
    public void DistinctOptionsGetDistinctHandles()
    {
        // The half of the handle contract this slice can reach. `Game` caches
        // handles on `(verb, anchor)` so that a re-offered option keeps its id
        // -- the property the recording has, where `End Phase` is id 1 at steps
        // 2, 4 and 6 rather than three different ids. **That half is not tested
        // here and cannot be**: no option is offered twice before the villain
        // phase, so nothing in this game re-offers anything. It becomes
        // testable with the second recorded transition.
        //
        // What is testable is the other half, and it is the half a cache gets
        // wrong: two different options must not collide onto one handle.
        var game = Begin();
        int mulligan = game.Pending!.Affordances.Single().Id;
        game.Resolve(Decision.Decline);
        int changeForm = game.Pending!.Affordances.Single(a => a.Verb == Game.ChangeForm).Id;
        game.Resolve(Decision.Decline);
        int endPhase = game.Pending!.Affordances.Single().Id;

        Assert.Equal(3, new HashSet<int> { mulligan, changeForm, endPhase }.Count);
    }

    [Fact]
    public void HandlesDoNotDependOnAnythingOutsideTheGame()
    {
        // Two identically dealt games hand out identical handles. A counter
        // that leaked across games -- a static, a clock, a hash of an address
        // -- would pass every other test here and make a replay unrepeatable.
        var first = Begin();
        var second = Begin();

        Assert.Equal(first.Pending!.Affordances.Single().Id,
                     second.Pending!.Affordances.Single().Id);
    }

    private static void AssertAffordances(JsonElement expected, Prompt actual, string where)
    {
        var recorded = expected.GetProperty("affordances").EnumerateArray()
            .Where(affordance => Game.DerivedVerbs.Contains(
                affordance.GetProperty("verb").GetString()!)
                || affordance.GetProperty("verb").GetString() == CardPlay.Verb)
            .ToList();

        // **Including `Play`, which this used to filter out of the recording.**
        // The engine offers every one of these the recording offers, in the
        // same order, and nothing it does not -- an extra affordance is as
        // wrong as a missing one, and only this direction catches it.
        //
        // Getting the `Play` list right is a sharper check than it looks. The
        // opening hand holds six cards and the recording offers four: `01088`
        // Energy is a resource card and cannot be played at all
        // (`rr:player-turn.2` does not list one), and `01003` Backflip is an
        // event whose ability is an Interrupt rather than an Action, so
        // `rr:player-turn.5.d` does not reach it either.
        Assert.Equal(recorded.Count, actual.Affordances.Count);

        for (int index = 0; index < recorded.Count; index++)
        {
            var want = recorded[index];
            var got = actual.Affordances[index];
            string what = $"{where}, affordance {index} ({got.Verb})";

            Assert.Equal(want.GetProperty("verb").GetString(), got.Verb);
            Assert.Equal(want.GetProperty("anchor_id").GetInt32(), got.AnchorId);
            Assert.Equal(want.GetProperty("anchor_player").GetInt32(), got.AnchorPlayer);
            Assert.Null(got.Illegal);

            // The cost as printed, **and its generators**, against what the
            // recording carries.
            //
            // The generators used to be skipped. The recording lists six for a
            // hand of six cards, one of which is being played -- so one
            // generator is not a card in hand at all, and a list built from
            // hand cards alone was short by exactly that one. It is
            // `rr:resource-ability`: Peter Parker's "Scientist -- **Resource**:
            // generate a [mental] resource", printed on the alter-ego face the
            // recorded board is showing. Now that it is written, this is the
            // sharpest check in the file: the order and the letters, on the
            // wire.
            var costs = want.GetProperty("costs");
            Assert.Equal(costs.GetArrayLength(), got.CostOptions.Count);
            for (int price = 0; price < got.CostOptions.Count; price++)
            {
                Assert.Equal(
                    costs[price].GetProperty("cost").GetString(),
                    got.CostOptions[price].Cost);

                // **The generators, by what they make rather than by id.**
                //
                // The recording's `effect` is the Python engine's own effect
                // id, not an object id: the four plays above list sources
                // `{3, 33, 38, 41, 42, 43}` for a hand whose object ids are
                // `{42, 45, 37, 9, 47, 46}`, and each play omits exactly the
                // one belonging to the card being played. So the ids cannot be
                // compared and the *letters* can. MARVEL-223.
                //
                // The count is the sharp half. It was five before
                // `rr:resource-ability` was written and the recording says six:
                // Peter Parker's "Scientist — **Resource**: generate a [mental]
                // resource" is a generator that is not a card in hand at all.
                var recordedLetters = costs[price].GetProperty("sources").EnumerateArray()
                    .Select(source => source.GetProperty("generates").GetString())
                    .Order(StringComparer.Ordinal);
                var madeLetters = (got.CostOptions[price].Sources ?? [])
                    .Select(source => source.Generates)
                    .Order(StringComparer.Ordinal);

                Assert.Equal(recordedLetters, madeLetters);
            }

            var targets = want.GetProperty("targets");
            if (targets.ValueKind == JsonValueKind.Null)
            {
                Assert.Null(got.Targets);
                continue;
            }

            Assert.NotNull(got.Targets);
            Assert.Equal(
                targets.GetProperty("legal").EnumerateArray().Select(id => id.GetInt32()),
                got.Targets.Legal);
            Assert.Equal(targets.GetProperty("min").GetInt32(), got.Targets.Min);
            Assert.Equal(targets.GetProperty("max").GetInt32(), got.Targets.Max);
            Assert.Equal(targets.GetProperty("is_search").GetBoolean(), got.Targets.IsSearch);
            Assert.False(got.Targets.IsGrouped, what);
        }
    }

    private static IReadOnlyList<string> RecordedDigests()
    {
        using var vectors = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("digest", "vectors.json")));
        var board = vectors.RootElement.GetProperty("cases")[0];
        Assert.Equal(Campaign, board.GetProperty("campaign").GetString());
        Assert.Equal((int)Seed, board.GetProperty("seed").GetInt32());
        return [.. board.GetProperty("step_digests").EnumerateArray()
            .Select(digest => digest.GetString()!)];
    }

    private static IReadOnlyList<JsonElement> RecordedPrompts() =>
        [.. MilestoneCase().GetProperty("prompts").EnumerateArray()];

    // The document is not disposed on purpose: `JsonElement` is a view into it,
    // and every caller here reads elements after the call returns. The file is
    // read per call rather than cached so a test cannot see another's leftovers.
    private static JsonElement MilestoneCase()
    {
        var prompts = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("digest", "prompts.json")));
        var board = prompts.RootElement.GetProperty("cases")[0];
        Assert.Equal(Campaign, board.GetProperty("campaign").GetString());
        Assert.Equal((int)Seed, board.GetProperty("seed").GetInt32());
        return board;
    }

    /// <summary>
    /// How the recording spells this prompt's kind.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A translation, and it lives here rather than on <c>Prompt</c> on purpose.
    /// The recorded <c>kind</c> is the name of a member of the Python engine's
    /// <c>TimingPriority</c>, an enum with twelve members of which four —
    /// <c>Rule</c>, <c>Statistics</c>, <c>Normal</c>, <c>End</c> — name nothing
    /// in the Rules Reference. It also flattens two questions the rules keep
    /// apart: <i>what</i> is being asked and <i>when</i>.
    /// </para>
    /// <para>
    /// So the engine carries <see cref="Question"/> and
    /// <see cref="TimingPriority"/>, both read off the rulebook, and this is
    /// where they are bent into the corpus's spelling. Every other engine that
    /// wants the recording to line up needs this function; none of them should
    /// have to think in it.
    /// </para>
    /// </remarks>
    private static string RecordedKind(Prompt prompt) => prompt switch
    {
        // "Normal" is the recording's word for a question that is not timed
        // around an occurrence at all -- a turn option, a target, a payment.
        { When: TimingPriority.Untimed } => "Normal",
        { When: TimingPriority.StatusForcedInterrupt or TimingPriority.ForcedInterrupt }
            => "ForcedInterrupt",
        { When: TimingPriority.Interrupt } => "Interrupt",
        { When: TimingPriority.ForcedResponse } => "ForcedResponse",
        { When: TimingPriority.Response } => "Response",
        _ => prompt.When.ToString(),
    };
}
