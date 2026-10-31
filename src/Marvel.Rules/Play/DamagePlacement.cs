using static Marvel.Rules.Play.Damage;
using static Marvel.Rules.Play.DamagePlacement;
using static Marvel.Rules.Play.DamageAttacks;
using static Marvel.Rules.Play.DamageRecovery;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Places damage and settles resulting defeat.</summary>
public static class DamagePlacement
{

    /// <summary>Resolves damage and retains dealt/taken facts across defeat.</summary>
    public static DamageResult Resolve(
        World world, ICardFacts facts, DamageRequest request, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(request);
        AttackDamageAccounting.ValidateBoostPlacement(world, request.ActivationId);
        DamageResolution? resolution = null;
        AbilityExecutionResult Apply(Occurrence cause, AbilityExecution? completion) =>
            world.Agenda.Executions.RunBatch(cause, entry =>
            {
                resolution = new DamageResolution(world, facts,
                    request with { RecordDefeatOn = cause }, entry, completion);
                resolution.Continue(events);
                return entry.Report(events);
            });
        if ((request.RecordDefeatOn ?? world.Agenda.Occurrence) is { } cause) Apply(cause, null);
        else RootDamageExecution.Run(world, request.Source, request.Target, (origin, owner) => Apply(origin, owner));
        return resolution!.Result;
    }

    /// <summary>Pay a damage cost through placement; its exact owner releases defeat after sibling costs are paid.</summary>
    public static DamageCostReceipt BeginCost(World world, ICardFacts facts, DamageRequest request,
        AbilityExecution owner, List<GameEvent> events)
    {
        world.Agenda.Executions.RequireActive(owner);
        DamageCostReceipt? receipt = null;
        var obligation = new AbilityCompletionObligation(owner);
        var captured = request with { RecordDefeatOn = request.RecordDefeatOn ?? owner.Occurrence };
        world.Agenda.Executions.RunBatch(captured.RecordDefeatOn, execution =>
        {
            receipt = new DamageCostReceipt(world, facts, captured, owner, execution, obligation);
            new DamageCostPlacement(world, facts, captured, receipt).Continue(events);
            return execution.Report(events);
        });
        return receipt!;
    }

    /// <summary>Resolve damage step 1 and return the still-imminent amount.</summary>
    public static DamageReplacement Replace(World world, DamageRequest request, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(request);
        var occurrence = new Occurrence(0, [Steps.DamageWouldBeDealt],
            Subject: request.Target.ObjectId, Player: request.Target.Owner, Target: request.Target.ObjectId,
            Actor: request.Source.ObjectId,
            ActorFacts: OccurrenceCard.Capture(request.Source, world.Facts),
            TargetFacts: OccurrenceCard.Capture(request.Target, world.Facts)) { DamageFromAttack = request.FromAttack };
        DamageReplacement? replacement = null;
        var result = world.Agenda.Executions.RunBatch(occurrence, entry =>
        {
            replacement = new DamageReplacement(request, entry);
            if (request.Amount <= 0 || !world.DamageAbilities.CanTakeDamage(world, request.Target, request.Source) || PlayerAttackPrevention.Prevents(world, request))
            {
                replacement.Replace(entry, 0);
                return entry.Finish([]);
            }
            return world.DamageAbilities.WouldBeDealt(world, entry, replacement);
        });
        ActivityBatches.Append(events, result.Events);
        return replacement!;
    }

    /// <summary>Continue damage after step 1 has fixed the amount dealt.</summary>
    internal static PlacedDamage PrepareAfterReplacement(
        World world, ICardFacts facts, Card source, Card target, long amount,
        string trigger, List<GameEvent> events)
        => PrepareAfterReplacement(world, facts, new(source, target, amount, trigger, "Damage"), events);

