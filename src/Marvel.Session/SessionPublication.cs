namespace Marvel.Session;

/// <summary>Ordinary publication cannot replace a verified migration with stale runtime or content authority.</summary>
public static class SessionPublication
{
    /// <summary>Requires exact gameplay compatibility and rejects an application downgrade before publication.</summary>
    public static void RequireCompatible(SessionCompatibility previous, SessionCompatibility candidate)
    {
        SessionReplay.RequireRuntimeCompatibility(candidate, previous);
        if (previous with { Application = candidate.Application } != candidate)
            throw new SessionSaveException("session compatibility changed; restore the current generation before writing");
    }
}
