using Marvel.Rules.Campaign;

namespace Marvel.Content.Setup;

/// <summary>Printed campaign composition; runtime admission remains the factory's responsibility.</summary>
public static class RiseOfRedSkullCampaign
{
    /// <summary>The five scenarios and campaign-only physical cards from Rise of Red Skull.</summary>
    public static CampaignDefinition Definition { get; } = new(
        "rise_of_red_skull",
        [new("crossbones", "crossbones_expert"), new("absorbing_man", "absorbing_man_expert"),
            new("taskmaster", "taskmaster_expert"), new("zola", "zola_expert"), new("red_skull", "red_skull_expert")],
        [Tech("04155"), Tech("04156"), Tech("04157"), Tech("04158")],
        [Condition("04159", 2), Condition("04160", 1), Condition("04161", 3), Condition("04162", 4)],
        ["04163", "04164", "04165", "04166"],
        ["04072", "04073", "04074", "04075"],
        ["04097", "04098", "04099", "04100"])
    {
        HydraPrisonFace = "04122",
        DelayCounterField = "k_delay",
    };

    // Engine choice: stable keys identify physical campaign cards independently of per-game object ids.
    private static CampaignCard Tech(string face) => new("tech:" + face, face, CampaignCardRole.Tech);

    private static CampaignCondition Condition(string number, int hitPointBonus) =>
        new(new("condition:" + number, number + "a", CampaignCardRole.Condition), number + "b", hitPointBonus);
}
