using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>
/// Runs authored card abilities. The one way a card's text enters the engine.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is what replaces a class per card.</b> There was one — a switch on
/// printed id, three cards deep and growing — and it was the "cards as scripts"
/// inversion this port exists to undo (<c>docs/migration.md</c>). A card is now
/// a row in <c>datasets/abilities/abilities.json</c>, and adding one is
/// authoring data rather than compiling code.
/// </para>
/// <para>
/// The vocabulary is small and every gap is loud. A node nothing implements
/// throws naming the node; a card nobody has authored throws naming the card.
/// Growing the engine means adding a case here and growing the game means adding
/// a row there, and the two are different activities on purpose.
/// </para>
/// <para>
/// See <c>docs/card-dsl.md</c> for the design this is the first executable
/// piece of, and <c>docs/enemy-attacks.md</c> for the cards it currently runs.
/// </para>
/// </remarks>
/// <param name="book">The authored cards.</param>
public sealed partial class AbilityRunner(AbilityBook book) : ICardAbilities
{
    private readonly AbilityBook book = book;

    // An activation is an agenda operation, while the sentence that initiated
    // it is a card operation. The stable agenda id is the join between them.
    // Entries live only until that activation's completion sentinel calls back.
    private readonly Dictionary<int, List<ActivationEffect>> activationEffects = [];

    // Which printed faces carry a constant ability. `Constant` is asked about
    // every card in play every time anything reads the effect list, and all but
    // a handful of cards answer nothing -- so the common answer is a set lookup
    // rather than a walk of the book.
    //
    // **A shortcut and nothing else.** Deleting it is an equivalent mutant: the
    // loop below finds the same abilities, just slower. It is here because
    // reading a stat goes through the effect list, and the digest reads every
    // stat of every card.
    private readonly HashSet<string> constant = new(
        book.Abilities
            .Where(ability => ability.Trigger.Timing == AbilityType.Constant)
            .Select(ability => ability.Card),
        StringComparer.Ordinal);

    /// <summary>The verb an option carries on the wire.</summary>
    public const string ChooseVerb = "Choose_Option";

    private static readonly string[] Branches = ["then", "else"];

    private static readonly DeckType[] Owned = [DeckType.UpgradesArea, DeckType.SupportsArea];

    // A facedown Ultron Drone retains the underlying player-card face id for
    // the state digest, but `rr:in-play-and-out-of-play.5` and `.13` make that
    // facedown card text inactive. Every authored-ability entry point goes
    // through this boundary so no trigger, action, constant, boost, or query
    // can accidentally execute the hidden card.
    private IEnumerable<CardAbility> On(Card card) =>
        FacedownDrones.Is(card) ? [] : book.On(card.FaceId);

    /// <summary>The authored cards, whether or not they do anything.</summary>
    public IReadOnlySet<string> Authored => book.Authored;

