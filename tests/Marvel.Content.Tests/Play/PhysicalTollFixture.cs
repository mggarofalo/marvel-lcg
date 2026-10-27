using Marvel.Content.Tests.Cards;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Canonical Physical Toll program in a nonpublic engine fixture.</summary>
internal static class PhysicalTollFixture
{
    internal static AbilityRunner Runner() => AuthoredCards.Runner();

    internal static (World World, AbilityRunner Runner, Card Source) Reveal(bool flip = false, bool remove = false)
    {
        var runner = Runner();
        var (world, _, _, _) = SpellMasteryFixture.BoardActualWithRunner(runner, "09035", true);
        var source = world.CreateCard("09027", world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            Round: 1, Number: 4, Subject: source.ObjectId, Seat: 0));
        var events = new List<Marvel.Rules.Events.GameEvent>();
        var form = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(1, form.Player);
        Assert.False(form.Cancellable);
        Sequence.Answer(world, world.Facts, runner, form,
            Decision.Take(form.Affordances[flip ? 0 : 1].Id), events);
        var choice = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(1, choice.Player);
        var branch = Assert.Single(choice.Affordances, offer => offer.Id == (remove ? 0 : 1));
        Assert.Contains(remove ? "Exhaust Stephen" : "3 additional", branch.DisplayLabel);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(branch.Id), events);
        Sequence.Finish(world, world.Facts, runner, events);
        return (world, runner, source);
    }

    internal static IReadOnlyList<Marvel.Rules.Events.GameEvent> Play(World world, AbilityRunner runner, Card card, params int[] paying)
    {
        var action = Assert.Single(runner.Actions(world, 1), offer => offer.Card == card.ObjectId);
        var cost = Assert.Single(runner.Describe(world, action).CostOptions);
        var allocations = ResourcePayment.Allocate(cost, paying)!;
        var events = runner.ExecuteAction(world, new PlayerAction(action, paying, card.FaceId == "09005"
            ? [world.Seats[1].InvocationDeck!.PublicTop!.ObjectId] : [], Allocations: allocations)).Events.ToList();
        Sequence.Finish(world, world.Facts, runner, events);
        return events;
    }
}


