using Marvel.Session;

namespace Marvel.Server;

/// <summary>Verifies and publishes content compatibility for an entire campaign authority aggregate.</summary>
internal static class CampaignContentUpgrade
{
    internal static StoredCampaign Restore(
        ICampaignGameFactory factory, ICampaignStore store, StoredCampaign previous)
    {
        CampaignSave upgraded = Verify(factory, previous.Save);
        if (ReferenceEquals(upgraded, previous.Save)) return previous;
        if (store is not IContentUpgradeCampaignStore upgrades)
            throw new SessionSaveException("this store cannot preserve a campaign upgrade predecessor");
        var candidate = previous with { Save = upgraded };
        FileCampaignStore.Validate(candidate);
        upgrades.CommitContentUpgrade(previous, candidate);
        return candidate;
    }

    internal static CampaignSave Verify(ICampaignGameFactory factory, CampaignSave original)
    {
        if (factory is not IContentUpgradeFactory migration
            || migration.ContentUpgrade(original.Compatibility) is not { } route)
            return original;
        return route.Verify(original, factory.CampaignDefinition!,
            setup => CampaignTransactions.Open(factory, setup), factory.InspectCampaign);
    }
}