    /// <inheritdoc/>
    public CardCounterPool? CounterPool(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        if (book.CounterPool(card.FaceId) is { } authored)
        {
            return authored;
        }

        // Rules-only and focused synthetic books may omit card-level metadata.
        // The complete supported book is separately held to an exact account
        // of every printed starting pool, so this compatibility path cannot
        // conceal an omission in shipped content.
        var (count, type) = Reveal.Uses(world.Facts.Attributes(card.FaceId));
        return count > 0
            ? new CardCounterPool(type, checked((int)count), Uses: true)
            : null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> EntersPlay(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        var events = new List<GameEvent>();
        var occurrence = new Occurrence(
            0, [Steps.CardEntersPlay], Subject: card.ObjectId,
            Player: ControllerOf(world, card));
        foreach (var ability in On(card).Where(ability =>
            ability.Trigger.Timing == AbilityType.WhenRevealed
            && string.Equals(
                ability.Trigger.Event, Steps.CardEntersPlay,
                StringComparison.Ordinal)))
        {
            var cast = new Cast(
                world, card, occurrence, ControllerOf(world, card), events, this)
            {
                Tier = ability.Trigger.Timing,
            };
            TrackResolution(cast, ability);
            Run(ability, cast);
            cast.CompleteResolution();
        }

        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> ActivationCompleted(World world, EnemyActivation result)
    {
        ArgumentNullException.ThrowIfNull(world);

        var events = new List<GameEvent>();
        var enemy = world.Cards[result.Enemy];
        if (result.Made)
        {
            foreach (var ability in On(enemy).Where(ability =>
                ability.Trigger.Timing == AbilityType.ForcedResponse
                && string.Equals(
                    ability.Trigger.Event, "WhenActivationCompleted",
                    StringComparison.Ordinal)))
            {
                var cast = new Cast(
                    world, enemy,
                    new Occurrence(
                        0, ["WhenActivationCompleted"],
                        Actor: enemy.ObjectId, Player: result.Player),
                    result.Player, events, this)
                {
                    Tier = ability.Trigger.Timing,
                };
                TrackResolution(cast, ability);
                Run(ability, cast);
                cast.CompleteResolution();
            }
        }

        if (activationEffects.Remove(result.Id, out var delayed))
        {
            foreach (var effect in delayed)
            {
                var delayedCast = new Cast(
                    world,
                    world.Cards[effect.Source],
                    new Occurrence(
                        0, ["WhenActivationCompleted"],
                        Actor: result.Enemy, Player: effect.Player),
                    effect.Player,
                    events,
                    this)
                {
                    Tier = effect.Tier,
                    AbilityActor = effect.AbilityActor >= 0
                        ? world.Cards[effect.AbilityActor]
                        : null,
                };
                delayedCast.Results["activationDamage"] = result.DamageDealt;
                delayedCast.Results["activationThreat"] = result.ThreatPlaced;
                delayedCast.Results["activationMade"] = result.Made ? 1 : 0;
                if (effect.Altered >= 0)
                {
                    delayedCast.BindAlteration(world.Cards[effect.Altered]);
                }
                Run(effect.Effect, delayedCast);
            }
        }

        if (world.Agenda.CompleteActivationWait(result) is { } continuation)
        {
            events.AddRange(ResumeAbility(world, continuation));
        }

        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> ResumeAbility(World world, PhaseStep continuation)
    {
        var source = continuation.Subject >= 0 && continuation.Subject < world.Cards.Count
            ? world.Cards[continuation.Subject]
            : throw new RulesNotImplementedException(
                $"activation continuation has no card at object id {continuation.Subject}");
        if (continuation.AbilityOrdinal < 0 || continuation.AbilityPath is not { } path)
        {
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' has an incomplete activation continuation");
        }

        var cast = Resuming(
            world, source, continuation.Seat, continuation.Tier, continuation.FinalStep,
            continuation.AbilityOccurrence) with
        {
            EachPlayerFrame = continuation.EachPlayerFrame,
            FinalPlayer = continuation.FinalPlayer,
            AbilityPlayer = continuation.AbilityPlayer,
            EventTrigger = continuation.Trigger,
            GainedKeywords = continuation.SurgeGained
                ? new HashSet<string>(["surge"], StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal),
        };
        cast.RestoreAbility(
            continuation.AbilityOrdinal, path, continuation.AbilityFace);
        cast.TrackResolution(continuation.AbilityOrdinal);
        RestorePersisted(cast, continuation);
        if (cast.Results.GetValueOrDefault("costProcedurePending") > 0)
        {
            // The cost has settled. Do not persist its resume marker into a
            // later suspension inside the effect, or that continuation would
            // restart the whole effect instead of resuming its own path.
            cast.Results.Remove("costProcedurePending");
            var ability = AbilityAt(
                source, continuation.Tier, continuation.AbilityOrdinal,
                continuation.AbilityFace);
            Use(world, source, ability, cast.Occurrence);
            if (world.Facts.Kind(source.FaceId) == CardKind.Event)
            {
                cast.Occurrence.BeginCard(
                    source.ObjectId,
                    [new PendingAbility(
                        source.ObjectId,
                        ability.Trigger.Timing,
                        continuation.Seat,
                        continuation.AbilityOrdinal)]);
            }
            Run(ability, cast);
            cast.CompleteResolution();
            DiscardEvent(source, cast);
            return cast.Events;
        }
        if (cast.Results.GetValueOrDefault("activationMade") > 0
            || cast.Results.GetValueOrDefault("procedureApplied") > 0)
        {
            cast.ResolveEffect();
        }
        RestorePathBindings(cast, path);
        var root = AbilityAt(
            source, continuation.Tier, continuation.AbilityOrdinal,
            continuation.AbilityFace).Effect;
        bool repeatDynamicActivation =
            cast.Results.Remove("repeatDynamicActivation");
        if (repeatDynamicActivation)
        {
            if (cast.Results.GetValueOrDefault("activationMade") > 0)
            {
                cast.Results["dynamicActivationMade"] = 1;
            }
            Run(NodeAtPath(root, path), cast);
            if (cast.Suspended)
            {
                cast.CompleteResolution();
                return cast.Events;
            }
        }
        int eachPlayer = path.ToList().FindIndex(frame =>
            frame.StartsWith("eachPlayer:", StringComparison.Ordinal));
        ResumeAfter(
            root, path, cast,
            stopBefore: continuation.EachPlayerFrame && !continuation.FinalPlayer
                ? eachPlayer
                : -1);
        cast.CompleteResolution();
        DiscardEvent(source, cast);
        return cast.Events;
    }

    /// <inheritdoc/>
    public void ResolveCardAttack(
        World world, CharacterAttack attack, Occurrence occurrence, List<GameEvent> events) =>
        ResolvePower(
            world, attack.Source, attack.Enemy, attack.Player, attack.AbilityIndex,
            attack.PowerOrdinal, attack.ResumeFrom, attack.FinalStep,
            attack.Targets ?? [attack.Enemy], attack.Amount, null,
            attack.Trigger, attack.SurgeGained, occurrence, events,
            BasicPowers.AttackVerb, attack.AbilityPath, attack.AbilityFace,
            attack.AbilityResults, attack.AbilityOccurrence, attack.Discarded,
            attack.EachPlayerFrame, attack.FinalPlayer, attack.AbilityPlayer,
            attack.AbilityHasContinuation, attack.AbilityActor);

    /// <inheritdoc/>
    public void ResolveCardThwart(
        World world, CharacterThwart thwart, Occurrence occurrence, List<GameEvent> events) =>
        ResolvePower(
            world, thwart.Source, thwart.Scheme, thwart.Player, thwart.AbilityIndex,
            thwart.PowerOrdinal, thwart.ResumeFrom, thwart.FinalStep,
            thwart.Targets ?? [thwart.Scheme], thwart.Amount, thwart.ImminentThreat,
            thwart.Trigger, thwart.SurgeGained, occurrence, events,
            BasicPowers.ThwartVerb, thwart.AbilityPath, thwart.AbilityFace,
            thwart.AbilityResults, thwart.AbilityOccurrence, thwart.Discarded,
            thwart.EachPlayerFrame, thwart.FinalPlayer, thwart.AbilityPlayer,
            thwart.AbilityHasContinuation, thwart.AbilityActor);

    private void ResolvePower(
        World world, int sourceId, int targetId, int player, int abilityIndex,
        int powerOrdinal, int resumeFrom, bool finalStep, IReadOnlyList<int> targets,
        long powerAmount, ThreatPlacement? imminentThreat, string eventTrigger,
        bool surgeGained,
        Occurrence occurrence,
        List<GameEvent> events, string power, IReadOnlyList<string>? abilityPath = null,
        string abilityFace = "", IReadOnlyDictionary<string, long>? abilityResults = null,
        Occurrence? abilityOccurrence = null, IReadOnlyList<int>? discarded = null,
        bool eachPlayerFrame = false, bool finalPlayer = false, int abilityPlayer = -1,
        bool abilityHasContinuation = false, int abilityActor = -1)
    {
        if (sourceId < 0 || sourceId >= world.Cards.Count)
        {
            throw new RulesNotImplementedException(
                $"card {power.ToLowerInvariant()} has no reconstructable source");
        }

        var source = world.Cards[sourceId];
        var abilities = AbilitiesOn(source, abilityFace).ToList();
        var ability = abilities.ElementAtOrDefault(abilityIndex)
            ?? throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no ability {abilityIndex} for its "
                + power.ToLowerInvariant());
        var wrappers = PowerNodes(ability.Effect, power).ToList();
        var wrapper = wrappers.ElementAtOrDefault(powerOrdinal)
            ?? throw new RulesNotImplementedException(
                $"'{source.FaceId}' ability {abilityIndex} has no {power.ToLowerInvariant()} "
                + $"wrapper {powerOrdinal}");
        var effect = Tree(wrapper.Require("effect"));
        var cast = new Cast(
            world, source, abilityOccurrence ?? occurrence, player, events, this)
        {
            Tier = ability.Trigger.Timing,
            FinalStep = finalStep,
            Power = power,
            PowerAmount = powerAmount,
            ImminentThreat = imminentThreat,
            EventTrigger = eventTrigger,
            PowerTargets = [.. targets.Select(id => world.Cards[id])],
            PowerActor = occurrence.Actor >= 0 ? world.Cards[occurrence.Actor] : null,
            EachPlayerFrame = eachPlayerFrame,
            FinalPlayer = finalPlayer,
            AbilityPlayer = abilityPlayer,
            AbilityActor = abilityActor >= 0 ? world.Cards[abilityActor] : null,
            GainedKeywords = surgeGained
                ? new HashSet<string>(["surge"], StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal),
        };
        cast.Choose(world.Cards[targetId]);
        RestorePersisted(cast, discarded, abilityResults);
        if (abilityPath is not null)
        {
            RestorePathBindings(cast, abilityPath);
        }
        if (SuspendsPowerEffect(effect, cast))
        {
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' suspends inside a {power.ToLowerInvariant()}, "
                + "which is not implemented");
        }
        int ordinal = abilities
            .Where(candidate => candidate.Trigger.Timing == ability.Trigger.Timing)
            .ToList()
            .IndexOf(ability);
        cast.RestoreAbility(ordinal, abilityPath ?? [], abilityFace);
        cast.TrackResolution(ordinal);
        var attackModifiers = power == BasicPowers.AttackVerb
            ? EventModifierEffects(cast, "attackDamage")
            : [];
        Run(effect, cast);

        // A modifier to "an attack" lasts through every damage node belonging
        // to that attack, then is consumed once. This is deliberately at the
        // wrapper boundary rather than in generic dealDamage: one attack may
        // damage several characters, while a later wrapper is a later attack.
        foreach (var modifier in attackModifiers)
        {
            world.Effects.Use(modifier);
        }

        if (power == BasicPowers.AttackVerb)
        {
            var attacker = cast.PowerActor ?? world.Seats[player].IdentityCard;
            if (!Keywords.Has(world, attacker, Keywords.Ranged, world.Facts))
            {
                foreach (var target in cast.Attacked.DistinctBy(card => card.ObjectId))
                {
                    Damage.Retaliate(world, world.Facts, target, attacker, cast.Trigger, events);
                }
            }
        }

        // The labelled power owns `chosen` while its effect runs. The outer
        // ability's earlier selection is a different binding and becomes
        // current again only when that outer continuation resumes.
        RestorePersistedChosen(cast, abilityResults, overwrite: true);

        if (!cast.Suspended && abilityPath is not null)
        {
            int eachPlayer = abilityPath.ToList().FindIndex(frame =>
                frame.StartsWith("eachPlayer:", StringComparison.Ordinal));
            ResumeAfter(
                ability.Effect, abilityPath, cast,
                stopBefore: eachPlayerFrame && !finalPlayer ? eachPlayer : -1);
        }
        else if (!cast.Suspended && resumeFrom >= 0)
        {
            if (ability.Effect.Kind != "seq")
            {
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' resumes a {power.ToLowerInvariant()} outside a sequence");
            }
            Sequence(ability.Effect, cast, resumeFrom);
        }
        cast.CompleteResolution();
        DiscardEvent(source, cast);
    }

    /// <inheritdoc/>
    public string ResourcesGeneratedBy(World world, Card source, Card? payingFor)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);

        string printed = Resources.GeneratedBy(source.FaceId, world.Facts);
        if (payingFor is null)
        {
            return printed;
        }

        string classes = world.Facts.Attributes(payingFor.FaceId)
            .GetValueOrDefault("Class", string.Empty);
        bool doubles = On(source).Any(ability =>
            ability.Trigger.Timing == AbilityType.Constant
            && ability.Effect.Kind == "doubleResourceFor"
            && classes.Split(';').Contains(
                Word(ability.Effect.Argument), StringComparer.Ordinal));

