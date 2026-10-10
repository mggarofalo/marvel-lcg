using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Executes authored card abilities at the scenario setup step.</summary>
internal static class SetupAbilityResolution
{
    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> Setup(this AbilityResolutionExecution execution, World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        if (!execution.program.Authored.Contains(card.FaceId))
        {
            // The same distinction `WhenRevealed` makes, and setup is where it
            // matters most: a scenario whose main scheme nobody has read would
            // otherwise deal a board that is quietly missing whatever its first
            // card said, and every later assertion would be about the wrong
            // game.
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' is being set up and no ability data is written for it; "
                + $"this engine has {execution.program.Authored.Count} authored card(s)");
        }

        var events = new List<GameEvent>();

        // `rr:setup-triggered-ability.2` times these to a step of setup rather
        // than to anything happening, so `Steps.Setup` is the step's name and
        // not a triggering condition -- no card can name it, because the reader
        // refuses an `event` on a Setup ability. What it is for is the events:
        // a board built during setup is told apart in the stream from one built
        // during a round.
        //
        // There is no player whose turn it is either. The card's owner resolves
        // it, which for an encounter card is the scenario.
        var occurrence = new Occurrence(
            0, [Steps.Setup], Subject: card.ObjectId, Player: card.Owner);

        foreach (var ability in execution.On(card))
        {
            if (ability.Trigger.Timing == AbilityType.Setup)
            {
                var cast = new AbilityResolutionState(world, card, occurrence, card.Owner, events)
                {
                    Tier = ability.Trigger.Timing,
                };
                execution.TrackResolution(cast, ability);
                execution.Run(ability, cast);
                cast.CompleteResolution();
            }
        }

        return events;
    }

}
