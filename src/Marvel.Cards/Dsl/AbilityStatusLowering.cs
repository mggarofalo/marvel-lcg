using Marvel.Rules.State;
using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;

namespace Marvel.Cards.Dsl;

/// <summary>Compiles the fixed kinds and selections of status operations.</summary>
internal static class AbilityStatusLowering
{
    internal static AbilityEffect.ChooseStatusRecipients Recipients(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "from", "status", "maximum", "count");
        string status = Text(Required(fields, "status", location), location.Child("status"));
        if (status is not (Statuses.Tough or Statuses.Stunned or Statuses.Confused))
            throw location.Child("status").Error($"'{status}' is not a supported status");
        if (fields.TryGetValue("count", out var count))
        {
            if (fields.ContainsKey("maximum")) throw location.Error("give either 'maximum' or 'count'");
            return new(Selected(fields, "from", location), status, 0, Number(count, location.Child("count")));
        }
        return new(Selected(fields, "from", location), status,
            AbilityCostLowering.PositiveCount(Required(fields, "maximum", location), location.Child("maximum")));
    }

    internal static AbilityRecipientEffect.ChooseHealRecipients HealRecipients(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "from", "amount", "maximum");
        return new(Selected(fields, "from", location),
            AbilityCostLowering.PositiveCount(Required(fields, "amount", location), location.Child("amount")),
            AbilityCostLowering.PositiveCount(Required(fields, "maximum", location), location.Child("maximum")));
    }

    internal static AbilityEffect.ReplaceStatus Replacement(AbilityValue value, AbilityLocation location)
    {
        var status = Given(value, location);
        if (status.Cards is not AbilityCardSelection.Bound)
            throw location.Error("replacement requires a single bound physical status card");
        return new(status.Cards, status.Status);
    }

    internal static AbilityEffect.GiveStatus Given(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "card", "status");
        string status = Text(Required(fields, "status", location), location.Child("status"));
        if (status is not (Statuses.Tough or Statuses.Stunned or Statuses.Confused))
        {
            throw location.Child("status").Error($"'{status}' is not a supported status");
        }
        return new(Selected(fields, "card", location), status);
    }

}
