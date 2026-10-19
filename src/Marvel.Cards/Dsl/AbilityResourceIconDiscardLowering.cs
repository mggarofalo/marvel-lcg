using static Marvel.Cards.Dsl.AbilityLowering;

namespace Marvel.Cards.Dsl;

internal static class AbilityResourceIconDiscardLowering
{
    internal static AbilityEffect.DiscardResourceIcons Lower(AbilityValue value, AbilityLocation location)
    {
        long count = Integer(value, location);
        if (count is < 1 or > 2) throw location.Error("discardResourceIcons supports one or two printed icons");
        return new((int)count);
    }
}