    internal static PlacedDamage PrepareAfterReplacement(
        World world, ICardFacts facts, DamageRequest request, List<GameEvent> events)
    {
        var (source, target, amount, trigger, _, _) = request;
        var assignment = DamageAssignment.AfterReplacement(
            amount, !request.IgnoresTough && amount > 0 && Statuses.Has(world, target, Statuses.Tough));
        if (assignment.Dealt <= 0)
        {
            return new PlacedDamage(target, 0, 0);
        }

        // `rr:tough.2`: "if a character with a tough status card would take any
        // amount of damage, **prevent all of that damage** and discard a tough
        // status card from that character instead." All of it, however much --
        // and `.2.1`, only one card per instance of damage.
        //
        // `rr:tough.2.2` is why this is checked here rather than earlier: "when
        // a hero with a tough status card defends an attack, they reduce the
        // damage from the attack by their DEF **first**. If the damage is
        // reduced to 0, the hero does not lose their tough status card." The
        // zero-dealt assignment returns before any status is discarded.
        if (assignment.SpendsTough)
        {
            world.DamageAbilities.DamagePreventedByTough(world, target, source, events);
            var tough = world.Areas
                .Where(area => area.Type == DeckType.StatusArea && area.Host == target.ObjectId)
                .SelectMany(area => area.Cards)
                .First(status => status.FaceId == Statuses.Tough);

            Discard.Card(world, tough, trigger, events);

            // `rr:tough.3`: "as a tough status card prevents damage fully, the
            // character who had the tough status card is **not considered to
            // have taken damage**." So no health event, and no defeat.
            return new PlacedDamage(target, assignment.Dealt, assignment.Taken);
        }

        // `rr:damage.step.3` and `.3.2`: modifying what the character takes
        // (including prevention) does not rewrite the amount the source dealt.
        // Step 1 has already fixed that dealt amount; only placement below uses
        // the reduced taken amount.
        assignment = assignment.AfterPrevention(
            world.DamageAbilities.WouldTake(world, target, source, assignment.Taken, events));
        if (request.PreventAllDamage) assignment = assignment.AfterPrevention(0);
        return new PlacedDamage(target, assignment.Dealt, assignment.Taken)
        {
            RemainingHealth = Math.Max(0, Health(world, facts, target) - target.Damage),
            ExcessThreat = ExcessDamageThreat.Capture(world, source),
        };
    }

    /// <summary>Place one already-fixed share of simultaneous damage at step 5.</summary>
    internal static void ApplyPlaced(
        World world, ICardFacts facts, PlacedDamage placed,
        string trigger, string verb, List<GameEvent> events)
    {
        if (!placed.Landed)
        {
            return;
        }

        long printed = Health(world, facts, placed.Target);
        long before = Math.Max(0, printed - placed.Target.Damage);
        placed.Target.TakeDamage(placed.Taken);
        long after = Math.Max(0, printed - placed.Target.Damage);
        events.Add(new FieldSet(placed.Target.ObjectId, "health", before, after)
        {
            Trigger = trigger,
            Verb = verb,
            Subjects = EffectiveCards.HasProfile(placed.Target)
                ? new Dictionary<int, string>
                { [placed.Target.ObjectId] = EffectiveCards.Title(placed.Target, facts) }
                : null,
        });
    }

    /// <summary>Resolve damage step 6 and defeat after step 5 placement.</summary>
    internal static Damage.Outcome FinishPlaced(
        World world, ICardFacts facts, Card source, PlacedDamage placed,
        string trigger, string verb, List<GameEvent> events, int by = -1,
        Occurrence? recordDefeatOn = null)
    {
        // `rr:leaves-play.1`: after a card leaves play, it is "considered to
        // be a new copy of the card." A delayed effect can make that happen
        // between damage step 5 and defeat; the old copy cannot then proceed
        // through damage steps 6 through 8.
        if (!placed.Landed || placed.Target.Incarnation != placed.Incarnation
            || !DeckTypes.IsInPlay(placed.Target.Area.Type))
        {
            return Damage.Outcome.NotDefeated;
        }

        var target = placed.Target;
        long after = Math.Max(0, Health(world, facts, target) - target.Damage);

        // `rr:damage.step.6` -- abilities that trigger "when [character]
        // would be defeated". Step 5 has placed the damage, so the condition
        // is now knowable without predicting through damage replacement or a
        // tough status card. An ability may change that imminent defeat, and
        // `rr:would.1` then makes the original condition invalid.
        if (after <= 0)
        {
            if (DefeatInterruption.Begin(world, target,
                    new DefeatCause(source.ObjectId, trigger, verb, by, recordDefeatOn), events))
                return Damage.Outcome.Suspended;
            after = Math.Max(0, Health(world, facts, target) - target.Damage);
        }

        // `rr:defeat`: "if a character has zero or fewer remaining hit points
        // [...] it is defeated". Not "less than zero" -- exactly zero is a
        // defeat, which is why this compares remaining against zero rather
        // than damage against printed.
        // The verb travels into the defeat, because `rr:defeat` says nothing
        // about what caused one and cards ask: Gene Pool answers "after an ally
        // is defeated **by anything other than consequential damage**", and
        // consequential damage is one of the verbs this is called with.
        if (after > 0)
        {
            return Damage.Outcome.NotDefeated;
        }

        return Defeat.Character(world, facts, target, trigger, events, how: verb, by: by,
            recordOn: recordDefeatOn);
    }

    /// <summary>
    /// A character's maximum hit points as the game currently counts them.
    /// </summary>
    /// <remarks>
    /// <c>rr:hit-points.2.3</c>: an ability that says a character "gets +X hit
    /// points" moves the dial, so this is the modified value and not the
    /// printed one.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="character">Whose hit points.</param>
    public static long Health(World world, ICardFacts facts, Card character)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(character);
        return CardValues.MaximumHealth(world, character, facts).CurrentValue;
    }
}
