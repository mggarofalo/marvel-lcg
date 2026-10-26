using System.Text.Json.Nodes;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Test-local 09029 interpretation; no production row or product admission.</summary>
internal static class HostedInvocationFixture
{
    internal static (World World, InvocationDeck Deck, Card Scheme, AbilityRunner Runner) Board(
        bool last = false, bool choice = false)
    {
        var (world, _, deck, _, _) = InvocationTopDiscardFixture.Board(0, last);
        string release = """{"releaseBeneath":{"invocationDeck":"true"}}""";
        if (choice) release = """{"seq":[RELEASE,{"chooseCard":{"from":{"query":"identities"},"chooser":"firstPlayer","effect":{"draw":{"player":"firstPlayer","count":1}}}}]}""".Replace("RELEASE", release, StringComparison.Ordinal);
        var row = JsonNode.Parse("""
            {"card":"09029","abilities":[
              {"trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
               "effect":{"storeBeneath":{"cards":{"query":"invocationTop"},"host":"this"}}},
              {"trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
               "effect":RELEASE}]}
            """.Replace("RELEASE", release, StringComparison.Ordinal));
        var runner = DoctorStrangeAbilityFixture.WithReplacements([row]);
        world.Abilities = runner;
        var scheme = world.CreateCard("09029", world.AreaOf(DeckType.EncounterDeck));
        return (world, deck, scheme, runner);
    }

    internal static void Reveal(World world, Card scheme, AbilityRunner runner, List<GameEvent> events)
    {
        World.MoveToTop(scheme, world.AreaOf(DeckType.RevealingArea, PlayArea.Of(0)));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 1, Subject: scheme.ObjectId, Seat: 0));
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
    }

    internal static Prompt? Defeat(World world, Card scheme, AbilityRunner runner, List<GameEvent> events)
    {
        Threat.Remove(world, world.Facts, world.ThreatAbilities,
            new(scheme, scheme.Tokens["k_threat"], "test", "Remove_Threat", 0)
                { RecordDefeatOn = new Marvel.Rules.Timing.Occurrence(100, [], Player: 0) }, events);
        return Sequence.Work(world, world.Facts, runner, events);
    }
}
