using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic boards for The Galaxy's Most Wanted modular sets over the production ability book.</summary>
internal static class GalaxyModularBoards
{
    internal const string CloakOfHercules = "16122";
    internal const string ObediencePotion = "16123";
    internal const string BeyondersBlazer = "16124";
    internal const string ThePoison = "16125";
    internal const string VandarianPowerStone = "16126";
    internal const string MonarchEgg = "16127";
    internal const string MagicalTeapot = "16128";
    internal const string PhilosophersStone = "16129";
    internal const string CrystalBall = "16130";
    internal const string KreeCombatArmor = "16131";
    internal const string KreeCommando = "16132";
    internal const string KreeLieutenant = "16133";
    internal const string KreePrivate = "16134";
    internal const string PsionicGhost = "16135";
    internal const string ServantBot = "16136";
    internal const string Starshark = "16137";
    internal const string PirateCommander = "16138";
    internal const string PirateLackey = "16139";
    internal const string SoundTheAlarms = "16140";
    internal const string HonorAmongThieves = "16141";
    internal const string BlackCat = "01002";

    /// <summary>Spider-Man in every seat (seat 0 on <paramref name="face"/>) against Rhino.</summary>
    internal static World Board(int players = 1, string face = "01001a") => ShipCommandBoards.Board(players, face);

    /// <summary>Puts <paramref name="face"/> on top of the encounter deck.</summary>
    internal static Card OnTop(World world, string face) => world.CreateCard(face, world.AreaOf(DeckType.EncounterDeck));

    /// <summary>Black Cat in <paramref name="player"/>'s play area.</summary>
    internal static Card Ally(World world, int player = 0) => GrootBoards.InPlay(world, BlackCat, DeckType.AlliesArea, player);

    /// <summary>
    /// Drains pending work: <paramref name="answer"/> first, then indirect damage to each
    /// identity, the first offered order or mandatory option, and declines everything else.
    /// </summary>
    internal static List<GameEvent> Drive(World world, Func<Prompt, Decision?>? answer = null)
    {
        var events = new List<GameEvent>();
        var runner = GrootBoards.Runner(world);
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        for (int step = 0; prompt is not null; step++)
        {
            Assert.True(step < 80, prompt.Description);
            var decision = answer?.Invoke(prompt) ?? GrootEncounterTests.AssignTo(world.Seats[prompt.Player].IdentityCard)(prompt)
                ?? (prompt.Asking == Question.Order && prompt.Affordances[0].Targets is { } order
                    ? new Decision(prompt.Affordances[0].Id, order.Legal)
                    : prompt.Cancellable ? Decision.Decline
                    : Decision.Take(prompt.Affordances[0].Id));
            Sequence.Answer(world, world.Facts, runner, prompt, decision, events);
            prompt = Sequence.Work(world, world.Facts, runner, events);
        }
        return events;
    }

    /// <summary>The villain attacks <paramref name="player"/>, and the work drains.</summary>
    internal static List<GameEvent> VillainAttacks(World world, int player = 0, Func<Prompt, Decision?>? answer = null)
    {
        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: false, player);
        return Drive(world, answer);
    }
}
