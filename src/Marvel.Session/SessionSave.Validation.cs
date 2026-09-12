namespace Marvel.Session;

/// <content>Structural validation steps for the canonical session save.</content>
public static partial class SessionSaveJson
{
    private static void RequireSupportedEnvelope(SessionSave save)
    {
        if (!string.Equals(save.Format, SessionSave.FormatName, StringComparison.Ordinal))
        {
            throw new SessionSaveException("save format is not supported");
        }

        if (save.Schema != SessionSave.CurrentSchema)
        {
            throw new SessionSaveException($"save schema {save.Schema} is not supported");
        }
    }

    private static void RequireRecords(SessionSave save)
    {
        if (save.Compatibility is null || save.Session is null || save.Setup is null
            || save.Initial is null || save.Units is null)
        {
            throw new SessionSaveException("save is missing a required record");
        }
    }

    private static void RequireCompatibilityIdentity(SessionCompatibility compatibility)
    {
        bool namesArePresent = !string.IsNullOrWhiteSpace(compatibility.Application)
            && !string.IsNullOrWhiteSpace(compatibility.ReplayContract)
            && !string.IsNullOrWhiteSpace(compatibility.RngContract)
            && !string.IsNullOrWhiteSpace(compatibility.StateDigest);
        bool datasetsAreIdentified = Sha256(compatibility.CardsSha256)
            && Sha256(compatibility.SetupSha256)
            && Sha256(compatibility.AbilitiesSha256);
        if (!namesArePresent || !datasetsAreIdentified)
        {
            throw new SessionSaveException("save compatibility identity is invalid");
        }
    }

    private static void RequireHistoryBounds(SessionSave save)
    {
        if (save.Revision < 0 || save.Cursor < 0 || save.Cursor > save.Units.Count
            || save.EditFrontier < 0 || save.EditFrontier > save.Cursor)
        {
            throw new SessionSaveException("save history bounds are invalid");
        }
    }

    private static void RequireSetup(SessionSetup setup)
    {
        if (setup.Heroes is not { Count: > 0 }
            || setup.Heroes.Any(string.IsNullOrWhiteSpace)
            || string.IsNullOrWhiteSpace(setup.Scenario))
        {
            throw new SessionSaveException("save setup is invalid");
        }
    }

    private static void RequireSessionIdentity(SessionIdentity session)
    {
        if (!StorageId(session.StorageId)
            || string.IsNullOrWhiteSpace(session.Label)
            || session.Label.Length > 256
            || session.Lifecycle is not ("active" or "retired"))
        {
            throw new SessionSaveException("save session identity is invalid");
        }
    }

    private static void RequireReplayRecords(SessionSave save)
    {
        if (save.Initial.Events is null
            || save.Initial.RngWords < 0
            || string.IsNullOrEmpty(save.Initial.StateDigest)
            || save.Units.Any(unit => InvalidUnit(unit, save.Setup.Heroes.Count)))
        {
            throw new SessionSaveException("save replay records are invalid");
        }
    }

    private static bool InvalidUnit(JournalUnit? unit, int playerCount) =>
        unit is null
        || unit.Decisions is not { Count: > 0 }
        || unit.Exposures is null
        || unit.Status is not ("open" or "complete")
        || unit.Decisions.Any(InvalidStep)
        || unit.Exposures.Any(exposure =>
            !InformationFrontier.IsCanonical(exposure, playerCount))
        || unit.Exposures.Select(exposure => exposure.Reason)
            .Distinct(StringComparer.Ordinal).Count() != unit.Exposures.Count;

    private static bool InvalidStep(JournalStep? step) =>
        step is null
        || step.Prompt is null
        || step.Decision is null
        || step.Events is null
        || step.RngWords < 0
        || string.IsNullOrEmpty(step.StateFingerprint)
        || step.Result is { Outcome: null or "" }
        || step.Result is { Round: < 0 };

    private static void RequireHistoryShape(SessionSave save)
    {
        int open = -1;
        int recordedFrontier = 0;
        for (int index = 0; index < save.Units.Count; index++)
        {
            JournalUnit unit = save.Units[index];
            if (unit.Exposures.Count > 0)
            {
                recordedFrontier = index + 1;
            }

            if (unit.Status == "open")
            {
                RequireOpenUnit(open, index, save.Cursor);
                open = index;
            }
        }

        if (recordedFrontier != save.EditFrontier)
        {
            throw new SessionSaveException("save information frontier is invalid");
        }
    }

    private static void RequireOpenUnit(int previousOpen, int index, int cursor)
    {
        if (previousOpen >= 0 || index != cursor - 1)
        {
            throw new SessionSaveException("save has an invalid open history unit");
        }
    }
}
