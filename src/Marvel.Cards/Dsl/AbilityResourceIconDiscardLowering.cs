using static Marvel.Cards.Dsl.AbilityLowering;
using Marvel.Rules.Play;

namespace Marvel.Cards.Dsl;

internal static class AbilityResourceIconDiscardLowering
{
    internal static AbilityEffect.DiscardResourceIcons Lower(AbilityValue value, AbilityLocation location)
    {
        string resources = "";
        if (value is AbilityValue.Map map && map.Entry("count") is { } count)
        {
            if (map.Entries.Keys.Any(key => key is not ("count" or "resources")))
                throw location.Error("expected count and optional resources");
            if (map.Entry("resources") is { } types)
            {
                resources = Text(types, location.Child("resources"));
                if (resources.Length == 0 || resources.Distinct().Count() != resources.Length
                    || resources.Any(type => !Resources.Types.Contains(type)))
                    throw location.Error("expected distinct supported resource symbols");
            }
            value = count;
        }
        var number = Number(value, location.Child("count"));
        if (number is AbilityNumber.Constant { Value: < 0 })
            throw location.Error("discardResourceIcons requires a nonnegative count");
        return new(number) { Resources = resources };
    }
}
