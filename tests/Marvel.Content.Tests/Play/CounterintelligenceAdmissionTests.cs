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

public sealed class CounterintelligenceAdmissionTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [Rule("rr:target.3")]
    [Rule("rr:target.3.3")]
    [Rule("rr:prevent.2")]
    [Rule("rr:interrupt.2")]
    public void AnotherControllerPreventsThreatBeforeTheSecondCopyIsReconsidered(int assigned)
    {
        // "A target is valid ... if any part ... can affect that target."
        // The discard cost "is not considered". Prevention reduces the amount
        // "being assigned before it is placed"; multiple interrupts share it.
        Assert.Equal(ResolveTwoCopies(assigned, false), ResolveTwoCopies(assigned, true));
    }

    private static string ResolveTwoCopies(int assigned, bool probe)
    {
        var f = new BlackWidowPreventionFixture();
        var first = f.World.CreateCard("08017", f.World.AreaOf(
            DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var second = f.Upgrade("08017");
        var main = f.World.TheCardIn(DeckType.MainSchemesArea)!;
        Threat.Schedule(f.World, new([main], f.Villain, assigned,
            ThreatCause.CardAbility, "two controllers", 0));
        var prompt = f.Find(first);
        Assert.Equal(0, prompt.Player);
        var occurrence = f.World.Agenda.Occurrence!;
        var placement = occurrence.Threat!;
        if (probe) ProbeAndReject(f, prompt, occurrence);
        f.Use(prompt, first);
        Assert.Equal(Math.Max(0, assigned - 3), placement.Remaining);
        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        if (probe) ProbeAndReject(f, prompt, occurrence);
        Assert.Equal(assigned > 3, f.Runner.Waiting(f.World, occurrence,
            WindowKind.Interrupt).Any(offer => offer.Card == second.ObjectId));

        if (assigned > 3)
        {
            prompt = f.Find(second);
            Assert.Equal(1, prompt.Player);
            Assert.Same(placement, f.World.Agenda.Occurrence!.Threat);
            if (probe) ProbeAndReject(f, prompt, occurrence);
            f.Use(prompt, second);
        }
        f.Finish(second);
        Assert.Equal(0, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(assigned > 3 ? DeckType.DiscardPile : DeckType.UpgradesArea, second.Area.Type);
        Assert.False(f.World.Agenda.IsBusy);
        Assert.False(f.World.IsOver);

        // A fully prevented assignment does not consume the second copy's
        // future opportunity or carry the first copy's spare prevention forward.
        Threat.Schedule(f.World, new([main], f.Villain, 2,
            ThreatCause.CardAbility, "next exact instance", 0));
        if (assigned <= 3)
        {
            prompt = f.Find(second);
            Assert.NotSame(placement, f.World.Agenda.Occurrence!.Threat);
            Assert.Equal(2, f.World.Agenda.Occurrence.Threat!.Remaining);
            f.Use(prompt, second);
        }
        f.Finish();
        Assert.Equal(assigned > 3 ? 2 : 0, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Assert.False(f.World.Agenda.IsBusy);
        return f.World.Digest().Canonical() + "|" + f.World.Random.Generator.WordsConsumed
            + "|" + string.Join(";", f.Events.Select(item => item.ToString()));
    }

    private static void ProbeAndReject(BlackWidowPreventionFixture f, Prompt prompt, Occurrence occurrence)
    {
        var digest = f.World.Digest().Canonical();
        long random = f.World.Random.Generator.WordsConsumed;
        var pending = f.World.Agenda.Outstanding.ToArray();
        long remaining = occurrence.Threat!.Remaining;
        var offers = f.Runner.Waiting(f.World, occurrence, WindowKind.Interrupt).ToArray();
        Assert.Equal(offers, f.Runner.Waiting(f.World, occurrence, WindowKind.Interrupt));
        Assert.ThrowsAny<Exception>(() => f.Answer(prompt, Decision.Take(int.MaxValue)));
        Assert.Equal(digest, f.World.Digest().Canonical());
        Assert.Equal(random, f.World.Random.Generator.WordsConsumed);
        Assert.Equal(pending, f.World.Agenda.Outstanding);
        Assert.Equal(remaining, occurrence.Threat.Remaining);
    }

    [Theory]
    [InlineData(0, false, false, false)]
    [InlineData(1, false, true, false)]
    [InlineData(0, true, true, false)]
    [InlineData(0, true, true, true)]
    [Rule("rr:target.3")]
    [Rule("rr:target.3.3")]
    [Rule("rr:target.3.4")]
    public void PreventionRequiresAnAffectableTargetButAnotherEffectMayQualify(
        int amount, bool additionalEffect, bool offered, bool reverseOrder)
    {
        // Synthetic contract: "at least one of those effects can affect the
        // target". Discarding the source as a cost cannot qualify by itself.
        string prevention = "{\"preventThreat\":" + amount + "}";
        string effect = additionalEffect
            ? "{\"and\":[" + prevention
                + ",{\"removeThreat\":{\"scheme\":\"trigger.target\",\"amount\":1}}]}"
            : prevention;
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01008","abilities":[{
              "trigger":{"event":"WhenThreatWouldBePlaced","timing":"Interrupt","subject":"game"},
              "cost":{"discard":"this"},"effect":EFFECT}]}]}
            """.Replace("EFFECT", effect, StringComparison.Ordinal)));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var source = world.CreateCard("01008", world.AreaOf(
            DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 1);
        Threat.Schedule(world, new([main], null, 1, ThreatCause.CardAbility, "synthetic", 0));
        var events = new List<Marvel.Rules.Events.GameEvent>();
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        if (offered)
        {
            Assert.NotNull(prompt);
            Sequence.Answer(world, world.Facts, runner, prompt,
                Decision.Take(Assert.Single(prompt.Affordances).Id), events);
            if (additionalEffect)
            {
                var order = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
                Assert.Equal(Question.Order, order.Asking);
                Sequence.Answer(world, world.Facts, runner, order,
                    Decision.Take(Assert.Single(order.Affordances).Id,
                        reverseOrder ? [1, 0] : [0, 1], []), events);
            }
            Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        }
        else Assert.Null(prompt);
        Assert.Equal(offered ? DeckType.DiscardPile : DeckType.UpgradesArea, source.Area.Type);
        Assert.Equal(offered ? 1 : 2, main.Tokens.GetValueOrDefault("k_threat"));
    }
}
