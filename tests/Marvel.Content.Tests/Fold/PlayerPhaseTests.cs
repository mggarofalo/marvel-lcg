using System.Text.Json;
using Marvel.Content.Setup;
using Marvel.Rules.Fold;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Fold;

/// <summary>
/// The fold, against the first three recorded steps of the milestone game.
/// </summary>
/// <remarks>
/// <para>
/// <c>OpeningBoardTests</c> proves the board <c>rhino / spider_man / 12345</c>
/// is dealt from. This proves the loop runs on it: three recorded steps produced
/// as the output of folding two answers, rather than one board produced by
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

    private static Game Begin() => Game.Begin(WorldSetup.Deal(
        Cards,
        Blueprints.From(Dealer.DealOrder(Setup, Campaign, Heroes)),
        [.. Heroes.Select(hero => Setup.Hero(hero).Name)],
        Seed));

    [Fact]
    public void TheFirstThreeRecordedBoardsAreProducedByFolding()
    {
        var recorded = RecordedDigests();
        var game = Begin();

        // Step 0: dealt, nothing folded yet.
        Assert.Equal(recorded[0], game.State.Digest().Canonical());

        // Steps 1 and 2: two declines.
        for (int step = 1; step <= 2; step++)
        {
            var result = game.Fold(Decision.Decline);
            Assert.Same(game.State, result.State);
            Assert.Equal(recorded[step], result.State.Digest().Canonical());
        }
    }

    [Fact]
    public void DecliningChangesNothingAndSaysSo()
    {
        // The event list is the fold's account of what changed. A decline that
        // emitted events would be claiming a change the digest denies, and a
        // digest that moved under an empty event list would be the same lie the
        // other way round. Both are checked together because either alone
        // passes while the pair disagrees.
        var game = Begin();
        string before = game.State.Digest().Canonical();

        for (int step = 1; step <= 2; step++)
        {
            var result = game.Fold(Decision.Decline);
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
            Assert.Equal(expected.GetProperty("kind").GetString(), actual.Kind.ToString());
            Assert.Equal(expected.GetProperty("trigger").GetString(), actual.Trigger);
            Assert.Equal(expected.GetProperty("label").GetString(), actual.Label);
            Assert.Equal(expected.GetProperty("cancellable").GetBoolean(), actual.Cancellable);
            Assert.NotEmpty(actual.Affordances);

            AssertAffordances(expected, actual, where);

            if (step < 2)
            {
                game.Fold(Decision.Decline);
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
    public void TheVillainPhaseIsTheBoundaryAndItSaysWhichRulesAreMissing()
    {
        var game = Begin();
        game.Fold(Decision.Decline);
        game.Fold(Decision.Decline);

        string before = game.State.Digest().Canonical();
        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => game.Fold(Decision.Decline));

        Assert.Contains("villain phase", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("hand refill", thrown.Message, StringComparison.Ordinal);

        // Thrown before anything was applied, so the caller still holds the
        // board they had. A boundary that half-finishes is worse than one that
        // stops.
        Assert.Equal(before, game.State.Digest().Canonical());
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }

    [Fact]
    public void TakingAnAffordanceNamesTheVerbItCannotResolve()
    {
        var game = Begin();
        var mulligan = game.Pending!.Affordances[0];

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => game.Fold(Decision.Take(mulligan.Id)));
        Assert.Contains(Game.ResolveMulligans, thrown.Message, StringComparison.Ordinal);
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
        game.Fold(Decision.Decline);
        int changeForm = game.Pending!.Affordances.Single(a => a.Verb == Game.ChangeForm).Id;
        game.Fold(Decision.Decline);
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
                affordance.GetProperty("verb").GetString()!))
            .ToList();

        // The fold offers every derived verb the recording offers, and nothing
        // it does not. `Play` is filtered out of the recording rather than
        // tolerated in the fold: an extra affordance is as wrong as a missing
        // one, and only this direction catches it.
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
            Assert.Empty(got.CostOptions);

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
}
