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
/// <para>
/// An enemy the attack reaches only after its initiation window has passed was
/// never offered to an ability that answers "when this card is attacked". The
/// engine opens no window for it, so such a recipient raises rather than
/// letting that ability silently miss its trigger.
/// </para>
/// </remarks>
internal static class AbilityAttackRecipients
{
    internal static IReadOnlyList<Card> Admit(
        AbilityDamageAndThreatContext context, Card attacker, IReadOnlyList<Card> targets)
    {
        var permitted = targets.Where(target => AttackPermissions.CanAttack(context.World, attacker, target)).ToArray();
        var initiated = Initiated(context);
        foreach (var target in permitted)
            if (!initiated.Contains(target.ObjectId) && AnswersItsAttack(context, target) is { } source)
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' answers its card being attacked, and this attack reaches '{target.FaceId}' after its initiation window");
        return permitted;
    }

    // The recipients the initiation window offered: the attack's enemy and any
    // recipients it declared alongside it.
    private static HashSet<int> Initiated(AbilityDamageAndThreatContext context)
    {
        if (context.World.Agenda.PlayerAttackOf(context.PowerOccurrence) is not { } attack) return [];
        HashSet<int> initiated = [attack.Enemy];
        if (attack.Continuation is AbilityPowerContinuation { DeclaredRecipients: { IsDefault: false } declared })
            initiated.UnionWith(declared.Select(recipient => recipient.Card));
        return initiated;
    }

    private static Card? AnswersItsAttack(AbilityDamageAndThreatContext context, Card target) =>
        context.World.Cards.FirstOrDefault(card => card.FaceUp
            && context.Program.On(card.FaceId).Any(ability => AnswersAttackOn(ability.Trigger, card, target)));

    // Temporal Shield's "When Kang is attacked" is the only such trigger the
    // book holds: an attachment keyed to its host.
    private static bool AnswersAttackOn(AbilityTrigger trigger, Card card, Card target) =>
        (trigger.Event == Steps.AttackInitiated || trigger.Also == Steps.AttackInitiated)
        && trigger.Target == AbilityRoles.AttachedTo && card.Area.Host == target.ObjectId;
}
