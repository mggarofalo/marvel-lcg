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
    /// <summary>
    /// Whose opportunity an ability in a window is, or <c>-1</c> for every
    /// seat's — <c>rr:ability.8</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The card's controller, unless the trigger names somebody. "Players can only
    /// trigger interrupt / response abilities on cards they control or on
    /// encounter cards", and an encounter card is one the scenario owns, so
    /// <c>-1</c> here means <i>anyone</i> rather than nobody.
    /// </para>
    /// <para>
    /// <b>A card that says "you" may name the occurrence's player rather than
    /// its controller.</b> <c>rr:you-your.7</c> — "for abilities that trigger
    /// 'after [enemy] attacks you,' 'you' refers to the attacked player, even
    /// if that player defended with an ally." Prelate Armor's "after
    /// <i>you</i> make a basic attack against Unus" is no opportunity at all
    /// for a player who did not attack, and it is that player's hand the cost
    /// would otherwise be priced against.
    /// </para>
    /// <para>
    /// Which is why this is written on the trigger rather than inferred from
    /// the card having no owner: "any player may" and "the player it happened
    /// to" are both things an encounter card can say, and only the card knows
    /// which it said.
    /// </para>
    /// </remarks>
    private int Controller(
        World world, CompiledCardAbility ability, Card card, Occurrence occurrence) =>
        RestrictedPlayer(world, ability, card) is { } restricted
            ? restricted
            : ability.Trigger.Player is not null
            ? occurrence.Player
            : ability.Trigger.Actor == AbilityRoles.You
                ? occurrence.ActorFacts?.Controller ?? ControllerOf(world, card)
                : ControllerOf(world, card);

    /// <summary>The one player allowed to use this encounter-card ability, if any.</summary>
    /// <remarks>
    /// <para>
    /// <c>rr:ability.8.1</c>: only the controller of a player card bearing an
    /// attachment may trigger or pay for that attachment's abilities that use
    /// “you” or “your”. The host supplies that controller; the attachment is
    /// still owned by the scenario.
    /// </para>
    /// <para>
    /// <c>rr:ability.8.2</c>: only the player whose play area holds an
    /// obligation may trigger or pay for it. This is a permission distinct
    /// from control, because an obligation remains an encounter card.
    /// </para>
    /// </remarks>
    private int? RestrictedPlayer(World world, CompiledCardAbility ability, Card card)
    {
        // The Golden Rules give explicit card text precedence. Obedience
        // Potion-shaped attachments say “Any player can do this,” so that
        // permission overrides the otherwise card-wide “your identity” binding.
        if (ability.AnyPlayer)
        {
            return null;
        }

        if (world.Facts.Kind(card.FaceId) == CardKind.Obligation
            && card.Area.PlayArea.IsPlayers)
        {
            return card.Area.PlayArea.Player;
        }

        if (world.Facts.Kind(card.FaceId) == CardKind.Attachment
            && card.Area.Host >= 0
            && card.Area.Host < world.Cards.Count
            && UsesYouOrYour(ability, card))
        {
            int controller = ControllerOf(world, world.Cards[card.Area.Host]);
            return controller >= 0 ? controller : null;
        }

        return null;
    }

    /// <summary>Whether the authored ability contains the printed “you/your” binding.</summary>
    private bool UsesYouOrYour(CompiledCardAbility ability, Card card) =>
        AbilityPlayerBindingAnalysis.UsesYouOrYour(program, ability, card);

    private static bool ContainsYouOrYour(AbilityCost? cost) =>
        AbilityPlayerBindingAnalysis.Contains(cost);

    /// <summary>Whether this player is permitted to initiate the ability.</summary>
    private bool MayInitiate(World world, CompiledCardAbility ability, Card card, int player)
    {
        // `rr:player-turn.5.a-c` grants permission per ability: a player may
        // use their card, an encounter card, or the particular ability whose
        // text allows them. One AnyPlayer ability must not expose its card's
        // other controller-only actions or resource abilities.
        bool cardPermits = ControllerOf(world, card) == player
            || card.Owner == World.Scenario
            || ability.AnyPlayer;
        return cardPermits
            && (RestrictedPlayer(world, ability, card) is not { } restricted
                || restricted == player);
    }

    private static bool Subject(
        World world, string? subject, Card card, Occurrence occurrence,
        int? restricted = null) => subject switch
    {
        null => true,
        AbilitySubjects.This => occurrence.Subject == card.ObjectId,
        AbilitySubjects.AttachedTo => card.Area.Host >= 0 && occurrence.Subject == card.Area.Host,
        AbilitySubjects.You => occurrence.Player >= 0
            && occurrence.Player == (restricted ?? ControllerOf(world, card)),

        // Nothing to match: the condition alone decides. `Waiting` has already
        // checked that the card is in play and that the occurrence carries the
        // condition, which is the whole of what such a card asks for.
        AbilitySubjects.Game => true,
        _ => throw new AbilityException($"'{subject}' is not a subject anything matches"),
    };

    /// <summary>Whether a captured card fills one named occurrence role.</summary>
    private static bool Role(
        World world, string? match, Card card, OccurrenceCard? role,
        int? restricted = null) => match switch
    {
        null => true,
        _ when role is null => false,
        AbilityRoles.This => role.Card == card.ObjectId,
        AbilityRoles.AttachedTo => card.Area.Host >= 0 && role.Card == card.Area.Host,
        AbilityRoles.You => role.Controller >= 0
            && (restricted is { } player
                ? role.Controller == player
                : card.Owner == World.Scenario || role.Controller == ControllerOf(world, card)),
        AbilityRoles.Villain => role.IsVillain,
        AbilityRoles.Minion => role.IsMinion,
        AbilityRoles.Hero => role.IsHero,
        AbilityRoles.Ally => role.IsAlly,
        AbilityRoles.Friendly => role.IsFriendly,
        AbilityRoles.Enemy => role.IsEnemy,
        _ => throw new AbilityException($"'{match}' is not an occurrence role matcher"),
    };

    /// <summary>Whether the occurrence's player fills the trigger's player role.</summary>
    private static bool Player(
        World world, string? match, Card card, Occurrence occurrence,
        int? restricted = null) => match switch
    {
        null or AbilityPlayers.TriggerPlayer => true,
        AbilityPlayers.You => occurrence.Player >= 0
            && occurrence.Player == (restricted ?? ControllerOf(world, card)),
        _ => throw new AbilityException($"'{match}' is not an occurrence player matcher"),
    };

    // ---- the effect tree ---------------------------------------------------

    private static void Run(CompiledCardAbility ability, Cast cast)
    {
        var labels = ability.Labels;
        if (labels.Length > 0)
        {
            if (!cast.LabelsPreflighted)
            {
                if (!AbilityInitiation.LabelsCanInitiate(ability, AdmissionContext(cast)))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' cannot initiate its labeled ability "
                        + "in the current state");
                }
                cast.LabelsPreflighted = true;
            }

            var performer = LabeledAbilities.Begin(
                cast.World, cast.World.Facts, Resolver(cast), cast.Source,
                labels, cast.Events);
            if (performer is null)
            {
                return;
            }

            cast.AbilityActor = performer;
            if (labels.Contains(Attack.DefenseVerb, StringComparer.Ordinal))
            {
                cast.Results["defenseAbilityDefender"] = performer.ObjectId;
                Attack.BeginDefenseAbility(cast.World, Resolver(cast), performer);
            }
        }

        Run(ability.Effect, cast);
    }

    private static void Run(AbilityEffect node, Cast cast)
    {
        int eventsBefore = cast.Events.Count;
        var agendaOwner = cast.World.Agenda.Current;
        var agendaOccurrence = cast.World.Agenda.Occurrence;
        var healthBefore = cast.World.Effects.CaptureCharacterHealth();
        var instruction = node;
        if (instruction is AbilityEffect.ActivateEnemies activation)
        {
            Activate(activation, node, cast);
        }
        else if (!TryRunCardState(instruction, cast)
            && !TryRunImmediateEffect(instruction, cast)
            && !TryRunDamageAndThreat(instruction, node, cast)
            && !TryRunCardMovement(instruction, cast))
        {
            RunRemainingEffect(node, cast);
        }
        if (cast.Events.Count > eventsBefore && EventMeansEffectApplied(node.OperationName()))
        {
            cast.ResolveEffect();
        }

        // A conditional constant can become Stalwart because this node changed
        // threat, counters, traits, or another dependency. `rr:stalwart.2`
        // removes existing stunned/confused cards at that transition, before
        // later text in the same ability reads the board.
        Statuses.RemoveAfflictionsIfStalwart(
            cast.World, cast.World.Facts, "stalwart", cast.Events);
        bool healthDefeatSuspended = cast.World.Effects.SettleLostHealth(
            healthBefore, cast.Trigger, cast.Events);
        if (healthDefeatSuspended && !cast.Suspended)
        {
            SuspendAfterProcedure(
                node, cast, agendaOwner, agendaOccurrence);
        }

        // `rr:attack-enemy-activation.3.2`: a defending ally that leaves play
        // immediately stops defending and exposes its controller's identity.
        // Recheck after every node so later text in the same ability, and the
        // next boost ability, reads the new attack roles rather than a stale
        // defender that has already moved.
        Attack.RefreshDefender(cast.World, cast.World.Facts);
    }

    private static void RunRemainingEffect(AbilityEffect node, Cast cast)
    {
        switch (node.OperationName())
        {
            case "seq":
                Sequence(node, cast, from: 0);
                break;

            case "and":
                // `rr:and` makes the effects simultaneous and independent;
                // `rr:first-player.3` gives their order to the first player.
                var simultaneous = OrderedEffects(node).ToList();
                if (simultaneous.Count <= 1)
                {
                    foreach (var effect in simultaneous)
                    {
                        RunChild(effect, $"and:{simultaneous.IndexOf(effect)}::", cast);
                    }
                    break;
                }
                SuspendForChoice(node, cast);
                break;

            case "then":
                ResolveDependent(node, cast, ResolutionOutcome.Full, "then");
                break;

            case "otherwise":
                ResolveDependent(node, cast, ResolutionOutcome.None, "otherwise");
                break;

            case "eachPlayer":
                if (HasNestedEachPlayer(node, cast))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' nests one each-player frame inside another, "
                        + "which is not implemented");
                }
                int eachPlayerAbility = AbilityOrdinal(node, cast);
                EachPlayerEffects.Schedule(
                    cast.World, cast.Source, cast.Position + 1, cast.Tier, cast.FinalStep,
                    cast.GainedKeywords.Contains("surge"), eachPlayerAbility,
                    [.. cast.AbilityPath], cast.AbilityFace, cast.Player,
                    ContinuationResults(cast, eachPlayerAbility),
                    cast.Occurrence, [.. cast.Discarded.Select(card => card.ObjectId)],
                    cast.HasContinuation, cast.AbilityActor?.ObjectId ?? -1);
                cast.Suspend();
                break;

            case "choose":
            case "chooseCard":
                Choose(node, cast);
                break;

            case "resolveSpecials":
                if (ResolveCards(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast).Count > 0)
                {
                    SuspendForChoice(node, cast);
                }

                break;

            case "payOrExhaust":
            case "payOrEffect":
                SuspendForChoice(node, cast);
                break;

            case "chooseTopForHand":
                if (TopCards(
                    cast.World.Seats[cast.Player].Deck,
                    EffectOf<AbilityEffect.ChooseTopForHand>(node, cast).Count).Count == 0)
                {
                    break;
                }
                SuspendForChoice(node, cast);
                break;

            case "chooseDiscardToShuffle":
            case "thwartDifferentSchemes":
            case "makeTheCall":
            case "legalPractice":
                SuspendForChoice(node, cast);
                break;

            case "afterActivation":
                if (cast.World.Activation is not { } current)
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' delays an effect and no enemy is activating");
                }

                ((AbilityRunner)cast.Abilities).RuntimeFor(cast.World).AfterActivation(
                    current.Id, new ActivationEffect(
                    cast.Source.ObjectId, cast.Player, cast.Tier,
                    EffectBody(node),
                    cast.Altered?.ObjectId ?? -1,
                    cast.AbilityActor?.ObjectId ?? -1));
                cast.ResolveEffect();
                break;

            case "if":
                var tested = ConditionalOf(node, cast).Test;
                var branch = ResolveCondition(tested, cast) ? "then" : "else";
                if (ConditionalBranch(node, branch) is { } taken)
                {
                    RunChild(taken, $"if:{branch}", cast);
                }

                break;

            case "forEach":
                ForEach(node, cast);
                break;

            case "eachTime":
                EachTime(node, cast);
                break;

            case "attack":
                ((AbilityRunner)cast.Abilities).SchedulePower(node, cast, BasicPowers.AttackVerb);
                break;

            case "defense":
                var defender = cast.AbilityActor ?? LabeledAbilities.Begin(
                    cast.World, cast.World.Facts, Resolver(cast), cast.Source,
                    [Attack.DefenseVerb], cast.Events);
                if (defender is not null)
                {
                    if (cast.AbilityActor is null)
                    {
                        cast.Results["defenseAbilityDefender"] = defender.ObjectId;
                        Attack.BeginDefenseAbility(cast.World, Resolver(cast), defender);
                    }
                    RunChild(EffectBody(node), "defense:effect", cast);
                }
                break;

            case "thwart":
                ((AbilityRunner)cast.Abilities).SchedulePower(node, cast, BasicPowers.ThwartVerb);
                break;

            case "thwartSchemes":
                var schemes = ResolveCards(EffectOf<AbilityEffect.ThwartGroup>(node, cast).Schemes, cast);
                if (schemes.Count > 0)
                {
                    cast.Choose(schemes[0]);
                    ((AbilityRunner)cast.Abilities).SchedulePower(
                        ((AbilityEffect.ThwartGroup)node).Thwart, cast, BasicPowers.ThwartVerb,
                        schemes[0], schemes, -1);
                }
                break;

            default:
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' uses the effect node '{node.OperationName()}', "
                    + "which is not implemented");
        }
    }

    private static bool EventMeansEffectApplied(string kind) => kind is not (
        "seq" or "and" or "then" or "otherwise" or "eachPlayer" or "if"
        or "forEach" or "eachTime" or "choose" or "chooseCard"
        or "resolveSpecials" or "payOrExhaust" or "payOrEffect"
        or "chooseTopForHand" or "chooseDiscardToShuffle"
        or "thwartDifferentSchemes" or "makeTheCall" or "legalPractice"
        or "attack" or "defense" or "thwart" or "thwartSchemes"
        or "placeThreat" or "enemyAttacks" or "enemySchemes");


    /// <summary>
    /// "Put the top card of your deck into play facedown … as a Drone minion."
    /// </summary>
    private static bool CanCreateDrones(AbilityEffect node, Cast cast) =>
        EffectOf<AbilityEffect.CreateDrones>(node, cast) is var drones
        && AbilityAdmissionFacts.CanCreateDrones(
            cast.World, Seats(drones.Players, cast), drones.Count);

    // The rules define the role, not this persisted result-key spelling. The
    // value survives a suspended printed sequence so only this defense ability
    // may replace the provisional defender it established.
    private static int ReplaceableDefenseDefender(Cast cast) =>
        checked((int)cast.Results.GetValueOrDefault("defenseAbilityDefender", -1));

}
