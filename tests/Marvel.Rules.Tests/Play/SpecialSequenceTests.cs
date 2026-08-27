using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class SpecialSequenceTests
{
    [Rule("rr:special")]
    [Fact]
    public void QueuedSpecialsResolveInOrderAndKnowWhichOneIsFinal()
    {
        // “Special abilities may only be resolved through the explicit
        // instruction of another card ability.” That instruction schedules
        // each selected card as data, so an intervening prompt cannot lose
        // the order or make every member final.
        var facts = new Printed();
        var world = Board(facts);
        var first = world.CreateCard(
            "first", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var second = world.CreateCard(
            "second", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var abilities = new Recorder();

        world.Agenda.Add(new PhaseStep(
            Steps.ResolveSpecial, 1, 1, Subject: first.ObjectId, Seat: 0, Plan: true));
        world.Agenda.Add(new PhaseStep(
            Steps.ResolveSpecial, 1, 2, Subject: second.ObjectId, Seat: 0, Plan: true,
            FinalStep: true));

        Assert.Null(Sequence.Work(world, facts, abilities, []));

        Assert.Equal(
            [(first.ObjectId, false), (second.ObjectId, true)],
            abilities.Specials);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:special")]
    [Fact]
    public void AChoiceInsideTheFinalSpecialKeepsTheFinalFlagWhenItResumes()
    {
        var facts = new Printed();
        var world = Board(facts);
        var source = world.CreateCard(
            "first", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var abilities = new Recorder();
        var step = new PhaseStep(
            Steps.ChooseOption, 1, 1, Index: 1, Subject: source.ObjectId, Seat: 0,
            Tier: AbilityType.Special, FinalStep: true);

        VillainPhase.Take(world, facts, abilities, step, []);
        VillainPhase.Answered(
            world, facts, abilities, step, Decision.Take(source.ObjectId), []);

        Assert.True(abilities.ChoosingWasFinal);
        Assert.True(abilities.ChoseWasFinal);
    }

    private static World Board(Printed facts)
    {
        var world = new World(facts, 1);
        world.CreateSeat("player");
        world.Seats[0].IdentityCard = world.CreateCard("hero", world.Seats[0].Hero);
        return world;
    }

    private sealed class Recorder : NoCardAbilities
    {
        public List<(int Card, bool Final)> Specials { get; } = [];

        public bool ChoosingWasFinal { get; private set; }

        public bool ChoseWasFinal { get; private set; }

        public override IReadOnlyList<GameEvent> ResolveSpecial(
            World world, Card card, int player, bool finalStep)
        {
            Specials.Add((card.ObjectId, finalStep));
            return [];
        }

        public override Prompt? Choosing(
            World world, Card source, int player, int stoppedAt,
            AbilityType? tier, bool finalStep)
        {
            ChoosingWasFinal = finalStep;
            return null;
        }

        public override IReadOnlyList<GameEvent> Chose(
            World world, Card source, int player, int stoppedAt, Decision input,
            AbilityType? tier, bool finalStep)
        {
            ChoseWasFinal = finalStep;
            return [];
        }
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "hero" => CardKind.Hero,
            "first" or "second" => CardKind.Upgrade,
            _ => CardKind.Treachery,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            faceId == "hero"
                ? new Dictionary<string, string> { ["HP"] = "10" }
                : new Dictionary<string, string>();

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? value)
            && long.TryParse(value, out long number)
                ? number
                : fallback;

        public long ConsequentialDamage(string faceId, string attribute) => 0;
    }
}
