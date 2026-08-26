using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Tests;

/// <summary>
/// Runs the agenda out, for a test that caused something and wants it finished.
/// </summary>
/// <remarks>
/// <para>
/// A great many things in this engine are steps rather than calls, because a
/// step can stop and ask a player something. A basic attack is one of them —
/// <c>rr:attack-player-ability-type.step.7</c> puts abilities around it — so a
/// test that calls <c>BasicPowers.BasicAttack</c> has scheduled an attack and
/// not dealt one.
/// </para>
/// <para>
/// Declining everything is deliberate. A test that wants a particular answer
/// given should drive <c>Sequence</c> itself and say so; this is for the far
/// commoner case of a test about what the board looks like once the thing it
/// caused has finished happening.
/// </para>
/// </remarks>
public static class Agendas
{
    /// <summary>Takes every outstanding step, declining every question.</summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do, if anything.</param>
    /// <returns>Everything that happened.</returns>
    public static List<Rules.Events.GameEvent> Finish(
        World world, ICardFacts facts, ICardAbilities? abilities = null)
    {
        var cards = abilities ?? new NoCardAbilities();
        var events = new List<Rules.Events.GameEvent>();
        var asked = Sequence.Work(world, facts, cards, events);
        for (int answered = 0; asked is not null; answered++)
        {
            // Bounded, because the failure this is most likely to meet is a
            // step that asks the same question for ever -- and a test that
            // hangs says far less than one that fails.
            Assert.True(answered < 20, $"'{asked.Label}' is still being asked");
            Sequence.Answer(world, facts, cards, asked, Decision.Decline, events);
            asked = Sequence.Work(world, facts, cards, events);
        }

        return events;
    }
}
