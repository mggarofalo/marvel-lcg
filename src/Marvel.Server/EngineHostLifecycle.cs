using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.View;
using System.Diagnostics;
using System.Security.Cryptography;

using static Marvel.Server.EngineHostResolution;
using static Marvel.Server.EngineHostHistory;
using static Marvel.Server.EngineHostLifecycle;

namespace Marvel.Server;

/// <summary>Owns lifecycle operations for an engine host.</summary>
internal static class EngineHostLifecycle
{
    internal static EngineResponse Close(this EngineHost host, EngineRequest request, RequestExecution execution)
    {
        if (request.Game is not null
            || request.Decision is not null
            || request.Viewer is not null
            || request.ExpectedRevision is not null
            || request.Cursor is not null
            || request.Order is not null)
        {
            return Failed(
                request, "invalid_request",
                "close does not accept game or decision");
        }

        if (!host.authority.TrySession(request, out string capability, out var access))
        {
            return Failed(request, "session_not_found", "the session capability is not valid");
        }

        if (access.Owner)
        {
            SessionSave retired = access.Session.Save with
            {
                Compatibility = host.compatibility,
                Session = access.Session.Save.Session with { Lifecycle = "retired" },
            };
            execution.ObservePersistence(() =>
                host.store.Commit(new StoredSession(retired, [])));
            host.authority.Remove(access.Session);
            execution.MarkSessionRetired();
        }
        else
        {
            string verifier = SessionAuthorityRegistry.Verifier(capability);
            List<StoredAuthority> authorities =
                host.authority.RevokedAuthorities(access.Session, verifier);
            SessionSave stamped = host.Stamp(access.Session.Save);
            execution.ObservePersistence(() =>
                host.store.Commit(new StoredSession(stamped, authorities)));
            access.Session.Publish(stamped);
            host.authority.Revoke(verifier);
        }

        return AuthorizedSessionProjector.Succeeded(request);
    }

    internal static void Restore(this EngineHost host)
    {
        foreach (SessionLoadResult candidate in host.store.LoadForRestore())
        {
            if (candidate.Session is not StoredSession stored)
            {
                LogUnreadable(host, candidate);
                continue;
            }

            if (stored.Save.Session.Lifecycle != "retired") RestoreCandidate(host, candidate, stored);
        }
    }

    private static void LogUnreadable(EngineHost host, SessionLoadResult candidate) =>
        host.log.Write(OperationalEventIds.SessionRestoreFailed, "rejected",
            gameId: candidate.StorageId, saveGeneration: candidate.Generation,
            stage: "quarantine", errorCode: candidate.ErrorCode ?? "restore_failed");

    private static void RestoreCandidate(
        EngineHost host, SessionLoadResult candidate, StoredSession stored)
    {
        var attempt = new SessionRestoreAttempt(host, candidate.Generation, stored);
        attempt.Restore();
    }

    internal static SessionSave Stamp(this EngineHost host, SessionSave save) => save with
    {
        Compatibility = host.compatibility,
    };

    internal static ReplayOpenedGame ReplayOpen(this EngineHost host, SessionSetup setup)
    {
        OpenedGame opened = host.factory.Create(new GameSpecification(
            setup.Scenario, setup.Heroes, setup.ModularSets, setup.Seed));
        return new ReplayOpenedGame(opened.Game, opened.SetupEvents);
    }

    internal static string NewStorageId() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    internal static SessionSetup ToSessionSetup(GameSpecification setup) =>
        new(setup.Scenario, [.. setup.Heroes],
            setup.ModularSets is null ? null : [.. setup.ModularSets], setup.Seed);

    internal static SessionSave Append(
        SessionSave save,
        JournalStep step,
        bool startsUnit,
        bool completesUnit,
        string role,
        int actor,
        int active,
        int round,
        string phase,
        Prompt? currentPrompt,
        IReadOnlyList<InformationExposure> exposures)
    {
        var units = save.Units.Take(save.Cursor).Select(unit => unit with
        {
            Decisions = [.. unit.Decisions],
        }).ToList();
        if (startsUnit)
        {
            units.Add(new JournalUnit(
                role,
                completesUnit ? "complete" : "open",
                actor,
                active,
                round,
                phase,
                [step],
                exposures));
        }
        else
        {
            if (units.Count == 0 || units[^1].Status != "open")
            {
                throw new ReplayDivergenceException(
                    "a dependent decision has no open history unit");
            }

            JournalUnit open = units[^1];
            units[^1] = open with
            {
                Status = completesUnit ? "complete" : "open",
                Decisions = [.. open.Decisions, step],
                Exposures = InformationFrontier.Merge(open.Exposures, exposures),
            };
        }

        if (currentPrompt is null)
        {
            units[^1] = units[^1] with { Role = "terminal", Status = "complete" };
        }

        return save with
        {
            Revision = save.Revision + 1,
            Cursor = units.Count,
            EditFrontier = exposures.Count > 0 ? units.Count : save.EditFrontier,
            CurrentPrompt = currentPrompt is null ? null : PromptRecord.From(currentPrompt),
            Units = units,
        };
    }

    internal static SessionCompatibility TestCompatibility() => new(
        Application: "test",
        ReplayContract: EngineBuildIdentity.ReplayContract,
        RngContract: "mt19937-iso-cxx",
        StateDigest: "state-digest-v2",
        CardsSha256: new string('0', 64),
        SetupSha256: new string('0', 64),
        AbilitiesSha256: new string('0', 64));

    internal static EngineResponse Failed(
        EngineRequest request, string code, string message) =>
        new(
            EngineProtocol.Version,
            Bounded(request.RequestId, EngineProtocol.MaximumIdentifierLength),
            Bounded(request.GameId, EngineProtocol.MaximumIdentifierLength),
            Capability: null,
            Prompt: null,
            Events: [],
            World: null,
            Error: new EngineError(
                Bounded(code, EngineProtocol.MaximumIdentifierLength),
                Bounded(message, EngineProtocol.MaximumErrorLength)));

    internal static string Bounded(string? value, int maximum) => value switch
    {
        null => string.Empty,
        { Length: var length } when length <= maximum => value,
        _ => value[..maximum],
    };

}
