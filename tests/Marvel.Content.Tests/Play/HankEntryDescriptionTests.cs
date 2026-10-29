using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HankEntryDescriptionTests
{
    [Theory]
    [InlineData("12007", "", "", 1)]
    [InlineData("01088", "", "", 2)]
    [InlineData("01088", "01089", "01090", 4)]
    [Rule("rr:cost.4.1")]
    public void OfferDescribesCompletedPaymentWithoutChangingState(string first, string second, string third, int count)
    {
        // Excess resources "were not paid for that cost"; Hank's printed cap is four.
        var runner = AuthoredCards.Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var hank = world.CreateCard("12011", seat.Hand);
        int[] payment = new[] { first, second, third }.Where(face => face.Length > 0)
            .Select(face => world.CreateCard(face, seat.Hand).ObjectId).ToArray();
        CardPlay.Play(world, world.Facts, runner, seat, hank, payment, []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var offered = Assert.Single(prompt.Affordances, offer => offer.AnchorId == hank.ObjectId);
        Assert.True(prompt.Cancellable);
        Assert.Equal("Place pym counters", offered.DisplayLabel);
        Assert.Contains($"Place {count} pym counter", offered.Description);
        Assert.Contains("maximum 4", offered.Description);
        Assert.Contains("Declining this interrupt places no counters", offered.Description);
        Assert.Contains("each pym counter gives Ant-Man +1 hit point", offered.Description);
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var agenda = world.Agenda.Outstanding.ToArray();
        var again = runner.Describe(world, new PendingAbility(hank.ObjectId, AbilityType.Interrupt, 0));
        Assert.Equal(offered.Description, again.Description);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(agenda, world.Agenda.Outstanding.ToArray());
    }
    [Fact]
    public void DescribingDoesNotAlterTheContinuedEntry()
    {
        static string Run(bool probe)
        {
            var runner = AuthoredCards.Runner();
            var world = ThorActualFixture.Board("06001a");
            world.Abilities = runner;
            var seat = world.Seats[0];
            var hank = world.CreateCard("12011", seat.Hand);
            var payment = world.CreateCard("12007", seat.Hand);
            CardPlay.Play(world, world.Facts, runner, seat, hank, [payment.ObjectId], []);
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
            var offer = Assert.Single(prompt.Affordances, item => item.AnchorId == hank.ObjectId);
            if (probe)
            {
                string before = world.Digest().Canonical();
                var random = world.Random.Generator.GetState();
                long words = world.Random.Generator.WordsConsumed;
                var pending = world.Agenda.Outstanding.ToArray();
                var described = runner.Describe(world, new PendingAbility(hank.ObjectId, AbilityType.Interrupt, 0));
                Assert.Equal(offer.Description, described.Description);
                Assert.Equal(before, world.Digest().Canonical());
                Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
                Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
                Assert.Equal(words, world.Random.Generator.WordsConsumed);
                Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
            }
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(offer.Id), []);
            Sequence.Finish(world, world.Facts, runner, []);
            Assert.Equal(1, hank.Tokens.GetValueOrDefault("c_pym"));
            Assert.Empty(world.Agenda.Outstanding);
            return world.Digest().Canonical();
        }
        Assert.Equal(Run(false), Run(true));
    }

    [Fact]
    public void ConditionalHealthGrantIsNotAdvertisedAsCertain()
    {
        var root = JsonNode.Parse(AuthoredCards.Text)!;
        var row = root["cards"]!.AsArray().Single(item => (string?)item!["card"] == "12011")!;
        row["abilities"]![0]!["when"] = JsonNode.Parse("""{"atLeast":{"value":0,"count":1}}""");
        var runner = new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var hank = world.CreateCard("12011", seat.Hand);
        var payment = world.CreateCard("12007", seat.Hand);
        CardPlay.Play(world, world.Facts, runner, seat, hank, [payment.ObjectId], []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var offer = Assert.Single(prompt.Affordances, item => item.AnchorId == hank.ObjectId);
        Assert.Contains("Place 1 pym counter", offer.Description);
        Assert.DoesNotContain("hit point", offer.Description);
    }

    [Fact]
    public void PrepaymentDescriptionIsOptionalAndReadOnlyForTheActualRow()
    {
        var runner = AuthoredCards.Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var hank = world.CreateCard("12011", world.Seats[0].Hand);
        var ordinary = world.CreateCard("01067", world.Seats[0].Hand);
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        string? description = runner.DescribeCardPlay(world, hank);
        Assert.Contains("Optional entry interrupt", description);
        Assert.Contains("per resource overpaid (maximum 4)", description);
        Assert.Contains("each pym counter gives Ant-Man +1 hit point", description);
        Assert.Null(runner.DescribeCardPlay(world, ordinary));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
    }

    [Fact]
    public void ConditionalEntryIsNotPromisedBeforePayment()
    {
        var root = JsonNode.Parse(AuthoredCards.Text)!;
        var row = root["cards"]!.AsArray().Single(item => (string?)item!["card"] == "12011")!;
        row["abilities"]![1]!["when"] = JsonNode.Parse("""{"atLeast":{"value":0,"count":1}}""");
        var runner = new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var hank = world.CreateCard("12011", world.Seats[0].Hand);
        Assert.Null(runner.DescribeCardPlay(world, hank));
    }

    [Fact]
    public void DifferentCounterInstructionIsNotAdvertisedAsHankEntry()
    {
        // Synthetic row: a fixed placement does not depend on excess payment.
        var root = JsonNode.Parse(AuthoredCards.Text)!;
        var row = root["cards"]!.AsArray().Single(item => (string?)item!["card"] == "12011")!;
        row["abilities"]![1]!["effect"]!["placeCounters"]!["count"] = 2;
        var runner = new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var hank = world.CreateCard("12011", world.Seats[0].Hand);
        Assert.Null(runner.DescribeCardPlay(world, hank));
    }

    [Fact]
    public void BlankedTextIsNotAdvertisedInSyntheticQuery()
    {
        var runner = AuthoredCards.Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var hank = world.CreateCard("12011", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Assert.NotNull(runner.DescribeCardPlay(world, hank));
        // Synthetic in-play query exercises the description's printed-text boundary.
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, PrintedText.Blanked,
            Card: hank.ObjectId, Affects: hank.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfPhase))
            { TargetIncarnation = hank.Incarnation });
        Assert.True(PrintedText.IsBlanked(world, hank));
        Assert.Null(runner.DescribeCardPlay(world, hank));
    }

    [Fact]
    public void PrepaymentQueryDoesNotChangeTheSubsequentEntry()
    {
        static string Run(bool probe)
        {
            var runner = AuthoredCards.Runner();
            var world = ThorActualFixture.Board("06001a");
            world.Abilities = runner;
            var seat = world.Seats[0];
            var hank = world.CreateCard("12011", seat.Hand);
            var payment = world.CreateCard("12007", seat.Hand);
            if (probe)
            {
                string before = world.Digest().Canonical();
                var random = world.Random.Generator.GetState();
                long words = world.Random.Generator.WordsConsumed;
                var pending = world.Agenda.Outstanding.ToArray();
                Assert.NotNull(runner.DescribeCardPlay(world, hank));
                Assert.Equal(before, world.Digest().Canonical());
                Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
                Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
                Assert.Equal(words, world.Random.Generator.WordsConsumed);
                Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
            }
            CardPlay.Play(world, world.Facts, runner, seat, hank, [payment.ObjectId], []);
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
            var offer = Assert.Single(prompt.Affordances, item => item.AnchorId == hank.ObjectId);
            Assert.Contains("Place 1 pym counter", offer.Description);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(offer.Id), []);
            Sequence.Finish(world, world.Facts, runner, []);
            Assert.Equal(1, hank.Tokens.GetValueOrDefault("c_pym"));
            Assert.Empty(world.Agenda.Outstanding);
            return world.Digest().Canonical();
        }
        Assert.Equal(Run(false), Run(true));
    }

}
