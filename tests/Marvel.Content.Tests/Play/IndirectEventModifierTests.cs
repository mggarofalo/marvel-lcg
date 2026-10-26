using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class IndirectEventModifierTests
{
    // Synthetic actions isolate allocation and query behavior. The authored
    // upgrade's nested initiation is covered by ModifierIndirectReviewTests.
    private static (World World, AbilityRunner Runner, Card Source) Begin(string control)
    {
        string face = control == "support" ? "01006" : "05003";
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
        {"cards":[{"card":"FACE","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "effect":{"seq":[{"indirectDamage":{"among":{"query":"heroesAndAllies"},"amount":3}},
            {"indirectDamage":{"among":{"query":"heroesAndAllies"},"amount":3}}]}
        }]}]}
        """.Replace("FACE", face, StringComparison.Ordinal)));
        var (_, world) = Playing(_ => { }, hero: true,
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var area = control == "support"
            ? world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0) : world.Seats[0].Hand;
        var source = world.CreateCard(face, area);
        var affected = control == "differentEvent" ? world.CreateCard("05003", world.Seats[0].Hand) : source;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "eventDamage",
            Amount: 2, Card: source.ObjectId, Affects: affected.ObjectId));
        world.CreateCard("01088", world.Seats[0].Hand);
        return (world, runner, source);
    }

    [Theory]
    [InlineData("matching", 5)]
    [InlineData("differentEvent", 3)]
    [InlineData("support", 3)]
    [Rule("rr:event.5")]
    public void EveryIndirectInstanceUsesOnlyItsOwnEventBonus(string control, int expected)
    {
        // "each of those instances is modified."
        var (world, runner, source) = Begin(control);
        Resolve(world, runner, source, expected);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(expected * 2, world.Seats[1].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void RepeatedOffersAndDescriptionsPreserveStateAndSubsequentAssignments()
    {
        var (world, runner, source) = Begin("matching");
        var (control, controlRunner, controlSource) = Begin("matching");
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        var effects = world.Effects.Registered.ToArray();
        for (int index = 0; index < 2; index++)
        {
            var action = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
            runner.Describe(world, action);
        }
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(effects, world.Effects.Registered);
        Resolve(world, runner, source, 5);
        Resolve(control, controlRunner, controlSource, 5);
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(control.Agenda.IsBusy, world.Agenda.IsBusy);
    }

    private static void Resolve(World world, AbilityRunner runner, Card source, int expected)
    {
        var action = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        var spent = source.Area == world.Seats[0].Hand
            ? new[] { world.Seats[0].Hand.Cards.Last(card => card.FaceId == "01088").ObjectId } : [];
        runner.ExecuteAction(world, new PlayerAction(action, spent, []));
        for (int index = 0; index < 2; index++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, [])!;
            var offer = Assert.Single(prompt.Affordances);
            Assert.Equal(expected, offer.Targets!.Min);
            Assert.Equal(expected, offer.Targets.Max);
            Assert.Equal($"Assign {expected} indirect damage", prompt.DisplayQuestion);
            Assert.Contains($"Assign {expected} damage", prompt.Description);

            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(offer.Id,
                Enumerable.Repeat(world.Seats[1].IdentityCard.ObjectId, expected).ToArray(), []), []);
        }
        Sequence.Finish(world, world.Facts, runner, []);
    }
}
