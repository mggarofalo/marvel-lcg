using System.Text.RegularExpressions;
using Marvel.Rules.Play;

namespace Marvel.Behavior.Run;

internal static class CoreTranscriptOutcomeAssertions
{
    internal static void NotEliminated(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        if (context.World.Seats[seat].Eliminated)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected seat {seat + 1} not to be eliminated");
        }
    }

    internal static void GameUnfinished(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.World.Result is not Outcome.Unfinished)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected an unfinished game; was {context.World.Result}");
        }
    }

    internal static void CatalogedException(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.ExpectedException is null)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: no cataloged exception is expected for this obligation");
        }

        string actual = context.PendingException is null
            ? "(none)"
            : $"{context.PendingException.GetType().Name}: {context.PendingException.Message}";
        if (!string.Equals(actual, context.ExpectedException, StringComparison.Ordinal))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected '{context.ExpectedException}'; reached '{actual}'");
        }

        context.ExceptionObserved = true;
    }

    internal static void PlayersLose(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.World.Result is not Outcome.PlayersLose)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected PlayersLose; was {context.World.Result}");
        }
    }

    internal static void VillainWins(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.World.Result is not Outcome.VillainWins)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected VillainWins; was {context.World.Result}");
        }
    }

    internal static void PlayersWin(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.World.Result is not Outcome.PlayersWin)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected PlayersWin; was {context.World.Result}");
        }
    }

    internal static void SeatEliminated(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        int seat = Seat(match, step);
        if (!context.World.Seats[seat].Eliminated)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected seat {seat + 1} to be eliminated");
        }
    }

    internal static void AttackEnded(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        _ = match;
        if (context.World.Attack is not null || context.World.Activation is not null)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected the attack and its activation to have ended");
        }
    }

    internal static void CombinedTriggeringConditions(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        var occurrence = context.World.Agenda.Occurrence
            ?? throw new TranscriptAssertionException(
                $"{step.Location}: expected a pending occurrence");
        string first = match.Groups["first"].Value;
        string second = match.Groups["second"].Value;
        if (!occurrence.Conditions.Contains(first, StringComparer.Ordinal)
            || !occurrence.Conditions.Contains(second, StringComparer.Ordinal))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected one occurrence containing {first} and {second}; "
                + $"was {string.Join(", ", occurrence.Conditions)}");
        }
    }
}
