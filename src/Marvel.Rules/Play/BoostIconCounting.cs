using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Counts boost icons, opening the count's interrupt window when an ability observes it.</summary>
/// <remarks>
/// <para>
/// Ruling 6962a015e8597900: "Boost icons are counted whenever a game step or card
/// ability determines the exact number of boost icons on an encounter card."
/// Ruling 310c4caa404d0f5a applies this to a card ability's "2 or more boost
/// icons" test, and ruling 4e1ff06e6c235760 to a count made by another player's
/// card. A boost card turned faceup during an activation is counted in its faceup
/// window (ruling 937d6ff4f970a1da); <see cref="BoostFaceupOperation"/> carries
/// that count.
/// </para>
/// <para>
/// The window is scheduled only when an interrupt is waiting for it. Engine
/// choice: an unobserved count completes inline, so content without a count
/// interrupt keeps its events, agenda and decisions.
/// </para>
/// </remarks>
public static class BoostIconCounting
{
    /// <summary>Engine spelling of the "when boost icons on an encounter card are counted" condition.</summary>
    public const string Counted = "WhenBoostIconsCounted";

    /// <summary>Engine spelling of the constant that adds a boost icon to every encounter card.</summary>
    public const string EncounterIncreaseField = "encounter_boost_icons";

    /// <summary>The boost icons on one card now, before any count adjusts them.</summary>
    /// <remarks>
    /// <c>rr:boost-boost-icon.1</c>: "A star icon is not itself considered a boost
    /// icon", so the printed numeric Boost value is the base. Modifiers to that card
    /// apply, and an encounter card also gains one icon for each in-play
    /// <see cref="EncounterIncreaseField"/> modifier (The Next Evolution, 15024).
    /// Amplify belongs to a boost card during its activation and is added there.
    /// </remarks>
    public static long Icons(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        long printed = Characteristics.IsLost(world, card, "boost_const") ? 0
            : StateFields.Modified(world, card, "boost_const", world.Facts, world.Players);
        return Math.Max(0, printed + (card.Owner == World.Scenario ? EncounterIncrease(world) : 0));
    }

    /// <summary>The icons every encounter card gains from constants in play.</summary>
    public static long EncounterIncrease(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        long total = 0;
        foreach (var area in world.Areas)
        {
            if (!DeckTypes.IsInPlay(area.Type)) continue;
            foreach (var card in area.Cards)
                total += StateFields.Modified(world, card, EncounterIncreaseField, world.Facts, world.Players);
        }
        return total;
    }

    /// <summary>Whether an interrupt is waiting for this count's window.</summary>
    /// <remarks>A read-only query: the candidate occurrence is never put on the agenda.</remarks>
    public static bool Interrupted(World world, BoostIconCount count, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(count);
        return world.WindowAbilities.Waiting(world, Occurrence(0, count, player), WindowKind.Interrupt).Count > 0;
    }

    /// <summary>Schedule each count's interrupt window in order, then the continuation.</summary>
    public static void Schedule(World world, IReadOnlyList<BoostIconCount> counts, int player, PhaseStep continuation)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(counts);
        var current = world.Agenda.Current;
        var steps = new List<PhaseStep>();
        foreach (var count in counts)
        {
            // Engine choice: a fresh dynamic id, as a would-ready window uses, keeps
            // this count distinct from the ability that asked for it.
            steps.Add(new PhaseStep(new BoostIconCountOperation(count),
                current?.Round ?? 0, current?.Number ?? 0, Subject: count.Original.ObjectId, Seat: player)
            { OccurrenceId = world.Agenda.nextPlayerActionOccurrence-- });
        }
        steps.Add(continuation);
        if (current is not null) world.Agenda.Now([.. steps]);
        else foreach (var step in steps) world.Agenda.Add(step);
    }

    internal static Occurrence Occurrence(int id, BoostIconCount count, int player) =>
        new(id, [Counted], Subject: count.Original.ObjectId, Player: player) { BoostCount = count };

    internal static Prompt? Apply(PhaseStep step)
    {
        step.Require<BoostIconCountOperation>().Count.Freeze();
        return null;
    }
}
