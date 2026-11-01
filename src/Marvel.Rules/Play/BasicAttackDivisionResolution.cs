using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Announces each newly attacked enemy, then deals every divided share at once.</summary>
/// <remarks>
/// rr:attack-player-ability-type.5: the attacker "is considered to have attacked
/// each of those enemies". An enemy other than the declared target is announced by
/// its own attack-initiated occurrence before damage (rr:interrupt.3), as for any
/// enemy a begun player attack reaches after its initiation.
/// </remarks>
internal sealed class BasicAttackDivisionResolution
{
    private readonly World world;
    private readonly ICardFacts facts;
    private readonly CharacterAttack attack;
    private readonly Card performer;
    private readonly (Card Card, long Amount)[] shares;
    private readonly bool overkill;
    private bool announced;
    private SimultaneousAttackDamageResult? damage;

    private BasicAttackDivisionResolution(World world, ICardFacts facts, CharacterAttack attack, Card performer,
        IReadOnlyList<(Card Card, long Amount)> shares, Occurrence occurrence, AbilityExecution execution)
    {
        this.world = world;
        this.facts = facts;
        this.attack = attack;
        this.performer = performer;
        this.shares = [.. shares];
        overkill = occurrence.BasicUse?.Overkill == true;
        Execution = execution;
    }

    internal AbilityExecution Execution { get; }

    internal static void Begin(World world, ICardFacts facts, CharacterAttack attack, Card performer,
        IReadOnlyList<(Card Card, long Amount)> shares, Occurrence occurrence, List<GameEvent> events)
    {
        occurrence.Retaliation = new(performer.ObjectId, performer.Incarnation,
            AttackKeywords.Has(world, performer, performer, Keywords.Ranged, basic: true),
            shares.Select(share => new RetaliationRecipient(Attacked.Capture(share.Card, facts), share.Card.Area.Id)),
            attack.Trigger);
        world.Agenda.Executions.RunBatch(occurrence, entry =>
        {
            new BasicAttackDivisionResolution(world, facts, attack, performer, shares, occurrence, entry).Continue(events);
            return entry.Report(events);
        });
    }

    private void Continue(List<GameEvent> events)
    {
        world.Agenda.Executions.RequireActive(Execution);
        if (!announced)
        {
            announced = true;
            if (AnnounceLateRecipients()) { Suspend(events); return; }
        }
        // A recipient that left during the announcements receives nothing; its
        // retained incarnation keeps a new copy from receiving its share.
        damage ??= SimultaneousAttackDamage.Resolve(world, facts,
            new(performer, performer, [.. shares.Select(share => share.Card)], 0, attack.Trigger, BasicPowers.AttackVerb)
            {
                Retaliate = false,
                Amounts = [.. shares.Select(share => share.Amount)],
                Basic = true,
                GrantedOverkill = overkill,
            }, events);
        if (!damage.IsComplete || world.IsOver) { Suspend(events); return; }
        CharacterAttackEnd.Complete(world, Execution.Occurrence, events);
        Execution.Occurrence.Also(Steps.BasicAttackEnds);
        Execution.Finish(events);
    }

    private bool AnnounceLateRecipients()
    {
        bool any = false;
        foreach (var (card, _) in shares.Where(share => share.Card.ObjectId != attack.Enemy))
        {
            world.Agenda.Then(new PhaseStep(
                new LateAttackRecipientOperation(performer.ObjectId, card.ObjectId, attack.Player),
                world.Agenda.Current?.Round ?? 0, 2, Index: attack.Player, Subject: card.ObjectId, Seat: attack.Player));
            any = true;
        }
        return any;
    }

    private void Suspend(List<GameEvent> events)
    {
        world.Agenda.ThenContinuation(new PhaseStep(new BasicAttackDivisionOperation(this),
            world.Agenda.Current?.Round ?? 0, 5, Plan: true), Execution.Occurrence);
        Execution.Yield(events);
    }

    internal static void Resume(World world, PhaseStep step, List<GameEvent> events)
    {
        var resolution = step.Require<BasicAttackDivisionOperation>().Resolution;
        world.Agenda.Executions.RequireContinuation(step);
        world.Agenda.Executions.Resume(resolution.Execution);
        resolution.Continue(events);
    }
}
