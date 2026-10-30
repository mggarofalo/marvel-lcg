using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal static class AbilityTriggerRoleMatching
{
    internal static bool Matches(
        World world, AbilityWindowAdmission.Candidate candidate, Occurrence occurrence, int? restricted)
    {
        var (card, ability, _, _) = candidate;
        return Subject(world, ability.Trigger.Subject, card, occurrence, restricted, candidate.TriggeringDefeat)
            && (ability.Trigger.Actor != AbilityRoles.You || occurrence.ActorFacts?.Kind is CardKind.Hero or CardKind.AlterEgo)
            && Role(world, ability.Trigger.Actor, card, occurrence.ActorFacts, restricted)
            && Role(world, ability.Trigger.Target, card, candidate.TriggeringAttack?.Facts ?? occurrence.TargetFacts, restricted)
            && Player(world, ability.Trigger.Player, card, occurrence, restricted);
    }

    private static bool Subject(
        World world, string? subject, Card card, Occurrence occurrence,
        int? restricted, Defeated? defeat) => subject switch
        {
            null => true,
            AbilitySubjects.This => occurrence.Is(ResourceSpentWindow.Condition)
                ? occurrence.SpentResourceCards.Any(spent => spent.Card == card.ObjectId
                    && spent.Incarnation == card.Incarnation)
                : (defeat?.Card ?? occurrence.Subject) == card.ObjectId,
            AbilitySubjects.AttachedTo =>
                card.Area.Host >= 0 && (defeat?.Card ?? occurrence.Subject) == card.Area.Host,
            AbilitySubjects.You => occurrence.Player >= 0
                && occurrence.Player
                    == (restricted ?? AbilityCardQueries.ControllerOf(world, card)),
            AbilitySubjects.Game => true,
            _ => throw new AbilityException(
                $"'{subject}' is not a subject anything matches"),
        };

    private static bool Role(
        World world, string? match, Card card, OccurrenceCard? role,
        int? restricted) => match switch
        {
            null => true,
            _ when role is null => false,
            AbilityRoles.This => role.Card == card.ObjectId,
            AbilityRoles.AttachedTo =>
                card.Area.Host >= 0 && role.Card == card.Area.Host,
            AbilityRoles.You => role.Controller >= 0
                && (restricted is { } player
                    ? role.Controller == player
                    : card.Owner == World.Scenario
                        || role.Controller == AbilityCardQueries.ControllerOf(world, card)),
            AbilityRoles.Villain => role.IsVillain,
            AbilityRoles.Minion => role.IsMinion,
            AbilityRoles.Hero => role.IsHero,
            AbilityRoles.Ally => role.IsAlly,
            AbilityRoles.Friendly => role.IsFriendly,
            AbilityRoles.Enemy => role.IsEnemy,
            _ => throw new AbilityException(
                $"'{match}' is not an occurrence role matcher"),
        };

    private static bool Player(
        World world, string? match, Card card, Occurrence occurrence,
        int? restricted) => match switch
        {
            null or AbilityPlayers.TriggerPlayer => true,
            AbilityPlayers.You when occurrence.Is(ResourceSpentWindow.Condition) =>
                occurrence.SpentResourceCards.Any(spent => spent.Card == card.ObjectId
                    && spent.Player == (restricted ?? AbilityCardQueries.ControllerOf(world, card))),
            AbilityPlayers.You => occurrence.Player >= 0
                && occurrence.Player
                    == (restricted ?? AbilityCardQueries.ControllerOf(world, card)),
            _ => throw new AbilityException(
                $"'{match}' is not an occurrence player matcher"),
        };
}
