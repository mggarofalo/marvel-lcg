using Marvel.Rules.State;
using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;

namespace Marvel.Cards.Dsl;

/// <summary>Compiles the fixed kinds and selections of status operations.</summary>
internal static class AbilityStatusLowering
{
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
