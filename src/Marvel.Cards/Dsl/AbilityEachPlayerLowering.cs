namespace Marvel.Cards.Dsl;

internal static class AbilityEachPlayerLowering
{
    internal static AbilityEffect.EachPlayer Lower(AbilityValue value, AbilityLocation location)
    {
        var fields = AbilitySelectorLowering.Fields(value, location, "effect", "player", "order");
        bool restricted = fields.TryGetValue("player", out var player);
        if (restricted && AbilityLowering.Text(player!, location.Child("player")) != "defeater")
            throw location.Child("player").Error("only the credited defeater is supported as a single-player frame");
        bool ordered = fields.TryGetValue("order", out var order);
        if (ordered && AbilityLowering.Text(order!, location.Child("order")) != "playerOrder")
            throw location.Child("order").Error("expected playerOrder");
        return new(AbilityEffectLowering.LowerEffect(AbilitySelectorLowering.Required(fields, "effect", location), location.Child("effect")))
        { DefeaterOnly = restricted, InPlayerOrder = ordered };
    }
}
