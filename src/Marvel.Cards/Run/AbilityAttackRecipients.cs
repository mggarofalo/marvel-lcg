using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>The enemies one authored attack-damage sentence deals its damage to as attacked recipients.</summary>
/// <remarks>
/// <para>
/// Every authored attack-damage recipient passes through here, whether the
/// attack named it at initiation, declared it with several others, or reaches it
/// afterwards ("deal 1 damage to each minion", a later-chosen enemy). Ruling
/// 310af249be272877 treats each such recipient as attacked, as
/// rr:attack-player-ability-type.5 does: the attacker "is considered to have
/// attacked each of those enemies".
/// </para>
/// <para>
/// rr:cannot: "The word cannot is absolute". A recipient the attacking character
/// cannot attack is not dealt the attack's damage.
/// </para>
/// </remarks>
internal static class AbilityAttackRecipients
{
    internal static IReadOnlyList<Card> Admit(
        AbilityDamageAndThreatContext context, Card attacker, IReadOnlyList<Card> targets) =>
        [.. targets.Where(target => AttackPermissions.CanAttack(context.World, attacker, target))];

    /// <summary>Whether the attack reaches <paramref name="target"/> after its initiation window.</summary>
    /// <remarks>
    /// Such a recipient is announced by its own attack-initiated occurrence
    /// before its damage (<see cref="LateAttackRecipientOperation"/>). The
    /// recipients the initiation window offered are the attack's enemy and any it
    /// declared alongside it. Authored attack damage is always dealt within a
    /// labelled attack; outside one there is no attack to announce a recipient in.
    /// </remarks>
    internal static bool IsLate(AbilityDamageAndThreatContext context, Card target)
    {
        if (context.World.Agenda.PlayerAttackOf(context.PowerOccurrence) is not { } attack) return false;
        return attack.Enemy != target.ObjectId
            && !(attack.Continuation is AbilityPowerContinuation { DeclaredRecipients: { IsDefault: false } declared }
                && declared.Any(recipient => recipient.Card == target.ObjectId));
    }

    /// <summary>
    /// Raises for a simultaneous late recipient that a card answers being attacked.
    /// </summary>
    /// <remarks>
    /// Simultaneous placement has no per-recipient announcement. Its only late
    /// recipients are splash minions (<c>attackAnyEnemy</c>'s splash body), and
    /// every admitted "when attacked" attachment attaches to a villain, so no
    /// admitted card reaches this raise.
    /// </remarks>
    internal static void RequireNoSimultaneousLateAnswer(
        AbilityDamageAndThreatContext context, IReadOnlyList<Card> targets)
    {
        foreach (var target in targets)
            if (IsLate(context, target) && AnswersItsAttack(context, target) is { } source)
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' answers its host being attacked, and simultaneous attack damage reaches '{target.FaceId}' after the attack's initiation");
    }

    private static Card? AnswersItsAttack(AbilityDamageAndThreatContext context, Card target) =>
        context.World.Cards.FirstOrDefault(card => card.FaceUp && card.Area.Host == target.ObjectId
            && context.Program.On(card.FaceId).Any(ability => AnswersHostAttack(ability.Trigger)));

    // Only the simultaneous splash guard reads this, and splash recipients are
    // minions. Every authored attachment keyed to its host on any event (an
    // attack, damage, or an attack's end, as Distracting Taunts) attaches to a
    // villain or an identity, so the event clause changes no current outcome.
    internal static bool AnswersHostAttack(AbilityTrigger trigger) =>
        (trigger.Event == Steps.AttackInitiated || trigger.Also == Steps.AttackInitiated)
        && trigger.Target == AbilityRoles.AttachedTo;
}
