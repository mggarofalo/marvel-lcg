using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Prompts;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic request-boundary probes, independent of printed card abilities.</summary>
public sealed class BoostDamageRequestAdmissionTests
{
    [Theory]
    [InlineData(-1, 1)]
    [InlineData(42, 0)]
    [InlineData(42, 1)]
    public void InvalidAccountingRefusesBeforeAnyMutation(int requestedId, long amount)
    {
        // Engine choice: raw accounting ids require a live receipt, even for
        // prevented/zero damage. A finished receipt never accepts a new request.
        var (_, world) = Playing(_ => { }, hero: true);
        var target = world.Seats[0].IdentityCard;
        var active = new EnemyActivation(target.ObjectId, 0, true, Id: 43);
        var finished = new EnemyActivation(target.ObjectId, 0, true, Id: 42);
        world.Activation = active;
        world.FinishedActivation = finished;
        Statuses.Give(world, target, Statuses.Tough);
        var digest = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        var current = world.Agenda.Current;
        var occurrence = world.Agenda.Occurrence;
        var busy = world.Agenda.IsBusy;
        var events = new List<GameEvent>();
        var request = new DamageRequest(target, target, amount, "synthetic", "Damage")
        { ActivationId = requestedId };
        var error = Assert.Throws<InvalidOperationException>(() =>
            DamagePlacement.Resolve(world, world.Facts, request, events));
        Assert.Contains("no live activation receipt", error.Message);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(current, world.Agenda.Current);
        Assert.Same(occurrence, world.Agenda.Occurrence);
        Assert.Equal(busy, world.Agenda.IsBusy);
        Assert.Same(active, world.Activation);
        Assert.Same(finished, world.FinishedActivation);
        Assert.Empty(events);
        // Compare the next admitted root occurrence with an unprobed control;
        // rejected requests must not consume execution/scheduling identities.
        var (_, control) = Playing(_ => { }, hero: true);
        var controlTarget = control.Seats[0].IdentityCard;
        DamagePlacement.Resolve(control, control.Facts,
            new(controlTarget, controlTarget, 0, "synthetic", "Damage"), []);
        DamagePlacement.Resolve(world, world.Facts,
            new(target, target, 0, "synthetic", "Damage"), []);
        Assert.Equal(control.Agenda.Outstanding.Select(step => step.OccurrenceId),
            world.Agenda.Outstanding.Select(step => step.OccurrenceId));
    }
    [Fact]
    public void SuspendedRequestRevalidatesItsReceiptBeforePlacement()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01092","abilities":[{
              "trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","target":"you"},
              "effect":{"choose":{"options":[{"preventDamage":"trigger.target"},
                {"draw":{"player":"you","count":1}}]}}
            }]}]}
            """));
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        InPlay(world, "01092");
        var target = world.Seats[0].IdentityCard;
        var active = new EnemyActivation(target.ObjectId, 0, true, Id: 42);
        world.Activation = active;
        var events = new List<GameEvent>();
        var result = DamagePlacement.Resolve(world, world.Facts,
            new DamageRequest(target, target, 1, "synthetic", "Damage") { ActivationId = 42 }, events);
        Assert.False(result.IsComplete);
        var choice = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(Question.Option, choice.Asking);
        Sequence.Answer(world, world.Facts, runner, choice,
            Decision.Take(choice.Affordances[0].Id), events);
        // Synthetic hostile lifecycle change after admission: a pending raw id
        // must not write to a completed receipt after its replacement resumes.
        world.Activation = null;
        world.FinishedActivation = active;
        var digest = world.Digest().Canonical();
        var error = Assert.Throws<InvalidOperationException>(() =>
            Sequence.Work(world, world.Facts, runner, events));
        Assert.Contains("no live activation receipt", error.Message);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(0, target.Damage);
        Assert.Same(active, world.FinishedActivation);
        Assert.Equal(0, world.FinishedActivation!.DamagePlaced);
        Assert.False(result.IsComplete);
    }

}
