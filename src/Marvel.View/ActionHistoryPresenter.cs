using System.Globalization;
using System.Text;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;

namespace Marvel.View;

/// <summary>Formats completed actions without exposing raw event diagnostics.</summary>
public static class ActionHistoryPresenter
{
    /// <summary>Builds one complete action entry from authorized engine facts.</summary>
    public static ActionHistoryPresentation PresentEntry(
        ActionHistoryFacts action,
        IReadOnlyList<GameEvent> events,
        WorldDescriptor world)
    {
        string summary = Present(action);
        IReadOnlyList<string> details = ActionHistoryResultPresentation.Present(action, events, world);
        bool discardIsRootChoice = action.Phase is "Mulligan" or "EndPhase";
        if (action.Outcome is null && discardIsRootChoice && details.Count > 0)
        {
            summary = details[0];
            details = details.Skip(1).ToArray();
        }
        return new ActionHistoryPresentation(summary, details);
    }

    /// <summary>Returns one player-facing sentence for a completed history unit.</summary>
    public static string Present(ActionHistoryFacts action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(action.Actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(action.Action);
        ArgumentNullException.ThrowIfNull(action.ResourceGeneratorIds);
        ArgumentNullException.ThrowIfNull(action.ResourceGenerators);

        string summary = Summary(action);
        return action.Outcome is { } outcome
            ? $"{summary} {EventPresenter.Terminal(outcome).Summary}"
            : summary;
    }

    private static string Summary(ActionHistoryFacts action)
    {
        if (string.Equals(action.Verb, CardPlay.Verb, StringComparison.Ordinal))
        {
            return PlaySummary(action);
        }
        else if (string.Equals(action.Verb, Game.ResolveMulligans, StringComparison.Ordinal))
        {
            return $"{action.Actor} chose their opening hand.";
        }
        else if (string.Equals(action.Verb, Game.ChangeForm, StringComparison.Ordinal))
        {
            return $"{action.Actor} changed form.";
        }
        else if (string.Equals(action.Verb, Game.EndPhaseVerb, StringComparison.Ordinal))
        {
            return EndPhaseSummary(action);
        }
        else if (string.Equals(action.Verb, BasicPowers.AttackVerb, StringComparison.Ordinal))
        {
            return BasicPowerSummary(action, "attacked");
        }
        else if (string.Equals(action.Verb, BasicPowers.ThwartVerb, StringComparison.Ordinal))
        {
            return BasicPowerSummary(action, "thwarted");
        }
        else if (string.Equals(action.Verb, BasicPowers.RecoverVerb, StringComparison.Ordinal))
        {
            return $"{action.Actor} recovered.";
        }
        else
        {
            return OtherSummary(action);
        }
    }

    private static string EndPhaseSummary(ActionHistoryFacts action) => action.Phase switch
    {
        "PlayerTurn" => $"{action.Actor} ended their turn.",
        "EndPhase" => $"{action.Actor} finished choosing hand discards.",
        _ => OtherSummary(action),
    };

    private static string PlaySummary(ActionHistoryFacts action)
    {
        string payment = action.ResourceGenerators.Count == 0
            ? string.Empty
            : $", generating resources from {Names(action.ResourceGenerators)}";
        return $"{action.Actor} played {action.Action}{payment}.";
    }

    private static string OtherSummary(ActionHistoryFacts action)
    {
        string choice = action.Action.Trim().TrimEnd('.');
        string phase = Words(action.Phase);
        string phaseName = phase.EndsWith(" phase", StringComparison.Ordinal)
            ? phase
            : $"{phase} phase";
        return action.Role == "phase_step"
            || !string.Equals(action.Phase, "PlayerTurn", StringComparison.Ordinal)
            ? $"{action.Actor} resolved {choice} during the {phaseName}."
            : $"{action.Actor} used {choice}.";
    }

    private static string BasicPowerSummary(ActionHistoryFacts action, string verb) =>
        string.Equals(action.Actor, action.Action, StringComparison.Ordinal)
            ? $"{action.Actor} {verb}."
            : $"{action.Actor} {verb} with {action.Action}.";

    /// <summary>Describes effect discards while omitting play and payment mechanics.</summary>
    public static IReadOnlyList<string> PresentDiscardDetails(
        ActionHistoryFacts action,
        IReadOnlyList<GameEvent> events,
        WorldDescriptor world) => ActionHistoryResultPresentation.Discards(action, events, world);

    private static string Names(IReadOnlyList<string> names) => names.Count switch
    {
        1 => names[0],
        2 => $"{names[0]} and {names[1]}",
        _ => $"{string.Join(", ", names.Take(names.Count - 1))}, and {names[^1]}",
    };

    private static string Words(string value)
    {
        var words = new StringBuilder(value.Length + 4);
        for (int index = 0; index < value.Length; index++)
        {
            if (index > 0 && char.IsUpper(value[index]) && char.IsLower(value[index - 1]))
            {
                words.Append(' ');
            }
            words.Append(char.ToLowerInvariant(value[index]));
        }
        return words.ToString();
    }
}
