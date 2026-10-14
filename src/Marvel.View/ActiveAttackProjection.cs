using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.View;

internal static class ActiveAttackProjection
{
    public static ActiveAttackDescriptor? From(World world, WorldDescriptor visible)
    {
        if (ActiveAttack.From(world) is not { } attack) return null;
        var cards = visible.Areas.SelectMany(area => area.Cards.Concat(area.Removed))
            .Where(card => card.Id is not null && card.Face is not null)
            .ToDictionary(card => card.Id!.Value);
        if (!cards.TryGetValue(attack.Source, out var source)
            || !cards.TryGetValue(attack.Target, out var target)) return null;
        return new ActiveAttackDescriptor(
            new ActivitySubject(attack.Source, source.Face!.Title, ActivitySubjectRole.Source),
            new ActivitySubject(attack.Target, target.Face!.Title, ActivitySubjectRole.Target)) { Identity = attack.Identity };
    }
}
