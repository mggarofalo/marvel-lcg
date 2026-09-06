using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private static void Choose(AbilityEffect node, Cast cast)
    {
        if (node.OperationName() == "choose" && ((AbilityEffect.Choose)node).Options.Length < 2)
        {
            throw new AbilityException(
                $"'{cast.Source.FaceId}' offers a choice of one, which is not a choice");
        }

        if (node.OperationName() == "choose"
            && !((AbilityEffect.Choose)node).Options.Any(option => OptionIsLegal(option, cast)))
        {
            // rr:target.2 and rr:choose-option.1: a mandatory encounter-card
            // ability with no valid option cannot initiate. Reaching that
            // instruction directly during reveal or boost resolution is a
            // no-effect resolution, not a question with an invented answer.
            if (!IsPlayerCard(cast)
                && cast.Tier is { } tier
                && AbilityTypes.IsMandatory(tier))
            {
                return;
            }
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' requires a choice and has no legal option");
        }

        if (node.OperationName() == "chooseCard"
            && LegalCardChoicesForContinuation(node, cast).Count == 0)
        {
            // A mandatory ability with no valid chosen target cannot initiate;
            // reaching it directly from reveal or boost resolution is a no-op.
            // Optional and action paths reject it during their preflight.
            return;
        }

        SuspendForChoice(node, cast);
    }

    /// <summary>Suspend an ability for one persisted player choice.</summary>
    private static void SuspendForChoice(AbilityEffect node, Cast cast)
    {
        // `Index` remains the legacy top-level resume point. New continuations
        // use AbilityOrdinal and AbilityPath below.
        int abilityOrdinal = AbilityOrdinal(node, cast);
        var abilityResults = ContinuationResults(cast, abilityOrdinal);
        var continuation = new PhaseStep(
            Steps.ChooseOption,
            cast.World.Agenda.Current?.Round ?? 0,
            2,
            Index: cast.Position + 1,
            Subject: cast.Source.ObjectId,
            Seat: cast.Player,

            // Which ability stopped. A card can have a choice in two of them,
            // and the card and the position do not say which -- see `Choice`.
            Tier: cast.Tier,
            FinalStep: cast.FinalStep,
            FinalPlayer: cast.FinalPlayer,
            EachPlayerFrame: cast.EachPlayerFrame,
            Trigger: cast.Trigger,
            SurgeGained: cast.GainedKeywords.Contains("surge"),
            Discarded: [.. cast.Discarded.Select(card => card.ObjectId)],
            AbilityOrdinal: abilityOrdinal,
            AbilityPath: [.. cast.AbilityPath],
            AbilityResults: abilityResults,
            AbilityOccurrence: cast.Occurrence,
            AbilityFace: cast.AbilityFace,
            AbilityPlayer: cast.AbilityPlayer,
            AbilityActor: cast.AbilityActor?.ObjectId ?? -1,
            AbilityHasContinuation: cast.HasContinuation);
        if (cast.Occurrence.Is(Steps.TurnAction))
        {
            cast.World.Agenda.ThenContinuation(continuation, cast.Occurrence);
        }
        else
        {
            cast.World.Agenda.Then(continuation);
        }

        cast.Suspend();
    }

    /// <summary>The characters indirect damage may be assigned to.</summary>
    /// <remarks>
    /// <c>rr:indirect-damage.4</c>: "characters that cannot take damage cannot
    /// be assigned indirect damage", and <c>.3.1</c> makes a character with no
    /// hit points left ineligible for the same reason — there is no amount that
    /// would not defeat it.
    /// </remarks>
    private static List<Card> Assignable(AbilityCardSelection among, Cast cast) =>
        AbilityDamageAndThreatExecution.Assignable(among, DamageAndThreatContext(cast));

    private static IReadOnlyList<Card> DamageTargets(AbilityCardSelection targets, Cast cast) =>
        AbilityDamageAndThreatExecution.DamageTargets(targets, DamageAndThreatContext(cast));

    /// <summary>How much indirect damage one character may be assigned.</summary>
    private static long Room(Cast cast, Card card) =>
        AbilityDamageAndThreatExecution.Room(card, DamageAndThreatContext(cast));

    /// <summary>Deals an assignment that is already worked out.</summary>
    /// <remarks>
    /// In object-id order, because <c>rr:indirect-damage.3</c> resolves it
    /// "simultaneously" and simultaneous still has to reach the event stream in
    /// some order — one the board cannot see and the wire can.
    /// </remarks>
    private static void Resolve(
        AbilityEffect node, Cast cast, Dictionary<int, long> assigned)
        => ApplyDamageAndThreat(
            AbilityDamageAndThreatExecution.ResolveAssigned(
                node, assigned, DamageAndThreatContext(cast)),
            node, cast);

    /// <summary>"Deal N damage to …" — <c>rr:damage</c>.</summary>
    /// <remarks>
    /// Through <see cref="Damage.Deal"/> and not at the token, because damage
    /// is one rule however it arrived: <c>rr:tough.2</c> prevents all of it and
    /// discards a status card instead, and <c>rr:defeat</c> is the other half
    /// of the same moment. A card that wrote to <c>k_damage</c> would skip
    /// both and leave a defeated character standing.
    /// </remarks>
    private static void DealDamage(AbilityEffect.Damage damage, AbilityEffect node, Cast cast, long multiplier = 1)
        => ApplyDamageAndThreat(
            AbilityDamageAndThreatExecution.DealDamage(
                damage, node, DamageAndThreatContext(cast), multiplier),
            node, cast);

    private void SchedulePower(AbilityEffect node, Cast cast, string power)
    {
        var target = ResolveCard(EffectOf<AbilityEffect.Power>(node, cast).Target!, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot find the target of its {power}");
        SchedulePower(node, cast, power, target, [target], -1);
    }

    private void SchedulePower(
        AbilityEffect node, Cast cast, string power, Card target,
        IReadOnlyList<Card> targets, long powerAmount)
    {
        var effect = EffectBody(node);
        var continuationChosen = cast.CaptureCurrentSelection();
        cast.Choose(target);
        if (SuspendsPowerEffect(
            effect, cast, bindingMayChange: powerAmount >= 0))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' suspends inside a {power.ToLowerInvariant()}, "
                + "which is not implemented");
        }

        var abilities = AbilitiesOn(cast.Source, cast.AbilityFace).ToList();
        var addresses = abilities
            .Select((ability, index) => (Ability: ability, Index: index))
            .Where(candidate => cast.Tier is null
                || candidate.Ability.Trigger.Timing == cast.Tier)
            .SelectMany(candidate => PowerNodes(candidate.Ability.Effect, power)
                .Select((wrapper, ordinal) =>
                    (candidate.Index, Ordinal: ordinal, Wrapper: wrapper)))
            .Where(candidate => ReferenceEquals(candidate.Wrapper, node))
            .ToList();
        if (addresses.Count != 1)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' {power.ToLowerInvariant()} has {addresses.Count} "
                + "reconstructable authored locations");
        }

        var address = addresses[0];
        int resumeFrom = cast.HasContinuation ? cast.Position + 1 : -1;
        IReadOnlyList<string> abilityPath = [.. cast.AbilityPath];
        var abilityResults = ContinuationResults(cast, abilities[address.Index]);
        if (continuationChosen is null)
        {
            abilityResults.Remove(PersistedChosen);
            abilityResults.Remove(PersistedChosenArea);
            abilityResults.Remove(PersistedChosenIncarnation);
        }
        else
        {
            PersistChosen(continuationChosen, abilityResults);
        }
        var discarded = cast.Discarded.Select(card => card.ObjectId).ToList();
        bool automaticThwartTarget = EffectOf<AbilityEffect.Power>(node, cast).AutomaticTarget
            || cast.CrisisIgnoringThwartWasValidated(node, address.Ordinal);
        bool scheduled = power == BasicPowers.AttackVerb
            ? BasicPowers.CardAttack(
                cast.World, cast.World.Facts, Resolver(cast), cast.Source, target, powerAmount,
                cast.Trigger, cast.Events, abilityIndex: address.Index,
                powerOrdinal: address.Ordinal, resumeFrom: resumeFrom,
                finalStep: cast.FinalStep,
                targets: [.. targets.Select(card => card.ObjectId)], nested: true,
                surgeGained: cast.GainedKeywords.Contains("surge"),
                abilityPath: abilityPath, abilityFace: cast.AbilityFace,
                abilityResults: abilityResults, abilityOccurrence: cast.Occurrence,
                discarded: discarded, eachPlayerFrame: cast.EachPlayerFrame,
                finalPlayer: cast.FinalPlayer, abilityPlayer: cast.AbilityPlayer,
                abilityHasContinuation: cast.HasContinuation,
                performer: cast.AbilityActor)
            : BasicPowers.CardThwart(
                cast.World, cast.World.Facts, Resolver(cast), cast.Source, target, powerAmount,
                cast.Trigger, cast.Events, abilityIndex: address.Index,
                powerOrdinal: address.Ordinal, resumeFrom: resumeFrom,
                finalStep: cast.FinalStep,
                targets: [.. targets.Select(card => card.ObjectId)],
                imminentThreat: cast.Occurrence.Threat,
                automaticTarget: automaticThwartTarget,
                nested: true,
                surgeGained: cast.GainedKeywords.Contains("surge"),
                abilityPath: abilityPath, abilityFace: cast.AbilityFace,
                abilityResults: abilityResults, abilityOccurrence: cast.Occurrence,
                discarded: discarded, eachPlayerFrame: cast.EachPlayerFrame,
                finalPlayer: cast.FinalPlayer, abilityPlayer: cast.AbilityPlayer,
                abilityHasContinuation: cast.HasContinuation,
                performer: cast.AbilityActor);
        if (!scheduled)
        {
            return;
        }

        cast.Suspend();
    }

    private static void RemoveThreat(AbilityEffect.RemoveThreat removal, Cast cast, long multiplier = 1)
        => ApplyDamageAndThreat(
            AbilityDamageAndThreatExecution.RemoveThreat(
                removal, DamageAndThreatContext(cast), multiplier),
            removal, cast);

    private static long EventModifier(Cast cast, string kind) =>
        AbilityEventModifiers.Amount(cast.World, cast.Source, kind);

    private static IReadOnlyList<ContinuousEffect> EventModifierEffects(
        Cast cast, string kind)
        => AbilityEventModifiers.Effects(cast.World, cast.Source, kind);

    /// <summary>Which card type a word names.</summary>
    private static CardKind Kind(string named) => named switch
    {
        "sideScheme" => CardKind.EncounterSideScheme,
        "minion" => CardKind.Minion,
        "ally" => CardKind.Ally,
        "upgrade" => CardKind.Upgrade,
        "treachery" => CardKind.Treachery,
        _ => throw new RulesNotImplementedException(
            $"'{named}' is not a card type this engine can name"),
    };


    /// <summary>
    /// "The villain attacks you", "the villain schemes" — an enemy activation
    /// a card asked for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Scheduled, not called.</b> <c>rr:attack-enemy-activation</c> is six
    /// steps and one of them asks a player who is defending, so an activation
    /// cannot resolve inside an ability that has to return. It goes on the
    /// agenda, and <c>Agenda.Then</c> puts it after the step that is running —
    /// which is what <c>rr:surge.2</c> wants anyway: finish resolving the card
    /// before what it caused happens.
    /// </para>
    /// <para>
    /// <b>Which activation is the card's to say.</b> <c>rr:activation.1</c>
    /// reads it off the player's form — attack in hero form, scheme in
    /// alter-ego form — but that rule is about the activation the villain phase
    /// schedules. A card that says "the villain attacks you" has already
    /// chosen, and reading the form here would make Assault do nothing to a
    /// hero who had flipped since the card was dealt.
    /// </para>
    /// <para>
    /// An activation collection suspends for an ordered target request whenever
    /// a read finds several eligible enemies. A dynamic collection re-reads
    /// after that ordered batch, excluding enemies it has already processed.
    /// </para>
    /// </remarks>
    private static void Activate(
        AbilityEffect.ActivateEnemies instruction, AbilityEffect node, Cast cast)
    {
        // The round the activation belongs to is the round the card was
        // revealed in. Nothing else on the agenda can tell it.
        int round = cast.World.Agenda.Current?.Round ?? 0;

        // "Speed Demon attacks **that character**." Absent on every card that
        // simply says "the villain attacks you", which is the case
        // `rr:attack-enemy-activation.1.1` calls normal: "the attacked
        // character is the player's hero". An ability naming one instead is
        // the exception the same clause allows.
        var namedTarget = instruction.Against;
        bool engagedHero = instruction.EngagedHero;
        int against = namedTarget is { } named
            ? ResolveCard(named, cast)?.ObjectId ?? -1
            : -1;

        // An ordinary "attacks you" activation belongs to the player
        // resolving the card. An attack against a named occurrence role gets
        // its attacked player from that role's snapshot instead. Speed Demon's
        // target can move or change control during this interrupt, but that
        // must not rewrite who was behind the character that attacked it.
        int seat = namedTarget switch
        {
            AbilityCardSelection.Bound { Binding: AbilityCardBinding.TriggerActor } =>
                cast.Occurrence.ActorFacts?.Controller ?? World.Scenario,
            AbilityCardSelection.Bound { Binding: AbilityCardBinding.TriggerTarget } =>
                cast.Occurrence.TargetFacts?.Controller ?? World.Scenario,
            null => cast.Player,
            _ => cast.Player,
        };

        if (seat < 0)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' initiates an enemy attack against a character "
                + "with no attacked player");
        }

        // "**(Resolve Speed Demon's attack first.)**" -- the card prints the
        // instruction, so the data records it. Absent, an activation a card
        // causes goes after whatever is happening, which is `rr:activation.8`:
        // "an activation initiated during another resolves after the current
        // activation has finished resolving." An interrupt that means to get
        // in front of the thing it answers has to say so, and Speed Demon's
        // parenthesis is the card saying it.
        bool first = instruction.First;

        bool dynamic = instruction.Dynamic;
        var enemies = ActivationCandidates(instruction, ResolveCards(instruction.Enemies, cast), cast).ToList();
        bool ordered = cast.Results.Remove("dynamicActivationOrderSet");
        if (enemies.Count > 1 && !ordered)
        {
            SuspendForChoice(node, cast);
            return;
        }
        if (ordered)
        {
            enemies = enemies
                .OrderBy(enemy => cast.Results.GetValueOrDefault(
                    $"dynamicActivationOrder:{enemy.ObjectId}", long.MaxValue))
                .ToList();
            foreach (var enemy in enemies)
            {
                cast.Results.Remove($"dynamicActivationOrder:{enemy.ObjectId}");
            }
        }
        var activations = new List<PhaseStep>();
        foreach (var enemy in enemies)
        {
            int activationSeat = engagedHero ? enemy.Area.PlayArea.Player : seat;
            if (activationSeat < 0
                || (engagedHero && !Forms.In(
                    cast.World,
                    cast.World.Seats[activationSeat],
                    cast.World.Facts,
                    Forms.Hero)))
            {
                continue;
            }

            activations.Add(new PhaseStep(
                instruction.Attack ? Steps.Attack : Steps.Scheme,
                round, 2, Index: activationSeat, Subject: enemy.ObjectId,
                Seat: activationSeat, Character: against));
        }

        var activationIds = new List<int>();
        foreach (var activation in activations)
        {
            if (dynamic)
            {
                cast.Results[$"dynamicActivation:{activation.Subject}"] = 1;
            }
            if (first)
            {
                activationIds.Add(cast.World.Agenda.NowActivation(activation));
            }
            else
            {
                activationIds.Add(cast.World.Agenda.ThenActivation(activation));
            }
        }

        if (activationIds.Count > 0)
        {
            if (dynamic)
            {
                cast.Results["repeatDynamicActivation"] = 1;
            }
            int abilityOrdinal = AbilityOrdinal(node, cast);
            cast.World.Agenda.AfterActivations(activationIds, new PhaseStep(
                Steps.ResumeAbility,
                round,
                2,
                Index: cast.Position + 1,
                Subject: cast.Source.ObjectId,
                Seat: cast.Player,
                Tier: cast.Tier,
                FinalStep: cast.FinalStep,
                FinalPlayer: cast.FinalPlayer,
                EachPlayerFrame: cast.EachPlayerFrame,
                Trigger: cast.Trigger,
                SurgeGained: cast.GainedKeywords.Contains("surge"),
                Discarded: [.. cast.Discarded.Select(card => card.ObjectId)],
                AbilityOrdinal: abilityOrdinal,
                AbilityPath: [.. cast.AbilityPath],
                AbilityResults: ActivationResults(cast, abilityOrdinal),
                AbilityOccurrence: cast.Occurrence,
                AbilityFace: cast.AbilityFace,
                AbilityPlayer: cast.AbilityPlayer,
                AbilityActor: cast.AbilityActor?.ObjectId ?? -1,
                AbilityHasContinuation: cast.HasContinuation));
            cast.WaitFor(activationIds);
            cast.Suspend();
        }
        else if (dynamic)
        {
            cast.Results["activationMade"] =
                cast.Results.GetValueOrDefault("dynamicActivationMade");
        }
    }

    private static AbilityEffect.ActivateEnemies ActivationOf(AbilityEffect node, Cast cast) =>
        (AbilityEffect.ActivateEnemies)node;

    private static IReadOnlyList<Card> ActivationCandidates(
        AbilityEffect.ActivateEnemies instruction, Cast cast) =>
        ActivationCandidates(instruction, Every(instruction.Enemies, cast), cast);

    private static IReadOnlyList<Card> ActivationCandidates(
        AbilityEffect.ActivateEnemies instruction, IReadOnlyList<Card> enemies, Cast cast) =>
        [.. enemies.Where(enemy => !instruction.Dynamic
            || cast.Results.GetValueOrDefault($"dynamicActivation:{enemy.ObjectId}") == 0)];

    private static Dictionary<string, long> ActivationResults(
        Cast cast, int abilityOrdinal)
    {
        var results = ContinuationResults(cast, abilityOrdinal);
        results.Remove("activationMade");
        results.Remove("activationDamage");
        results.Remove("activationThreat");
        return results;
    }

    /// <summary>Gameplay results plus engine-owned state needed after suspension.</summary>
    private static Dictionary<string, long> ContinuationResults(
        Cast cast, int abilityOrdinal)
    {
        var results = new Dictionary<string, long>(cast.Results, StringComparer.Ordinal);
        if (cast.Abilities is AbilityRunner runner)
        {
            cast.PersistCrisisIgnoringThwarts(
                runner.AbilityAt(
                    cast.Source, cast.Tier, abilityOrdinal, cast.AbilityFace),
                results);
        }
        PersistSource(cast, results);
        PersistChosen(cast, results);
        return results;
    }

    /// <summary>Resume the containing ability after a rules procedure finishes.</summary>
    private static void SuspendAfterProcedure(
        AbilityEffect node, Cast cast, PhaseStep? agendaOwner = null,
        Occurrence? agendaOccurrence = null)
    {
        int abilityOrdinal = AbilityOrdinal(node, cast);
        var results = ContinuationResults(cast, abilityOrdinal);
        results["procedureApplied"] = 1;
        var continuation = new PhaseStep(
            Steps.ResumeAbility,
            cast.World.Agenda.Current?.Round ?? 0,
            2,
            Index: cast.Position + 1,
            Subject: cast.Source.ObjectId,
            Seat: cast.Player,
            Plan: true,
            Tier: cast.Tier,
            FinalStep: cast.FinalStep,
            FinalPlayer: cast.FinalPlayer,
            EachPlayerFrame: cast.EachPlayerFrame,
            Trigger: cast.Trigger,
            SurgeGained: cast.GainedKeywords.Contains("surge"),
            Discarded: [.. cast.Discarded.Select(card => card.ObjectId)],
            AbilityOrdinal: abilityOrdinal,
            AbilityPath: [.. cast.AbilityPath],
            AbilityResults: results,
            AbilityOccurrence: cast.Occurrence,
            AbilityFace: cast.AbilityFace,
            AbilityPlayer: cast.AbilityPlayer,
            AbilityActor: cast.AbilityActor?.ObjectId ?? -1,
            AbilityHasContinuation: cast.HasContinuation);
        if (agendaOwner is null)
        {
            cast.World.Agenda.Then(continuation);
        }
        else
        {
            cast.World.Agenda.ContinueBeforeOwner(
                agendaOccurrence
                    ?? throw new InvalidOperationException(
                        "a suspended rules procedure has no containing occurrence"),
                agendaOwner.Value,
                continuation);
        }
        cast.Suspend();
    }

    /// <summary>Resume an initiated ability after its cost procedure settles.</summary>
    private static void SuspendAfterCost(
        Cast cast, int abilityOrdinal, PhaseStep? owner, Occurrence? occurrence)
    {
        var results = ContinuationResults(cast, abilityOrdinal);
        results["costProcedurePending"] = 1;
        var continuation = new PhaseStep(
            Steps.ResumeAbility,
            cast.World.Agenda.Current?.Round ?? 0,
            2,
            Subject: cast.Source.ObjectId,
            Seat: cast.Player,
            Plan: true,
            Tier: cast.Tier,
            FinalStep: cast.FinalStep,
            FinalPlayer: cast.FinalPlayer,
            EachPlayerFrame: cast.EachPlayerFrame,
            Trigger: cast.Trigger,
            SurgeGained: cast.GainedKeywords.Contains("surge"),
            Discarded: [.. cast.Discarded.Select(card => card.ObjectId)],
            AbilityOrdinal: abilityOrdinal,
            AbilityPath: [],
            AbilityResults: results,
            AbilityOccurrence: cast.Occurrence,
            AbilityFace: cast.AbilityFace,
            AbilityPlayer: cast.AbilityPlayer,
            AbilityActor: cast.AbilityActor?.ObjectId ?? -1,
            AbilityHasContinuation: cast.HasContinuation);
        if (owner is null)
        {
            cast.World.Agenda.Then(continuation);
        }
        else
        {
            cast.World.Agenda.ContinueBeforeOwner(
                occurrence ?? throw new InvalidOperationException(
                    "a suspended cost has no containing occurrence"),
                owner.Value, continuation);
        }
    }

    private static Dictionary<string, long> ContinuationResults(
        Cast cast, CompiledCardAbility ability)
    {
        var results = new Dictionary<string, long>(cast.Results, StringComparer.Ordinal);
        cast.PersistCrisisIgnoringThwarts(ability, results);
        PersistSource(cast, results);
        PersistChosen(cast, results);
        return results;
    }

    private const string PersistedChosen = "__continuation.chosen";
    private const string PersistedChosenArea = "__continuation.chosen_area";
    private const string PersistedChosenIncarnation = "__continuation.chosen_incarnation";
    private const string PersistedSourceIncarnation = "__continuation.source_incarnation";

    private static void PersistSource(Cast cast, Dictionary<string, long> results) =>
        results[PersistedSourceIncarnation] = cast.SourceBindingIncarnation;

    private static void PersistChosen(Cast cast, Dictionary<string, long> results)
    {
        if (cast.CaptureCurrentSelection() is { } chosen)
        {
            PersistChosen(chosen, results);
        }
    }

    private static void PersistChosen(
        AbilityCardReference chosen, Dictionary<string, long> results)
    {
        results[PersistedChosen] = chosen.Card.ObjectId;
        results[PersistedChosenArea] = chosen.Area;
        results[PersistedChosenIncarnation] = chosen.Incarnation;
    }

    /// <summary>Propagate one reveal-scoped Surge gain to work already suspended.</summary>
    private static void RememberGainedSurge(World world, int source)
    {
        // Choice and each-player continuations are saveable agenda data. An
        // earlier ability can already have scheduled one when a later sibling
        // ability gains Surge, so its original snapshot must be advanced too.
        // The rulebook determines the shared non-numeric keyword instance; the
        // propagation mechanism is the engine's choice.
        world.Agenda.MarkSurgeGained(source);
        if (world.CharacterAttack is { Source: var attackSource } attack
            && attackSource == source)
        {
            world.CharacterAttack = attack with { SurgeGained = true };
        }
        if (world.CharacterThwart is { Source: var thwartSource } thwart
            && thwartSource == source)
        {
            world.CharacterThwart = thwart with { SurgeGained = true };
        }
    }

    private static IReadOnlyList<Card> AlliesInPlayerDiscards(World world) =>
        AbilityExpressionEvaluation.AlliesInPlayerDiscards(world);

    private static IReadOnlyList<ResourceSource> MakeTheCallSources(
        World world, int player, Card source, Card ally) =>
        AbilityExpressionEvaluation.MakeTheCallSources(world, player, source, ally);
}
