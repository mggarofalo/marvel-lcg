using Marvel.Rules.Events;
using static Marvel.View.EventSubjectDescriptions;

namespace Marvel.View;

/// <summary>Names the established result of an authorized completed attack.</summary>
internal static class AttackCompletionPresentation
{
    internal static string Summary(AttackCompleted completed, WorldDescriptor world) =>
        $"{Card(completed.Enemy, world, completed)}'s attack on {Card(completed.Target, world, completed)} ended. "
        + (completed.Defender >= 0
            ? $"{Card(completed.Defender, world, completed)} defended."
            : "The attack was undefended.")
        + (completed.DamageDealt is { } damage
            ? $" The attack dealt {damage.ToString(System.Globalization.CultureInfo.InvariantCulture)} damage."
            : "");

    internal static int[] Anchors(AttackCompleted completed) =>
        new[] { completed.Enemy, completed.Target, completed.Defender }
            .Where(id => id >= 0).Distinct().ToArray();
}
