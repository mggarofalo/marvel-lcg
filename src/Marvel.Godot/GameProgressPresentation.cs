using Marvel.Rules.Play;
using Marvel.Server;

namespace Marvel.Godot;

/// <summary>The visibly distinct state of the local table.</summary>
public enum GameProgressKind
{
    /// <summary>The authoritative prompt is accepting one decision.</summary>
    AwaitingDecision,

    /// <summary>One mutation was sent and its outcome is not yet known.</summary>
    Resolving,

    /// <summary>The players defeated the final villain stage.</summary>
    PlayersWin,

    /// <summary>The villain completed the final main scheme.</summary>
    VillainWins,

    /// <summary>The players lost without the villain winning.</summary>
    PlayersLose,

    /// <summary>An error occurred, but a subsequent sync recovered the table.</summary>
    Recovered,

    /// <summary>A sent mutation has no authoritative result and cannot be repeated.</summary>
    Unconfirmed,

    /// <summary>The product could not open or load a table.</summary>
    Unavailable,
}

/// <summary>Copy and input policy for one current table state.</summary>
public sealed record GameProgressPresentation(
    GameProgressKind Kind,
    string Title,
    string Description,
    string Status,
    bool LocksDecisions)
{
    /// <summary>Describes one complete authoritative response.</summary>
    public static GameProgressPresentation FromResponse(EngineResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(response.World);
        return response.World.Outcome switch
        {
            Outcome.Unfinished => new(
                GameProgressKind.AwaitingDecision,
                "Your move.",
                $"{response.World.Areas.Count} visible areas · "
                    + $"{response.Events.Count} new events",
                "DECISION READY  ·  "
                    + Humanize(response.Prompt!.Asking.ToString()).ToUpperInvariant(),
                LocksDecisions: false),
            Outcome.PlayersWin => new(
                GameProgressKind.PlayersWin,
                "Victory.",
                "The players defeated the final villain stage.",
                "GAME COMPLETE  ·  PLAYERS WIN",
                LocksDecisions: true),
            Outcome.VillainWins => new(
                GameProgressKind.VillainWins,
                "Defeat.",
                "The villain completed the final main scheme.",
                "GAME COMPLETE  ·  VILLAIN WINS",
                LocksDecisions: true),
            Outcome.PlayersLose => new(
                GameProgressKind.PlayersLose,
                "Defeat.",
                "The players lost when the encounter could not continue.",
                "GAME COMPLETE  ·  PLAYERS LOSE",
                LocksDecisions: true),
            _ => throw new ArgumentOutOfRangeException(
                nameof(response), response.World.Outcome, "unknown game outcome"),
        };
    }

    /// <summary>Shows that one mutation is in flight and cannot be repeated.</summary>
    public static GameProgressPresentation Resolving() => new(
        GameProgressKind.Resolving,
        "Resolving decision…",
        "The engine is committing the selected action.",
        "DECISION SENT  ·  WAITING FOR THE AUTHORITATIVE TABLE",
        LocksDecisions: true);

    /// <summary>Preserves an authoritative recovered table while explaining its error.</summary>
    public static GameProgressPresentation Recovered(
        EngineResponse response,
        ClientStartupError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        GameProgressPresentation current = FromResponse(response);
        return current.Kind == GameProgressKind.AwaitingDecision
            ? new GameProgressPresentation(
                GameProgressKind.Recovered,
                "Table recovered.",
                error.Message,
                $"CURRENT DECISION RESTORED  ·  {error.Code.ToUpperInvariant()}",
                LocksDecisions: false)
            : current with
            {
                Description = current.Description + " The final table was recovered after: "
                    + error.Message,
            };
    }

    /// <summary>Locks a table whose sent mutation could not be reconciled.</summary>
    public static GameProgressPresentation Unconfirmed(ClientStartupError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(
            GameProgressKind.Unconfirmed,
            "Table unconfirmed.",
            error.Message + " Restart or reconnect before taking another action.",
            $"MUTATION NOT REPEATED  ·  {error.Code.ToUpperInvariant()}",
            LocksDecisions: true);
    }

    /// <summary>Shows a setup or product failure rather than a gameplay ending.</summary>
    public static GameProgressPresentation Unavailable(ClientStartupError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(
            GameProgressKind.Unavailable,
            "Game unavailable.",
            error.Message,
            $"PRODUCT ERROR  ·  {error.Code.ToUpperInvariant()}",
            LocksDecisions: true);
    }

    internal static string Humanize(string value)
    {
        var result = new System.Text.StringBuilder(value.Length + 8);
        for (int index = 0; index < value.Length; index++)
        {
            char current = value[index];
            if (index > 0 && (current == '_'
                || char.IsUpper(current) && char.IsLower(value[index - 1])))
            {
                result.Append(' ');
            }

            if (current != '_')
            {
                result.Append(current);
            }
        }
        return result.ToString();
    }
}
