using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual-row controls on nonpublic boards; form mutation is a synthetic admission probe.</summary>
public sealed class DoctorStrangeLastSupportReviewTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:obligation.1")]
    [Rule("rr:obligation.4")]
    [Rule("rr:cost.1")]
    public void TollRemovalRequiresTheNamedAlterEgoReadyAndNeverExhaustsTheRevealer(bool alterEgo, bool exhausted)
    {
        // Obligations' "you" applies "only to the player whose play area the obligation is in".
        // A cost arrow distinguishes "a cost from an effect"; printed 09027 requires exhausting Stephen.
        var runner = PhysicalTollFixture.Runner();
        var (world, _, _, _) = SpellMasteryFixture.BoardActualWithRunner(runner, "09035", true);
        var stephen = world.Seats[1].IdentityCard;
        if (alterEgo) stephen.TurnTo("09001b");
        if (exhausted) stephen.Exhaust();
        var revealer = world.Seats[0].IdentityCard;
        var source = world.CreateCard("09027", world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            Round: 1, Number: 4, Subject: source.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        var form = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(1, form.Player);
        Assert.Equal(PlayArea.Of(1), source.Area.PlayArea);
        var remain = Assert.Single(form.Affordances, offer => offer.DisplayLabel!.Contains("Remain", StringComparison.Ordinal));
        Sequence.Answer(world, world.Facts, runner, form, Decision.Take(remain.Id), events);
        var choice = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(1, choice.Player);
        bool removable = alterEgo && !exhausted;
        Assert.Equal(removable, choice.Affordances.Any(offer => offer.Id == 0));
        if (!removable)
        {
            string before = SpellMasteryFixture.Snapshot(world);
            Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner,
                choice, Decision.Take(0), events));
            Assert.Equal(before, SpellMasteryFixture.Snapshot(world));
        }
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(removable ? 0 : 1), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(alterEgo ? "09001b" : "09001a", stephen.FaceId);
        Assert.Equal(!exhausted && !removable, stephen.Ready);
        Assert.True(revealer.Ready);
        Assert.Equal(removable ? DeckType.RemovedArea : DeckType.ObligationsArea, source.Area.Type);
        Assert.Equal(removable ? 0 : 1, world.Effects.Registered.Count(effect => effect.Card == source.ObjectId));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.2")]
    [Rule("rr:cost.7")]
    public void DefenseRequiresHeroFormAndPaymentControlledByItsDefendingPlayer()
    {
        // "the form of the player ... is checked now"; costs use "game elements they control".
        var (world, runner, source, payment, _) = DesperateDefensePrerequisiteFixture.Board(1, 4);
        var hero = world.Seats[1].IdentityCard;
        var helper = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, helper, Decision.Decline, []);
        var defense = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, defense, Decision.Take(hero.ObjectId), []);
        var interrupt = Sequence.Work(world, world.Facts, runner, [])!;
        var offer = Assert.Single(interrupt.Affordances, item => item.AnchorId == source.ObjectId);
        var occurrence = world.Agenda.Occurrence!;
        Assert.Contains(runner.Waiting(world, occurrence, WindowKind.Interrupt), item => item.Card == source.ObjectId);
        // Synthetic state probe isolates the actual row's form admission at the same committed defense.
        hero.TurnTo("01001b");
        Assert.DoesNotContain(runner.Waiting(world, occurrence, WindowKind.Interrupt), item => item.Card == source.ObjectId);
        hero.TurnTo("01001a");
        var foreign = world.CreateCard("01088", world.Seats[0].Hand);
        string before = SpellMasteryFixture.Snapshot(world);
        Assert.ThrowsAny<Exception>(() => Sequence.Answer(world, world.Facts, runner, interrupt,
            Decision.Take(offer.Id, [], [foreign.ObjectId]), []));
        Assert.Equal(before, SpellMasteryFixture.Snapshot(world));
        Assert.Equal(DeckType.HandsArea, foreign.Area.Type);
        Sequence.Answer(world, world.Facts, runner, interrupt, Decision.Take(offer.Id, [], [payment.ObjectId]), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.True(hero.Ready);
        Assert.Equal(0, hero.Damage);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }
}
