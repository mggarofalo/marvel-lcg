using Marvel.Rules.Campaign;

namespace Marvel.Session;

/// <summary>Checks that campaign facts come from the admitted attempt and extend its committed effects.</summary>
public static class CampaignProof
{
    /// <summary>Validates the immutable attempt inputs against its prepared campaign.</summary>
    public static void RequireSetup(CampaignState state, SessionSave proof)
    {
        if (proof.Setup.Seed != CampaignSeed.ForAttempt(state.Seed, state.Stage, state.Attempt)
            || !proof.Setup.Heroes.SequenceEqual(state.Players.Where(player => player.Participating).Select(player => player.Hero))
            || CampaignSaveJson.Canonical(proof.Setup.Campaign) != CampaignSaveJson.Canonical(CampaignRules.Setup(state))
            || CampaignSaveJson.Canonical(proof.Setup.PlayerDecks) != CampaignSaveJson.Canonical(state.Players.Where(player => player.Participating).Select(player => player.Deck).ToArray()))
            throw new ReplayDivergenceException("campaign attempt setup differs from prepared authority");
        if (proof.Units.Count != 0)
            throw new ReplayDivergenceException("attempt start contains decisions made before admission");
    }

    /// <summary>Rejects history changes crossing an already committed campaign effect.</summary>
    public static void RequireExtension(SessionSave previous, SessionSave next)
    {
        if (CampaignSaveJson.Canonical(previous.Setup) != CampaignSaveJson.Canonical(next.Setup)
            || previous.Session.StorageId != next.Session.StorageId || previous.Revision > next.Revision)
            throw new ReplayDivergenceException("campaign attempt proof changes its admitted identity");
        var prefix = previous.Units.Take(previous.Cursor).SelectMany(unit => unit.Decisions).ToArray();
        var steps = next.Units.Take(next.Cursor).SelectMany(unit => unit.Decisions).Take(prefix.Length).ToArray();
        if (CampaignSaveJson.Canonical(prefix) != CampaignSaveJson.Canonical(steps))
            throw new ReplayDivergenceException("game history crosses a committed campaign effect");
    }
}
