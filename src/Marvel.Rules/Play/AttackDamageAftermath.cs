using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Attack roles and excess damage retained across a target's defeat decision.</summary>
internal sealed record AttackDamageAftermath(
    Card Attacker, Card Source, Card Target, CardKind TargetKind,
    int ControllingPlayer, long Excess, bool Overkill, bool CanRetaliate, string Trigger)
{
    internal void ResolveOverkill(World world, ICardFacts facts,
        Damage.Outcome outcome, List<GameEvent> events)
    {
        if (outcome == Damage.Outcome.Defeated && Overkill)
            Spill(world, facts, events);
    }

    internal void FinishOrSuspend(World world, ICardFacts facts,
        Damage.Outcome outcome, List<GameEvent> events)
    {
        if (outcome == Damage.Outcome.Suspended)
        {
            world.Agenda.Then(new PhaseStep(
                Steps.FinishAttackDamage,
                world.Agenda.Current?.Round ?? 0,
                5,
                Subject: Target.ObjectId,
                Seat: ControllingPlayer,
                Plan: true,
                Character: Attacker.ObjectId,
                ProcedureSource: Source.ObjectId,
                ProcedureTrigger: Trigger,
                ProcedureVerb: TargetKind.ToString(),
                ProcedureAmount: Excess,
                ProcedureFlag: Overkill,
                FinalStep: CanRetaliate));
        }
        else if (CanRetaliate)
            DamageAttacks.Retaliate(world, facts, Target, Attacker, Trigger, events);
    }

    internal static void Resume(World world, ICardFacts facts,
        PhaseStep step, List<GameEvent> events)
    {
        var target = world.Cards[step.Subject];
        var attacker = world.Cards[step.Character];
        var source = world.Cards[step.ProcedureSource];
        bool defeated = !DeckTypes.IsInPlay(target.Area.Type);
        if (defeated && step.ProcedureFlag && step.ProcedureAmount > 0)
        {
            var aftermath = new AttackDamageAftermath(attacker, source, target,
                Enum.Parse<CardKind>(step.ProcedureVerb, ignoreCase: false),
                step.Seat, step.ProcedureAmount, step.ProcedureFlag,
                step.FinalStep, step.ProcedureTrigger);
            aftermath.Spill(world, facts, events);
        }
        if (step.FinalStep)
            DamageAttacks.Retaliate(world, facts, target, attacker, step.ProcedureTrigger, events);
    }

    /// <summary>Excess damage goes to the defeated ally's controller or the villain.</summary>
    private void Spill(World world, ICardFacts facts, List<GameEvent> events)
    {
        // rr:overkill.1 sends damage beyond an ally's hit points to "the identity
        // of the player who controls the ally"; a defeated minion's excess goes
        // to "the villain". Control and kind are captured before defeat.
        var onto = TargetKind switch
        {
            CardKind.Ally when ControllingPlayer >= 0 =>
                world.Seats[ControllingPlayer].IdentityCard,
            CardKind.Minion => world.TheCardIn(DeckType.VillainArea),
            _ => null,
        };
        if (onto is not null)
        {
            // rr:overkill.2: this is "damage from an attack" but "does not
            // constitute an attack against that character", so it does not retaliate.
            DamagePlacement.DealWithAmounts(
                world, facts, Source, onto, Excess, Trigger, Keywords.Overkill, events,
                out long dealt, out _);
            AttackDamageAccounting.RecordRecipient(world, Source, onto, dealt);
        }
    }
}