        return doubles ? printed + printed : printed;
    }

    /// <inheritdoc/>
    public DefenderChoice Defenders(
        World world, EnemyAttack attack, IReadOnlyList<Card> candidates)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(candidates);

        var enemy = world.Cards[attack.Enemy];
        bool requiresControlledAlly = On(enemy).Any(ability =>
            ability.Trigger.Timing == AbilityType.Constant
            && ability.Effect.Kind == "requireAllyDefender");
        if (!requiresControlledAlly)
        {
            return new DefenderChoice(candidates, Required: false);
        }

        var allies = candidates.Where(card =>
            card.Ready
            && card.Area.PlayArea == PlayArea.Of(attack.Player)
            && world.Facts.Kind(card.FaceId) == CardKind.Ally).ToList();
        return allies.Count > 0
            ? new DefenderChoice(allies, Required: true)
            : new DefenderChoice(candidates, Required: false);
    }

    /// <inheritdoc/>
    public bool CanRemoveThreat(World world, Card scheme, int ignoredSource = -1)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(scheme);

        foreach (var card in world.Cards.Where(card =>
                     card.ObjectId != ignoredSource && DeckTypes.IsInPlay(card.Area.Type)))
        {
            foreach (var ability in On(card).Where(ability =>
                ability.Trigger.Timing == AbilityType.Constant))
            {
                if (ProhibitsThreatRemoval(
                        ability.Effect,
                        new Cast(world, card, new Occurrence(0, []),
                            ControllerOf(world, card), [], this),
                        scheme))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public IReadOnlyList<PendingAbility> Waiting(
        World world, Occurrence occurrence, WindowKind window)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(occurrence);

        var waiting = new List<PendingAbility>();
        foreach (var card in world.Cards)
        {
            // `rr:ability.1` -- a card's ability functions while the card is in
            // play. Being attached to something is not the same as being in
            // play: the recorded Tough hangs off Rhino from a zone that is not.
            bool eventInHand = world.Facts.Kind(card.FaceId) == CardKind.Event
                && card.Owner >= 0
                && card.Area == world.Seats[card.Owner].Hand;
            if (!DeckTypes.IsInPlay(card.Area.Type) && !eventInHand)
            {
                continue;
            }

            var written = On(card).ToList();
            for (int index = 0; index < written.Count; index++)
            {
                var ability = written[index];
                IEnumerable<int> players;
                if (!ability.AnyPlayer)
                {
                    players = [Controller(world, ability, card, occurrence)];
                }
                else if (ability.Trigger.Player == AbilityPlayers.TriggerPlayer
                    && occurrence.Player >= 0)
                {
                    // The permission is broad, but the trigger is not:
                    // trigger.player is the seat the occurrence happened to.
                    players = [occurrence.Player];
                }
                else
                {
                    players = world.PlayerOrder;
                }
                foreach (int controller in players)
                {
                    if (!Answers(
                            world, ability, card, occurrence, window,
                            ability.AnyPlayer ? controller : null))
                    {
                        continue;
                    }

                    // `rr:initiating-abilities.step.2` -- "if the card or ability
                    // has a form requirement (for example, 'Hero form only' or
                    // 'Hero Action'), the form of the player playing that card or
                    // initiating that ability is checked now." Step 2 is about any
                    // ability, not only an action: Prelate Armor prints a *Hero
                    // Response*, and an alter-ego cannot initiate one.
                    if (!InForm(world, controller, ability.Trigger.Form, card))
                    {
                        continue;
                    }

                    var eligibility = new Cast(
                        world, card, occurrence, controller, [], this);
                    eligibility.SetPaymentMayMutate(
                        ability.Cost is not null
                        || world.Facts.Kind(card.FaceId) == CardKind.Event,
                        ability.Cost);
                    if ((ability.When is not null && !Test(ability.When, eligibility))
                        || (controller >= 0 && !CanInitiate(ability, eligibility)))
                    {
                        continue;
                    }

                    // `rr:initiating-abilities.step.3` -- the cost and "the
                    // player's ability to pay them" are one step, and only "if both
                    // conditions are met" do the later steps happen. So an ability
                    // nobody can pay for is not an offer that fails at step 5; it
                    // never reaches the window at all.
                    if (!Payable(world, card, controller, ability.Cost)
                        || !EventPayable(world, card, controller, ability)
                        || !Available(world, card, ability, occurrence))
                    {
                        continue;
                    }

                    int ordinal = written.Take(index).Count(candidate =>
                        candidate.Trigger.Timing == ability.Trigger.Timing);
                    waiting.Add(new PendingAbility(
                        card.ObjectId, ability.Trigger.Timing, controller, ordinal));
                }
            }
        }

        return waiting;
    }

    /// <inheritdoc/>
    public Affordance Describe(World world, PendingAbility ability)
    {
        ArgumentNullException.ThrowIfNull(world);

        var card = world.Cards[ability.Card];
        var found = Pending(card, ability);

        // The ability's own name is the verb: an affordance for Foresight is
        // offered as `Foresight`, so a client has something to render without
        // knowing what the ability does. One string does for both fields
        // because the engine carries one -- see the remarks on `Affordance.Id`.
        var price = CombinedPrice(world, card, ability.Player, found);
        return new Affordance(
            Id: ability.Card,
            Verb: found.Name,
            AnchorId: ability.Card,
            AnchorPlayer: ability.Player,
            Label: found.Name,
            Targets: Asking(world, ability.Player, found.Cost),
            Costs: price is null ? null : [price]);
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Resolve(
        World world, Occurrence occurrence, PendingAbility ability, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen) =>
        Resolve(world, occurrence, ability, paying, chosen, values: null, allocations: null);

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Resolve(
        World world, Occurrence occurrence, PendingAbility ability, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values = null,
        IReadOnlyList<ResourceAllocation>? allocations = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(occurrence);
        ArgumentNullException.ThrowIfNull(paying);
        ArgumentNullException.ThrowIfNull(chosen);

        var card = world.Cards[ability.Card];
        var found = Pending(card, ability);

        var events = new List<GameEvent>();

        // **Who "you" is, which is not who may trigger it.**
        // `PendingAbility.Player` is control -- `rr:ability.8` lets any player
        // use an optional ability on an encounter card, so an encounter card's
        // is the scenario. That is the right answer to "whose opportunity is
        // this" and the wrong one to "who does the card mean by *you*".
        //
        // `rr:you-your.7` is explicit for the case this arrived on: "for
        // abilities that trigger 'after [enemy] attacks you,' 'you' refers to
        // the attacked player, even if that player defended with an ally." The
        // attacked player is the occurrence's, so an ability on a card nobody
        // owns resolves as the player the occurrence happened to. `.16` is not
        // in the way -- it says an encounter card's ability is not performed by
        // that player's identity, which is about who acts, not about who the
        // word points at.
        int resolving = ability.Player >= 0 ? ability.Player : occurrence.Player;
        var cast = new Cast(world, card, occurrence, resolving, events, this)
        {
            Tier = found.Trigger.Timing,
        };
        cast.SetPaymentMayMutate(
            found.Cost is not null || world.Facts.Kind(card.FaceId) == CardKind.Event,
            found.Cost);

        if (!Available(world, card, found, occurrence))
        {
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' has reached its printed maximum for this "
                + "ability's period");
        }

        // A forced ability is resolved rather than offered, but its printed
        // arrow cost is still paid at `rr:initiating-abilities.step.5`.
        // Superhuman Strength's “discard this card” names the whole payment,
        // so no player decision is needed. A mandatory cost that does require
        // a selection needs a prompt carried by the timing window; refuse that
        // state instead of choosing on the player's behalf.
        if (AbilityTypes.IsMandatory(found.Trigger.Timing) && found.Cost is not null)
        {
            if (!MandatoryCostIsAutomatic(found.Cost))
            {
                throw new RulesNotImplementedException(
                    $"'{card.FaceId}' has a mandatory ability whose '{found.Cost.Kind}' "
                    + "cost requires a player decision");
            }
            if (!Payable(world, card, resolving, found.Cost))
            {
                return events;
            }
        }
        else if (!CounterCostsPayable(world, card, resolving, found.Cost))
        {
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' can no longer pay this ability's cost");
        }

        if (resolving >= 0 && !CanInitiate(found, cast))
        {
            // A mandatory ability with no valid target does not become a
            // question and cannot initiate. The window has still reached it,
            // so resolving it means doing nothing rather than stopping the
            // timing sequence on an impossible instruction.
            if (AbilityTypes.IsMandatory(found.Trigger.Timing))
            {
                return events;
            }
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' cannot initiate this ability in the current state");
        }

        // `rr:initiating-abilities` keeps the steps apart, and step 5 pays
        // before step 6 resolves. Nothing here can abort for want of resources,
        // because step 3 -- `Payable`, when the ability was offered -- already
        // asked whether the cost could be paid at all. What it cannot check is
        // that the player named a payment that works, and `CardPlay.Spend`
        // refuses one that does not.
        ValidatePayment(found.Cost, paying, chosen, values, cast);
        PayEvent(card, paying, cast, found.Effect, allocations, found.Cost);
        if (world.Facts.Kind(card.FaceId) == CardKind.Event
            && ResourceRequirement(found.Cost, card).Length > 0)
        {
            PayNonResourceCosts(found.Cost, paying, chosen, values, cast);
        }
        else
        {
            Pay(found.Cost, paying, chosen, values, cast);
        }
        if (cast.Suspended)
        {
            SuspendAfterCost(cast, ability.Ordinal);
            return events;
        }
        Use(world, card, found, occurrence);
        if (world.Facts.Kind(card.FaceId) == CardKind.Event)
        {
            occurrence.BeginCard(card.ObjectId, [ability]);
        }
        cast.RestoreAbility(ability.Ordinal, []);
        cast.TrackResolution(ability.Ordinal);
        Run(found, cast);
        cast.CompleteResolution();
        DiscardEvent(card, cast);
        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player) =>
        WhenRevealed(
            world, card, player,
            new Occurrence(0, [Steps.CardRevealed], Subject: card.ObjectId, Player: player));

    /// <inheritdoc/>
    public IReadOnlyList<PendingAbility> WhenRevealedAbilities(
        World world, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        if (!book.KnowsWhenRevealed(card.FaceId))
        {
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' was revealed and no ability data is written for it; "
                + $"this engine has {book.Authored.Count} authored card(s)");
        }

        return [.. On(card)
            .Where(ability => ability.Trigger.Timing == AbilityType.WhenRevealed)
            .Where(ability => string.Equals(
                ability.Trigger.Event, Steps.CardRevealed, StringComparison.Ordinal))
            .Select((_, ordinal) => new PendingAbility(
                card.ObjectId, AbilityType.WhenRevealed, player, ordinal))];
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> WhenRevealed(
        World world, Card card, int player, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(occurrence);

        if (!book.KnowsWhenRevealed(card.FaceId))
        {
            // Authored-and-does-nothing is a different thing from nobody having
            // read the card, and only one of them is safe to treat as silence.
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' was revealed and no ability data is written for it; "
                + $"this engine has {book.Authored.Count} authored card(s)");
        }

        var reveals = On(card)
            .Where(ability => ability.Trigger.Timing == AbilityType.WhenRevealed)
            .Select((ability, ordinal) => (Ability: ability, Ordinal: ordinal))
            .Where(entry => string.Equals(
                entry.Ability.Trigger.Event, Steps.CardRevealed,
                StringComparison.Ordinal))
            .ToList();
        var addresses = reveals.Select(entry => new PendingAbility(
            card.ObjectId, AbilityType.WhenRevealed, player, entry.Ordinal)).ToList();
        if (world.Facts.Kind(card.FaceId) == CardKind.Treachery)
        {
            occurrence.BeginCard(card.ObjectId, addresses);
        }

        var events = new List<GameEvent>();
        if (CancelWhenRevealed(world, card, player, occurrence))
        {
            return events;
        }

        // One reveal can contain several authored abilities. A non-numeric
        // keyword gained by more than one of them is still one keyword, so the
        // casts share which keyword grants have already resolved.
        var gainedKeywords = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (ability, ordinal) in reveals)
        {
            // `rr:ability.step.3` -- "When Revealed" *is* the occurrence, not a
            // window around it. An interrupt or a response to a card being
            // revealed is a different ability and reaches the board through
            // `Waiting`, so matching on the condition alone would run it twice.
            var cast = new Cast(world, card, occurrence, player, events, this)
            {
                Tier = ability.Trigger.Timing,
                GainedKeywords = gainedKeywords,
            };
            cast.RestoreAbility(ordinal, []);
            cast.TrackResolution(ordinal);
            Run(ability, cast);
            cast.CompleteResolution();
        }

        return events;
    }

    /// <inheritdoc/>
    public bool CancelWhenRevealed(
        World world, Card card, int player, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(occurrence);

        var authored = On(card)
            .Where(ability => ability.Trigger.Timing == AbilityType.WhenRevealed)
            .Select((ability, ordinal) => (ability, ordinal))
            .Where(entry => string.Equals(
                entry.ability.Trigger.Event, Steps.CardRevealed,
                StringComparison.Ordinal))
            .Select(entry => new PendingAbility(
                card.ObjectId, AbilityType.WhenRevealed, player, entry.ordinal));
        var addresses = authored
            .Concat(Reveal.KeywordAbilities(world, world.Facts, card, player))
            .ToList();
        var cancellation = world.Effects.Active().FirstOrDefault(effect =>
            string.Equals(effect.Kind, "cancelWhenRevealed", StringComparison.Ordinal)
            && effect.Affects == card.ObjectId);
        var kind = world.Facts.Kind(card.FaceId);
        bool mayBeCanceled = !CardKinds.IsVillain(kind) && kind != CardKind.MainScheme;
        if (!mayBeCanceled || cancellation is null || !world.Effects.Use(cancellation))
        {
            return false;
        }

        if (world.Facts.Kind(card.FaceId) == CardKind.Treachery)
        {
            occurrence.BeginCard(card.ObjectId, addresses);
        }
        foreach (var address in addresses)
        {
            occurrence.Cancel(address);
        }
        return true;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Boost(World world, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        // **Not "is the card authored" but "is this half of it".** A card with
        // two abilities at two tiers -- `01168` Sweeping Swoop has a "When
        // Revealed" and a "Boost" -- would otherwise pass on the strength of
        // the half somebody had written, and the other half would go back to
        // being silent.
        var boosts = On(card)
            .Where(ability => ability.Trigger.Timing == AbilityType.Boost)
            .ToList();

        if (boosts.Count == 0)
        {
            // **The star gates the complaint, not the run.** The printed
            // `Boost` attribute counts icons and `rr:boost-boost-icon.1` says a
            // star is not one, so a card with an ability and a card without
            // carry the same number and only the text box can tell them apart.
            // Asked here rather than first, so that the text box cannot veto
            // authored data.
            return world.Facts.HasBoostAbility(card.FaceId)
                ? throw new RulesNotImplementedException(
                    $"card '{card.FaceId}' was turned faceup as a boost card and prints a "
                    + "'Boost' ability that no ability data is written for")
                : [];
        }

        var events = new List<GameEvent>();
        var occurrence = new Occurrence(
            0, [Steps.CardRevealed], Subject: card.ObjectId, Player: player);

        foreach (var (ability, ordinal) in boosts.Select((ability, ordinal) =>
                     (ability, ordinal)))
        {
            // `rr:ability` puts a "Boost" ability at the occurrence tier, like
            // "When Revealed": it is the thing happening rather than a window
            // around it, so there is nothing to offer and nothing to decline.
            var cast = new Cast(world, card, occurrence, player, events, this)
            {
                Tier = ability.Trigger.Timing,
            };
            cast.RestoreAbility(ordinal, []);
            cast.TrackResolution(ordinal);
            Run(ability, cast);
            cast.CompleteResolution();
        }

        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> ResolveSpecial(
        World world, Card card, int player, bool finalStep)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        var ability = On(card).SingleOrDefault(candidate =>
            candidate.Trigger.Timing == AbilityType.Special)
            ?? throw new RulesNotImplementedException(
                $"card '{card.FaceId}' has no authored Special ability");
        var events = new List<GameEvent>();
        var cast = new Cast(
            world, card,
            new Occurrence(0, [Steps.ResolveSpecial], Subject: card.ObjectId, Player: player),
            player, events, this)
        {
            Tier = AbilityType.Special,
            FinalStep = finalStep,
        };
        if (CanInitiate(ability, cast))
        {
            cast.RestoreAbility(0, []);
            cast.TrackResolution(0);
            Run(ability, cast);
            cast.CompleteResolution();
        }
        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ResourceSource> ResourceAbilities(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);

        var sources = new List<ResourceSource>();
        foreach (var card in Triggerable(world, player).ToList())
        {
            foreach (var ability in On(card))
            {
                var eligibility = new Cast(
                    world,
                    card,
                    new Occurrence(
                        0, [Steps.TurnAction], Subject: card.ObjectId, Player: player),
                    player,
                    [],
                    this);
                if (ability.Trigger.Timing != AbilityType.Resource
                    || !MayInitiate(world, ability, card, player)
                    || !Available(world, card, ability)
                    || !InForm(world, player, ability.Trigger.Form)
                    || !Payable(world, card, player, ability.Cost)
                    || (ability.When is not null && !Test(ability.When, eligibility)))
                {
                    continue;
                }

                // The letters this makes, read off the effect rather than the
                // printed `RES` field: `RES` is what discarding the card
                // generates, and an ability is a different way to make one.
                string generated = Generated(ability.Effect, world, player);
                if (generated.Length > 0)
                {
                    sources.Add(new ResourceSource(card.ObjectId, generated));
                }
            }
        }

        return sources;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ResourceSource> PrintedResourceAbilities(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);

        var available = ResourceAbilities(world, player);
        return
        [
            .. available.Where(source => On(world.Cards[source.Effect]).Any(ability =>
                ability.Trigger.Timing == AbilityType.Resource
                && string.Equals(
                    ability.PrintedResources, source.Generates, StringComparison.Ordinal))),
        ];
    }

    /// <summary>
    /// Whether an ability has uses left this round — <c>rr:limit</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "Each copy of an ability with such a limit may be used X times per the
    /// specified period, <b>per instance of that ability</b>", so the count is
    /// kept against the card in play rather than the printed id: two Peter
    /// Parkers at one table have one use each.
    /// </para>
    /// <para>
    /// <b>Kept as a lasting effect and not a token.</b> A card's tokens are on
    /// the wire — they are the digest's <c>fields</c> — so counting uses there
    /// would put a number in every recorded board that the recording does not
    /// have. A lasting effect is not digested, and it expires at the end of the
    /// round without anything having to remember to clear it.
    /// </para>
    /// </remarks>
    private bool Available(
        World world, Card card, CardAbility ability, Occurrence? occurrence = null)
    {
        if (ability.Limit is { } limit
            && world.Effects.Active().Count(effect =>
                effect.Card == card.ObjectId
                && string.Equals(
                    effect.Kind, Spent(card, ability), StringComparison.Ordinal)) >= limit)
        {
            return false;
        }

        if (ability.Maximum is not { } maximum)
        {
            return true;
        }
        if (maximum.Period == MaximumPeriod.Instance && occurrence is null)
        {
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' has a per-instance maximum outside an occurrence window");
        }

        string key = MaximumSpent(world, card, maximum.Period, occurrence);
        return world.Effects.Active().Count(effect =>
            string.Equals(effect.Kind, key, StringComparison.Ordinal)) < maximum.Uses;
    }

    /// <summary>Records one use of a limited ability, until the round ends.</summary>
    private void Use(
        World world, Card card, CardAbility ability, Occurrence? occurrence = null)
    {
        if (ability.Limit is not null)
        {
            world.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect,
                Kind: Spent(card, ability),
                Card: card.ObjectId,
                Affects: card.ObjectId,
                Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound)));
        }

        if (ability.Maximum is { } maximum)
        {
            Duration lasts = maximum.Period switch
            {
                MaximumPeriod.Round => Duration.UntilEndOf(TimingPoints.EndOfRound),
                MaximumPeriod.Phase => Duration.UntilEndOf(TimingPoints.EndOfPhase),
                MaximumPeriod.Game or MaximumPeriod.Instance => Duration.WhileInPlay,
                _ => throw new ArgumentOutOfRangeException(nameof(ability)),
            };
            world.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect,
                Kind: MaximumSpent(world, card, maximum.Period, occurrence),
                Card: card.ObjectId,
                Lasts: lasts));
        }
    }

    private static string MaximumSpent(
        World world, Card card, MaximumPeriod period, Occurrence? occurrence)
    {
        string title = world.Facts.Title(card.FaceId);
        string instance = period == MaximumPeriod.Instance
            ? ":" + (occurrence?.Id
                ?? throw new RulesNotImplementedException(
                    $"'{card.FaceId}' has a per-instance maximum without an occurrence"))
                .ToString(System.Globalization.CultureInfo.InvariantCulture)
            : string.Empty;
        return $"maximum:{period}:{title}{instance}";
    }

    /// <summary>The effect kind that stands for one use of this instance of an ability.</summary>
    private string Spent(Card card, CardAbility ability)
    {
        var written = On(card).ToList();
        int ordinal = written.FindIndex(candidate => ReferenceEquals(candidate, ability));
        if (ordinal < 0)
        {
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' used an ability that is not on its current face");
        }

        // `rr:limit` counts "per instance of that ability". The engine chooses
        // the printed face plus its ordinal as the stable ability identity;
        // names are display text and two unnamed abilities default to the same
        // card name. Face matters because two sides can each have ordinal zero.
        // `rr:leaves-play.1` makes a returning card a new copy with no memory,
        // so its in-play incarnation is part of the key as well.
        return "spent:"
            + card.Incarnation.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ":"
            + ability.Card
            + ":"
            + ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>What letters an effect generates, if it only generates.</summary>
    private static string Generated(AbilityNode effect, World world, int player)
    {
        if (effect.Kind == "generate")
        {
            return Word(effect.Argument);
        }

        if (effect.Kind == "generateTopDiscard")
        {
            var cards = world.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player).Cards;
            return cards.Count > 0
                ? Resources.GeneratedBy(cards[^1].FaceId, world.Facts)
                : string.Empty;
        }

        throw new RulesNotImplementedException(
            $"a resource ability whose effect is '{effect.Kind}' generates nothing this "
            + "engine can read");
    }

    /// <inheritdoc/>
    public string UseResource(World world, int player, int card, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);

        var holder = world.Cards[card];
        var ability = On(holder).FirstOrDefault(candidate =>
        {
            var eligibility = new Cast(
                world,
                holder,
                new Occurrence(
                    0, [Steps.TurnAction], Subject: holder.ObjectId, Player: player),
                player,
                [],
                this);
            return candidate.Trigger.Timing == AbilityType.Resource
                && MayInitiate(world, candidate, holder, player)
                && Available(world, holder, candidate)
                && InForm(world, player, candidate.Trigger.Form)
                && Payable(world, holder, player, candidate.Cost)
                && (candidate.When is null || Test(candidate.When, eligibility));
        })
            ?? throw new RulesNotImplementedException(
                $"card {card} has no resource ability left to use this round");

        var cast = new Cast(
            world, holder,
            new Occurrence(0, [Steps.TurnAction], Subject: holder.ObjectId, Player: player),
            player, events, this)
        {
            Tier = ability.Trigger.Timing,
        };
        Pay(ability.Cost, [], [], cast);
        Use(world, holder, ability);
        return Generated(ability.Effect, world, player);
    }

    /// <inheritdoc/>
    public bool CanTakeDamage(World world, Card target, Card source)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        if (!DeckTypes.IsInPlay(target.Area.Type))
        {
            return true;
        }

        foreach (var ability in On(target).Where(ability =>
            ability.Trigger.Timing == AbilityType.Constant))
        {
            var cast = new Cast(
                world, target, new Occurrence(0, []), ControllerOf(world, target), [], this);
            if (ProhibitsDamage(ability.Effect, cast, source))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public DamageProjection PreviewDamageReplacement(
        World world, Card target, Card source, long amount)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        var occurrence = new Occurrence(
            0, [Steps.DamageWouldBeDealt], Subject: target.ObjectId, Player: target.Owner);
        foreach (PendingAbility pending in Waiting(world, occurrence, WindowKind.Interrupt)
                     .Where(candidate => candidate.Type == AbilityType.ForcedInterrupt))
        {
            Card card = world.Cards[pending.Card];
            CardAbility ability = Pending(card, pending);
            string name = world.Facts.Title(card.FaceId);
            if (ContainsEffect(ability.Effect, "soakDamage"))
            {
                long threshold = SoakDiscardThreshold(ability.Effect);
                bool discarded = threshold > 0
                    && SaturatingSum(card.Damage, [amount]) >= threshold;
                return new DamageProjection(
                    0,
                    $"{name} takes the damage instead"
                    + (discarded ? " and will be discarded" : string.Empty));
            }

            return new DamageProjection(
                null, $"{name} has a forced interrupt that modifies this damage");
        }

        return new DamageProjection(amount);
    }

    /// <inheritdoc/>
    public DefeatProjection? PreviewDefeatReplacement(
        World world, Card target, long maximumHealth)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);

        var occurrence = new Occurrence(
            0, [Steps.CardWouldBeDefeated], Subject: target.ObjectId, Player: target.Owner);
        foreach (PendingAbility pending in Waiting(world, occurrence, WindowKind.Interrupt)
                     .Where(candidate => candidate.Type == AbilityType.ForcedInterrupt))
        {
            Card card = world.Cards[pending.Card];
            CardAbility ability = Pending(card, pending);
            string name = world.Facts.Title(card.FaceId);
            if (HealsAllDamage(ability.Effect))
            {
                return new DefeatProjection(
                    maximumHealth,
                    $"{name} heals all damage instead"
                    + (ContainsEffect(ability.Effect, "discard")
                        ? " and will be discarded"
                        : string.Empty));
            }
            return new DefeatProjection(
                null, $"{name} has a forced interrupt before defeat");
        }
        return null;
    }

    private static bool HealsAllDamage(AbilityNode node) =>
        node.Kind == "heal"
        && Tree(node.Require("amount")).Kind == "damageOn"
        || MutationChildren(node).Any(HealsAllDamage);

    /// <inheritdoc/>
    public bool CanReady(World world, Card target, Card source)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        foreach (var card in world.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards))
        {
            foreach (var ability in On(card).Where(ability =>
                ability.Trigger.Timing == AbilityType.Constant))
            {
                var cast = new Cast(
                    world, card, new Occurrence(0, []), ControllerOf(world, card), [], this);
                if (ProhibitsReady(ability.Effect, cast, target))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool ProhibitsReady(AbilityNode node, Cast cast, Card target) =>
        node.Kind switch
        {
            "seq" or "and" => Nodes(node.Argument).Any(step =>
                ProhibitsReady(step, cast, target)),
            "if" => node.Field(Test(Tree(node.Require("test")), cast) ? "then" : "else")
                is { } branch && ProhibitsReady(Tree(branch), cast, target),
            "preventReady" => Find(node.Argument, cast)?.ObjectId == target.ObjectId,
            _ => false,
        };

    private static bool ProhibitsDamage(AbilityNode node, Cast cast, Card source) =>
        node.Kind switch
        {
            "seq" or "and" => Nodes(node.Argument)
                .Any(step => ProhibitsDamage(step, cast, source)),
            "if" => node.Field(Test(Tree(node.Require("test")), cast) ? "then" : "else")
                is { } branch && ProhibitsDamage(Tree(branch), cast, source),
            "preventDamageFrom" => cast.World.Facts.Kind(source.FaceId)
                    == Kind(Word(node.Require("sourceKind")))
                && Rules.State.Traits.Has(
                    cast.World, source, Word(node.Require("sourceTrait")), cast.World.Facts),
            "preventDamageWhile" => Test(Tree(node.Require("condition")), cast),
            _ => false,
        };

    /// <inheritdoc/>
    public long WouldBeDealt(
        World world, Card target, Card source, long amount, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);

        if (amount <= 0)
        {
            return amount;
        }

        var occurrence = new Occurrence(
            0, [Steps.DamageWouldBeDealt], Subject: target.ObjectId, Player: target.Owner);

        long left = amount;
        foreach (var (card, ability) in Waiting(world, occurrence))
        {
            // **Forced only.** `rr:ability.11` makes everything optional unless
            // prefaced by "Forced", and an optional interrupt is a question --
            // which needs a window, which dealing damage has not got. A card
            // that would ask here is refused by name rather than resolved
            // without asking.
            if (ability.Trigger.Timing != AbilityType.ForcedInterrupt)
            {
                // Optional interrupts are offered by the agenda before attack
                // damage is applied. A direct damage call has no window, so it
                // cannot trigger one and must not resolve it on the player's
                // behalf.
                continue;
            }

            var cast = new Cast(world, card, occurrence, target.Owner, events, this)
            {
                Incoming = left,
                Tier = ability.Trigger.Timing,
            };

            TrackResolution(cast, ability);
            Run(ability, cast);
            cast.CompleteResolution();

            // An ability that touched the damage says so; one that did nothing
            // to it leaves it alone. `rr:damage.step.1` holds abilities that
            // *may* replace the damage, not ones that must.
            left = cast.Remaining < 0 ? left : cast.Remaining;
            if (left <= 0)
            {
                // `rr:replacement-effect.1` -- "when an effect is replaced, it
                // is no longer considered imminent and no further interrupts or
                // responses to that effect can be triggered."
                return 0;
            }
        }

        return left;
    }

    /// <inheritdoc/>
    public long WouldTake(
        World world, Card target, Card source, long amount, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(events);

        var prevention = world.Effects.Active().FirstOrDefault(effect =>
            string.Equals(effect.Kind, "preventDamage", StringComparison.Ordinal)
            && effect.Affects == target.ObjectId);
        if (prevention is null || !world.Effects.Use(prevention))
        {
            return amount;
        }

        long prevented = prevention.Amount <= 0 ? amount : prevention.Amount;
        return Math.Max(0, amount - prevented);
    }

    /// <inheritdoc/>
    public void DamagePreventedByTough(
        World world, Card target, Card source, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(events);

        var prevention = world.Effects.Active().Where(effect =>
            string.Equals(effect.Kind, "preventDamage", StringComparison.Ordinal)
            && effect.Affects == target.ObjectId).ToList();
        foreach (var effect in prevention)
        {
            world.Effects.Use(effect);
        }
    }

    /// <inheritdoc/>
    public void WouldBeDefeated(World world, Card target, List<GameEvent> events)
    {
        _ = WouldBeDefeated(
            world, target, target, Steps.CardWouldBeDefeated,
            Steps.CardWouldBeDefeated, -1, events);
    }

    /// <inheritdoc/>
    public bool WouldBeDefeated(
        World world, Card target, Card source, string trigger, string verb, int by,
        List<GameEvent> events, Occurrence? recordDefeatOn = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);

        var occurrence = new Occurrence(
            0, [Steps.CardWouldBeDefeated], Subject: target.ObjectId, Player: target.Owner);
        var spent = world.Agenda.Occurrence;

        while (AbilityWindow.Tiers(
            Waiting(world, occurrence, WindowKind.Interrupt)
                .Where(pending => spent?.MayTrigger(WindowKind.Interrupt, pending.Card) ?? true),
            WindowKind.Interrupt,
            occurrence) is { Count: > 0 } tiers)
        {
            var (mandatory, optional) = AbilityWindow.Split(tiers[0]);
            if (mandatory.Count == 0)
            {
                SuspendWouldBeDefeated(
                    world, target, source, trigger, verb, by, occurrence, optional,
                    recordDefeatOn);
                return false;
            }

            if (mandatory.Count > 1)
            {
                SuspendWouldBeDefeated(
                    world, target, source, trigger, verb, by, occurrence, mandatory,
                    recordDefeatOn);
                return false;
            }

            occurrence.Trigger(WindowKind.Interrupt, mandatory[0].Card);
            spent?.Trigger(WindowKind.Interrupt, mandatory[0].Card);
            events.AddRange(Resolve(world, occurrence, mandatory[0], [], []));

            // `rr:would.1`: once the interrupt changes the imminent defeat,
            // no later interrupt to that original condition may be used.
            if (Damage.Health(world, world.Facts, target) - target.Damage > 0)
            {
                return true;
            }
        }

        return true;
    }

    private static void SuspendWouldBeDefeated(
        World world, Card target, Card source, string trigger, string verb, int by,
        Occurrence occurrence, IReadOnlyList<PendingAbility> pending,
        Occurrence? recordDefeatOn)
    {
        var step = new PhaseStep(
            Steps.ChooseWouldBeDefeated,
            world.Agenda.Current?.Round ?? 0,
            6,
            Subject: target.ObjectId,
            Seat: target.Owner >= 0 ? target.Owner : world.FirstPlayer,
            Plan: true,
            ProcedureAbilities: [.. pending],
            ProcedureOccurrence: occurrence,
            ProcedureOwnerOccurrence: recordDefeatOn,
            ProcedureSource: source.ObjectId,
            ProcedureTrigger: trigger,
            ProcedureVerb: verb,
            ProcedureBy: by);

        if (world.Agenda.Occurrence is { } parent)
        {
            world.Agenda.ThenContinuation(step, parent);
            world.Agenda.BeforeResponses(parent);
        }
        else
        {
            world.Agenda.Add(step);
        }
    }

    /// <summary>Every authored ability answering one occurrence, with its card.</summary>
    /// <remarks>
    /// <b>Gathered before any of it runs.</b> An ability can make an area —
    /// giving a status card creates one to hold it — and walking
    /// <c>World.Areas</c> lazily while resolving would be modifying the
    /// collection being read.
    /// </remarks>
    private List<(Card Card, CardAbility Ability)> Waiting(World world, Occurrence what) =>
    [
        .. world.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .ToList()
            .SelectMany(card => On(card)
                .Where(ability => Answers(world, ability, card, what))
                .Select(ability => (Card: card, Ability: ability)))
            .ToList(),
    ];

    /// <summary>Whether one ability answers this occurrence at all.</summary>
    private bool Answers(
        World world, CardAbility ability, Card card, Occurrence what)
    {
        int? restricted = RestrictedPlayer(world, ability, card);
        return ability.Trigger.Event is { } condition
            && what.Conditions.Contains(condition, StringComparer.Ordinal)
            && Subject(world, ability.Trigger.Subject, card, what, restricted)
            && Role(world, ability.Trigger.Actor, card, what.ActorFacts, restricted)
            && Role(world, ability.Trigger.Target, card, what.TargetFacts, restricted)
            && Player(world, ability.Trigger.Player, card, what, restricted);
    }

    /// <inheritdoc/>
    public int? AttachesTo(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        if (book.Attaches(card.FaceId) is not { } element)
        {
            return null;
        }

        // No occurrence and no events, for the reason `Constant` has none:
        // this is asked while a card is being placed, so it answers a question
        // and does not act on the answer. `Find` reads the board and nothing
        // else.
        var candidates = Every(
            element,
            new Cast(world, card, new Occurrence(0, []), card.Owner, [], this));

        return candidates.Count switch
        {
            0 => null,
            1 => candidates[0].ObjectId,
            _ => throw new RulesNotImplementedException(
                $"'{card.FaceId}' can attach to {candidates.Count} equally eligible cards. "
                + "rr:first-player.1 gives that choice to the first player, and attaching "
                + "during a reveal has no target prompt yet"),
        };
    }

    /// <inheritdoc/>
    public int? SetupController(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        return book.FirstPlayerControls(card.FaceId) ? world.FirstPlayer : null;
    }

    /// <inheritdoc/>
    public void ValidateForPlay(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var incomplete = world.Cards.FirstOrDefault(card =>
            DeckTypes.IsInPlay(card.Area.Type) && book.IsPlacementOnly(card.FaceId));
        if (incomplete is not null)
        {
            throw new RulesNotImplementedException(
                $"card '{incomplete.FaceId}' is in play, but only its setup placement "
                + "and absence of a When Revealed ability are implemented; its remaining "
                + "printed text is not implemented");
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<int>? AttachmentTargets(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        if (book.Attaches(card.FaceId) is not { } element)
        {
            return null;
        }

        return [.. Every(element, new Cast(
            world, card, new Occurrence(0, []), card.Owner, [], this))
            .Select(candidate => candidate.ObjectId)];
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Setup(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        if (!book.Authored.Contains(card.FaceId))
        {
            // The same distinction `WhenRevealed` makes, and setup is where it
            // matters most: a scenario whose main scheme nobody has read would
            // otherwise deal a board that is quietly missing whatever its first
            // card said, and every later assertion would be about the wrong
            // game.
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' is being set up and no ability data is written for it; "
                + $"this engine has {book.Authored.Count} authored card(s)");
        }

        var events = new List<GameEvent>();

        // `rr:setup-triggered-ability.2` times these to a step of setup rather
        // than to anything happening, so `Steps.Setup` is the step's name and
        // not a triggering condition -- no card can name it, because the reader
        // refuses an `event` on a Setup ability. What it is for is the events:
        // a board built during setup is told apart in the stream from one built
        // during a round.
        //
        // There is no player whose turn it is either. The card's owner resolves
        // it, which for an encounter card is the scenario.
        var occurrence = new Occurrence(
            0, [Steps.Setup], Subject: card.ObjectId, Player: card.Owner);

        foreach (var ability in On(card))
        {
            if (ability.Trigger.Timing == AbilityType.Setup)
            {
                var cast = new Cast(world, card, occurrence, card.Owner, events, this)
                {
                    Tier = ability.Trigger.Timing,
                };
                TrackResolution(cast, ability);
                Run(ability, cast);
                cast.CompleteResolution();
            }
        }

        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<Card> PlayerSetupCards(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentOutOfRangeException.ThrowIfNegative(player);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(player, world.Players);

        return
        [
            .. world.Areas
                .Where(area => DeckTypes.IsInPlay(area.Type)
                    && area.PlayArea == PlayArea.Of(player))
                .SelectMany(area => area.Cards)
                .Where(card => ControllerOf(world, card) == player
                    && On(card).Any(
                        ability => ability.Trigger.Timing == AbilityType.Setup)),
        ];
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> ResolveEachPlayer(
        World world, Card source, int player, int stoppedAt,
        AbilityType? tier, bool finalStep, bool finalPlayer)
    {
        var step = world.Agenda.Current;
        var written = AbilitiesOn(source, step?.AbilityFace)
            .Where(ability => tier is null || ability.Trigger.Timing == tier)
            .ToList();
        int ordinal = step is { What: Steps.ResolveEachPlayer, AbilityOrdinal: >= 0 }
            ? step.Value.AbilityOrdinal
            : written.FindIndex(ability => EachPlayers(ability.Effect).Any());
        var outer = written.ElementAtOrDefault(ordinal)?.Effect
            ?? throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no reconstructable each-player ability");
        var parentPath = step is { What: Steps.ResolveEachPlayer, AbilityPath: { } path }
            ? path
            : outer.Kind == "seq" ? [$"seq:{stoppedAt - 1}"] : [];
        var each = NodeAtPath(outer, parentPath);
        if (each.Kind != "eachPlayer")
        {
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no each-player frame at step {stoppedAt - 1}");
        }

        var cast = Resolving(
            world, source, player, tier, finalStep, step?.AbilityOccurrence) with
        {
            EachPlayerFrame = true,
            FinalPlayer = finalPlayer,
            AbilityPlayer = step?.AbilityPlayer ?? player,
            GainedKeywords = world.Agenda.Current is
                { What: Steps.ResolveEachPlayer, SurgeGained: true }
                    ? new HashSet<string>(["surge"], StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal),
        };
        RestorePersisted(cast, step);
        cast.RestoreAbility(
            ordinal, [.. parentPath, "eachPlayer:effect"], step?.AbilityFace);
        cast.TrackResolution(ordinal);
        RestorePathBindings(cast, parentPath);
        cast.At(stoppedAt - 1);
        cast.SetContinuation(finalPlayer && (step?.AbilityHasContinuation
            ?? (outer.Kind == "seq" && stoppedAt < Nodes(outer.Argument).Count())));
        Run(Tree(each.Require("effect")), cast);
        if (!cast.Suspended && finalPlayer)
        {
            cast.SetAbilityPath(parentPath);
            cast.RestorePlayer(cast.AbilityPlayer);
            cast.SetContinuation(false);
            ResumeAfter(outer, parentPath, cast);
        }
        cast.CompleteResolution();
        return cast.Events;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>A reader, not a runner.</b> Everything else on this class resolves an
    /// ability once, at a moment, and records what it did. A constant ability
    /// has no moment: <c>rr:ability</c> makes it active "as soon as its card
    /// enters play" and keeps it active "while the card is in play", so the
    /// question is never "what did it do" but "what is it doing". The answer is
    /// worked out afresh whenever anything reads the effect list, which is what
    /// <c>rr:modifiers</c> describes the game as doing continuously.
    /// </para>
    /// <para>
    /// So this shares the interpreter's tests and amounts and none of its
    /// verbs. <c>Grants</c> walks <c>seq</c> and <c>if</c> and stops at
    /// <c>grant</c>; there is no route from here to anything that moves a card
    /// or deals damage, which is what makes it safe to call from inside the
    /// rules rather than between them.
    /// </para>
    /// </remarks>
    public IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        // A player's printed card is blank while Ultron uses it facedown as a
        // Drone minion. Its face id remains underneath for the digest, so the
        // interpreter must use the runtime card kind rather than mistake that
        // id for active player-card text.
        if (!constant.Contains(card.FaceId))
        {
            return [];
        }

        var found = new List<ContinuousEffect>();
        foreach (var ability in On(card))
        {
            if (ability.Trigger.Timing != AbilityType.Constant)
            {
                continue;
            }

            // No occurrence, because there is none: a constant ability is not
            // timed to anything. The empty event list is never written to --
            // nothing `Grants` reaches records anything -- and the card's
            // current controller stands in for the resolving player, which for
            // an encounter card is the scenario.
            Grants(
                ability.Effect,
                new Cast(
                    world, card, new Occurrence(0, []), ControllerOf(world, card), [], this),
                found);
        }

        return found;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> WhenCardDefeated(World world, Card card, Defeated defeated)
    {
        var events = new List<GameEvent>();
        _ = WhenCardDefeated(world, card, defeated, Steps.CardDefeated, events);
        return events;
    }

    /// <inheritdoc/>
    public bool WhenCardDefeated(
        World world, Card card, Defeated defeated, string trigger,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(defeated);

        var written = On(card)
            .Where(ability => ability.Trigger.Timing == AbilityType.WhenDefeated)
            .ToList();

        // **The printed check gates the complaint, not the run.** Nothing in
        // the printed attributes records a "When Defeated", so an unwritten one
        // and a card that has none look identical from here -- but that is only
        // a question when there is nothing written. Asking it first would let
        // the text box veto authored data, which is the wrong way round: the
        // data is what the engine runs.
        if (written.Count == 0 && world.Facts.HasWhenDefeated(card.FaceId))
        {
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' was defeated and prints a 'When Defeated' "
                + "ability that no ability data is written for");
        }

        // **Two occurrences, and each is asked what only it can answer.**
        //
        // This one is built here because the matching needs the defeated card:
        // "when **attached minion** is defeated" is a claim about which card
        // died, while the occurrence the defeat joined keeps the cause. An
        // attack carries its actor and target separately. This occurrence also
        // carries the provenance because "the player who defeated this scheme"
        // is on the card and not on the board.
        //
        // What it cannot answer is `rr:triggering-condition.1`, "each
        // **Interrupt** ability can only be triggered once per occurrence of
        // its triggering condition". The occurrence there is the one on the
        // agenda: it is what lasts, it is what a still-open interrupt window is
        // polling, and it is where a second defeat in the same moment would
        // find an ability already spent. This one is made fresh on every call
        // and would forget all of that.
        var occurrence = new Occurrence(
            0, [Steps.CardDefeated], Subject: card.ObjectId, Player: card.Owner);
        occurrence.Also(defeated);

        var spent = world.Agenda.Occurrence;
        var elsewhere = Answering(world, card, occurrence, spent);
        if (elsewhere.Count == 0)
        {
            // `rr:when-defeated-abilities.2` says all abilities on the defeated
            // card resolve. Their printed/data order is already authoritative;
            // the cross-card ordering question from `rr:forced.5` does not
            // arise until another card answers the same defeat.
            foreach (var ability in written)
            {
                var cast = new Cast(world, card, occurrence, card.Owner, events, this)
                {
                    Tier = ability.Trigger.Timing,
                };
                TrackResolution(cast, ability);
                Run(ability, cast);
                cast.CompleteResolution();
            }
            return true;
        }

        var own = written.Select((_, ordinal) => new PendingAbility(
            card.ObjectId, AbilityType.WhenDefeated, card.Owner, ordinal));
        var waiting = own.Concat(elsewhere).ToList();
        while (waiting.Count > 0)
        {
            var mandatory = waiting
                .Where(ability => AbilityTypes.IsMandatory(ability.Type))
                .ToList();
            var offered = mandatory.Count > 0
                ? mandatory
                : waiting.Where(ability => !AbilityTypes.IsMandatory(ability.Type)).ToList();
            if (offered.Count > 1 || mandatory.Count == 0)
            {
                SuspendCardDefeated(
                    world, card, trigger, occurrence, defeated, offered);
                return false;
            }

            var next = offered[0];
            occurrence.Trigger(WindowKind.Interrupt, next.Card);
            spent?.Trigger(WindowKind.Interrupt, next.Card);
            events.AddRange(Resolve(world, occurrence, next, [], []));
            waiting.Remove(next);
        }

        return true;
    }

    private static void SuspendCardDefeated(
        World world, Card card, string trigger, Occurrence occurrence,
        Defeated defeated, IReadOnlyList<PendingAbility> pending)
    {
        occurrence.Also(defeated);
        var step = new PhaseStep(
            Steps.ChooseCardDefeatedAbility,
            world.Agenda.Current?.Round ?? 0,
            7,
            Subject: card.ObjectId,
            Seat: card.Owner >= 0 ? card.Owner : world.FirstPlayer,
            Plan: true,
            ProcedureAbilities: [.. pending],
            ProcedureOccurrence: occurrence,
            ProcedureTrigger: trigger,
            ProcedureVerb: defeated.How,
            ProcedureBy: defeated.By);

        if (world.Agenda.Occurrence is { } parent)
        {
            world.Agenda.ThenContinuation(step, parent);
            world.Agenda.BeforeResponses(parent);
        }
        else
        {
            world.Agenda.Add(step);
        }
    }

    /// <summary>
    /// The forced interrupts on <i>other</i> cards that answer this defeat —
    /// <c>rr:damage.step.7</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The earliest tier with anything in it, because <c>rr:forced.4</c> orders
    /// the tiers and a later one does not initiate while an earlier one is
    /// still waiting. A status card's forced interrupt is its own tier ahead of
    /// the rest — <c>rr:ability.step.2.a</c>.
    /// </para>
    /// <para>
    /// A non-forced interrupt is returned as the earliest waiting tier. The
    /// rules layer persists that tier as a procedure continuation and offers it
    /// without moving the defeated card or replaying the damage.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="card">The card that was defeated.</param>
    /// <param name="occurrence">The defeat, which is what an ability matches against.</param>
    /// <param name="spent">
    /// The occurrence on the agenda, which is what remembers what has already
    /// fired — <c>rr:triggering-condition.1</c>. Null when a caller reached
    /// this without anything happening on the agenda.
    /// </param>
    private IReadOnlyList<PendingAbility> Answering(
        World world, Card card, Occurrence occurrence, Occurrence? spent)
    {
        var tiers = AbilityWindow.Tiers(
            Waiting(world, occurrence, WindowKind.Interrupt)
                .Where(pending => pending.Card != card.ObjectId)
                .Where(pending => spent?.MayTrigger(WindowKind.Interrupt, pending.Card) ?? true),
            WindowKind.Interrupt,
            occurrence);

        foreach (var tier in tiers)
        {
            var (mandatory, optional) = AbilityWindow.Split(tier);
            if (mandatory.Count > 0 || optional.Count > 0)
            {
                return mandatory.Count > 0 ? mandatory : optional;
            }
        }

        return [];
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Act(
        World world, PendingAbility ability, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values = null,
        IReadOnlyList<ResourceAllocation>? allocations = null)
        => Act(
            world, ability, paying, chosen,
            new Occurrence(
                0, [Steps.TurnAction], Subject: ability.Card, Player: ability.Player),
            values, allocations);

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Act(
        World world, PendingAbility ability, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen, Occurrence occurrence,
        IReadOnlyDictionary<string, long>? values = null,
        IReadOnlyList<ResourceAllocation>? allocations = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(paying);
        ArgumentNullException.ThrowIfNull(chosen);
        ArgumentNullException.ThrowIfNull(occurrence);

        var card = world.Cards[ability.Card];
        var found = Pending(card, ability);

        var events = new List<GameEvent>();
        var cast = new Cast(
            world,
            card,
            occurrence,
            ability.Player,
            events,
            this)
        {
            Tier = found.Trigger.Timing,
        };

        // The wire only names an affordance; it is not authority to use one
        // after its legality has changed. Re-run the same initiation checks
        // that produced Actions before any cost moves a card or exhausts a
        // game element. `rr:cost.6` and `rr:event.3` make target availability
        // part of whether the cost may be paid at all.
        if (!ActionAvailable(world, card, found, ability.Player, cast))
        {
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' cannot initiate this action in the current state");
        }
        if (!CounterCostsPayable(world, card, ability.Player, found.Cost))
        {
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' can no longer pay this ability's counter cost");
        }

        // `rr:initiating-abilities` keeps the steps apart, and step 5 pays
        // before step 6 resolves.
        ValidatePayment(found.Cost, paying, chosen, values, cast);
        PayEvent(card, paying, cast, found.Effect, allocations, found.Cost);
        if (world.Facts.Kind(card.FaceId) == CardKind.Event
            && ResourceRequirement(found.Cost, card).Length > 0)
        {
            PayNonResourceCosts(found.Cost, paying, chosen, values, cast);
        }
        else
        {
            Pay(found.Cost, paying, chosen, values, cast);
        }
        if (cast.Suspended)
        {
            SuspendAfterCost(cast, ability.Ordinal);
            return events;
        }
        Use(world, card, found, occurrence);
        if (world.Facts.Kind(card.FaceId) == CardKind.Event)
        {
            occurrence.BeginCard(card.ObjectId, [ability]);
        }
        cast.RestoreAbility(ability.Ordinal, []);
        cast.TrackResolution(ability.Ordinal);
        Run(found, cast);
        cast.CompleteResolution();
        DiscardEvent(card, cast);
        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<PendingAbility> Actions(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);

        var found = new List<PendingAbility>();
        foreach (var card in Triggerable(world, player).ToList())
        {
            var written = On(card).ToList();
            for (int index = 0; index < written.Count; index++)
            {
                var ability = written[index];
                if (ability.Trigger.Timing is not (AbilityType.Action or AbilityType.ForcedAction)
                    || !MayInitiate(world, ability, card, player))
                {
                    continue;
                }

                var eligibility = new Cast(world, card, new Occurrence(
                    0, [Steps.TurnAction], Subject: card.ObjectId, Player: player),
                    player, [], this);
                if (ActionAvailable(world, card, ability, player, eligibility)
                    && Payable(world, card, player, ability.Cost)
                    && EventPayable(world, card, player, ability))
                {
                    int ordinal = written.Take(index).Count(candidate =>
                        candidate.Trigger.Timing == ability.Trigger.Timing);
                    found.Add(new PendingAbility(
                        card.ObjectId, ability.Trigger.Timing, player, ordinal));
                }
            }
        }

        return found;
    }

    /// <summary>Whether an action can pass every initiation check right now.</summary>
    private bool ActionAvailable(
        World world, Card card, CardAbility ability, int player, Cast eligibility)
    {
        eligibility.SetPaymentMayMutate(
            ability.Cost is not null || world.Facts.Kind(card.FaceId) == CardKind.Event,
            ability.Cost);
        return MayInitiate(world, ability, card, player)
        && Available(world, card, ability)
        && InForm(world, player, ability.Trigger.Form)
        && (ability.When is null || Test(ability.When, eligibility))
        && CanInitiate(ability, eligibility);
    }

    /// <summary>The exact same-timing ability named by a pending ordinal.</summary>
    private CardAbility Pending(Card card, PendingAbility pending) =>
        On(card)
            .Where(candidate => candidate.Trigger.Timing == pending.Type)
            .ElementAtOrDefault(pending.Ordinal)
        ?? throw new AbilityException(
            $"card '{card.FaceId}' has no '{pending.Type}' ability at ordinal "
            + pending.Ordinal);

    /// <summary>
    /// The cards one player may trigger an action on —
    /// <c>rr:player-turn.5</c>'s four places.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>.a</c> "a card in play they control", <c>.b</c> "an encounter card in
    /// play", <c>.d</c> "an event card in their hand <i>(by playing that
    /// event)</i>". <c>.c</c> — "any card in play with text that allows that
    /// player to trigger its action ability" — is a card's own text and belongs
    /// to whichever card says it, so there is nothing general to write here.
    /// </para>
    /// <para>
    /// <b>An event is reached from the hand and nowhere else.</b> That is why
    /// <c>CardPlay.Price</c> refuses to offer one: an event is not
    /// <c>rr:player-turn.2</c>'s "ally, upgrade, support, or player side
    /// scheme", it is played by triggering its action.
    /// </para>
    /// </remarks>
    private IEnumerable<Card> Triggerable(World world, int player)
    {
        foreach (var area in world.Areas)
        {
            if (!DeckTypes.IsInPlay(area.Type))
            {
                continue;
            }

            foreach (var card in area.Cards)
            {
                // `.a` and `.b`: yours, or nobody's. A card another player
                // controls is theirs to trigger -- `rr:player-turn.6` is how
                // you ask them.
                if (ControllerOf(world, card) == player
                    || card.Owner == World.Scenario
                    || On(card).Any(ability => ability.AnyPlayer))
                {
                    yield return card;
                }
            }
        }

        // `.d`, and only events: an ally in hand is played rather than
        // triggered, and `rr:player-turn.2` is where that happens.
        foreach (var card in world.Seats[player].Hand.Cards)
        {
            if (world.Facts.Kind(card.FaceId) == CardKind.Event)
            {
                yield return card;
            }
        }
    }

    /// <summary>
    /// Whether the player is in the form an ability requires —
    /// <c>rr:player-turn.5.1</c>.
    /// </summary>
    private static bool InForm(World world, int player, string? form) =>
        form is null || Forms.In(world, world.Seats[player], world.Facts, form);

    /// <summary>
    /// The same question asked of an ability in a window, where the seat may be
    /// nobody's.
    /// </summary>
    /// <remarks>
    /// A form is a property of an identity, and an ability offered to every
    /// player at once is not offered to an identity. A card that means one seat
    /// says so on its trigger; one that names a form without naming a seat has
    /// asked a question about nobody.
    /// </remarks>
    private static bool InForm(World world, int player, string? form, Card card)
    {
        if (form is null)
        {
            return true;
        }

        return player >= 0
            ? InForm(world, player, form)
            : throw new RulesNotImplementedException(
                $"'{card.FaceId}' requires '{form}' form and is offered to every player, "
                + "so there is no identity whose form to read");
    }

}
