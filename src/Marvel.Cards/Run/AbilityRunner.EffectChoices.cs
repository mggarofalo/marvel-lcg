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

    /// <summary>"… heals N damage" — <c>rr:heal</c>.</summary>
    /// <remarks>
    /// <para>
    /// What it records is the point. <c>rr:heal</c> heals up to the amount, and
    /// a character at full health or damaged by less heals less than it was
    /// told to — so <c>result.healed</c> is what actually moved, and a card
    /// reading "if no damage was healed this way" reads that rather than
    /// checking the character's health first. The check <i>before</i> is
    /// silently wrong: it reads a number the heal may never reach.
    /// </para>
    /// <para>
    /// A target that is not on the board heals nothing rather than throwing.
    /// "Rhino heals 4 damage. If no damage was healed this way, this card gains
    /// surge" is a sentence with an answer for the absent villain, and it is
    /// the surge.
    /// </para>
    /// </remarks>
    private static void Heal(AbilityEffect.Heal heal, Cast cast)
    {
        long healed = Find(heal.Card, cast) is { } target
            ? Damage.Heal(
                cast.World, cast.World.Facts, target, Amount(heal.Amount, cast),
                cast.Trigger, "Heal", cast.Events)
            : 0;

        cast.Results["healed"] = healed;
    }

    /// <summary>
    /// "Assign N damage among …" — <c>rr:indirect-damage</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>.1</c>: "indirect damage dealt to a player can be divided as that
    /// player chooses among characters under their control." <c>.2</c> is the
    /// group form, "among friendly characters in play", which is what "assign X
    /// damage among heroes and allies" means.
    /// </para>
    /// <para>
    /// <b>Only asked when there is something to ask.</b> A player with no ally
    /// has one character, so every point goes to their identity and there is no
    /// division to choose — which is most of the 101 cards in the pool that
    /// deal indirect damage. It suspends only when the eligible characters can
    /// hold the damage more than one way.
    /// </para>
    /// <para>
    /// <c>.3.1</c> caps each character at its remaining hit points: "a
    /// character cannot be assigned more indirect damage than would cause it to
    /// be defeated", assessed "without accounting for interactions with other
    /// abilities". <c>.3.2</c> keeps a tough character eligible up to that same
    /// cap even though the tough card will prevent all of it, and <c>.3</c>
    /// assigns everything before resolving any of it.
    /// </para>
    /// </remarks>
    private static void Indirect(AbilityEffect.IndirectDamage damage, AbilityEffect node, Cast cast)
    {
        long amount = Amount(damage.Amount, cast);
        var eligible = Assignable(damage.Among, cast);

        if (amount <= 0 || eligible.Count == 0)
        {
            return;
        }

        if (eligible.Count == 1)
        {
            // No division to choose. `.3.1`'s cap still applies -- a character
            // cannot be assigned more than would defeat it -- so what is over
            // the cap is simply not assigned.
            Assign(node, cast, [eligible[0]], amount);
            return;
        }

        SuspendForChoice(node, cast);
    }

    /// <summary>The characters indirect damage may be assigned to.</summary>
    /// <remarks>
    /// <c>rr:indirect-damage.4</c>: "characters that cannot take damage cannot
    /// be assigned indirect damage", and <c>.3.1</c> makes a character with no
    /// hit points left ineligible for the same reason — there is no amount that
    /// would not defeat it.
    /// </remarks>
    private static List<Card> Assignable(AbilityCardSelection among, Cast cast) =>
        Assignable(Every(among, cast), cast);

    private static List<Card> Assignable(IReadOnlyList<Card> among, Cast cast) =>
    [
        .. among.Where(card =>
            Room(cast, card) > 0
            && cast.Abilities.CanTakeDamage(cast.World, card, cast.Source)),
    ];

    private static IReadOnlyList<Card> DamageTargets(AbilityCardSelection targets, Cast cast) =>
        DamageTargets(Every(targets, cast), cast);

    private static IReadOnlyList<Card> DamageTargets(IReadOnlyList<Card> targets, Cast cast) =>
        [.. targets.Where(target =>
            cast.Abilities.CanTakeDamage(cast.World, target, cast.Source))];

    /// <summary>How much indirect damage one character may be assigned.</summary>
    private static long Room(Cast cast, Card card) =>
        Damage.Health(cast.World, cast.World.Facts, card) - card.Damage;

    /// <summary>Assigns the damage, then resolves it — <c>rr:indirect-damage.3</c>.</summary>
    /// <remarks>
    /// "All indirect damage from a single source is <b>first assigned and then
    /// resolved simultaneously</b>." So the whole assignment is worked out
    /// before any of it is dealt, which is what stops the first point defeating
    /// a character and making the rest illegal.
    /// </remarks>
    private static void Assign(
        AbilityEffect node, Cast cast, IReadOnlyList<Card> among, long amount)
    {
        var assigned = new Dictionary<int, long>();
        long left = amount;

        foreach (var card in among)
        {
            if (left <= 0)
            {
                break;
            }

            long take = Math.Min(Room(cast, card), left);
            if (take <= 0)
            {
                continue;
            }

            assigned[card.ObjectId] = take;
            left -= take;
        }

        Resolve(node, cast, assigned);
    }

    /// <summary>Deals an assignment that is already worked out.</summary>
    /// <remarks>
    /// In object-id order, because <c>rr:indirect-damage.3</c> resolves it
    /// "simultaneously" and simultaneous still has to reach the event stream in
    /// some order — one the board cannot see and the wire can.
    /// </remarks>
    private static void Resolve(
        AbilityEffect node, Cast cast, Dictionary<int, long> assigned)
    {
        bool suspended = false;
        foreach (var (card, damage) in assigned.OrderBy(each => each.Key))
        {
            suspended |= Damage.DealOutcome(
                cast.World, cast.World.Facts, cast.Source, cast.World.Cards[card], damage,
                cast.Trigger, "Indirect_Damage", cast.Events) == Damage.Outcome.Suspended;
        }
        if (suspended)
        {
            SuspendAfterProcedure(node, cast);
        }
    }

    /// <summary>"Deal N damage to …" — <c>rr:damage</c>.</summary>
    /// <remarks>
    /// Through <see cref="Damage.Deal"/> and not at the token, because damage
    /// is one rule however it arrived: <c>rr:tough.2</c> prevents all of it and
    /// discards a status card instead, and <c>rr:defeat</c> is the other half
    /// of the same moment. A card that wrote to <c>k_damage</c> would skip
    /// both and leave a defeated character standing.
    /// </remarks>
    private static void DealDamage(AbilityEffect.Damage damage, AbilityEffect node, Cast cast, long multiplier = 1)
    {
        long amount = ModifiedAbilityDamage(SaturatingMultiply(
            Amount(damage.Amount, cast), multiplier), cast);
        string verb = damage.AttackVerb ? "Attack" : "Deal_Damage";
        bool suspended = false;
        foreach (var target in Every(damage.Cards, cast))
        {
            long before = target.Damage;
            suspended |= Damage.DealOutcome(
                cast.World, cast.World.Facts, cast.Source, target, amount, cast.Trigger, verb,
                cast.Events) == Damage.Outcome.Suspended;
            if (cast.Power == BasicPowers.AttackVerb && target.Damage > before)
            {
                cast.Occurrence.Also(Steps.DamageDealt);
            }
        }
        if (suspended)
        {
            SuspendAfterProcedure(node, cast);
        }
    }

    private static long ModifiedAbilityDamage(long amount, Cast cast)
    {
        amount = SaturatingSum(amount, [EventModifier(cast, "eventDamage")]);
        return cast.Power == BasicPowers.AttackVerb
            ? SaturatingSum(amount, [EventModifier(cast, "attackDamage")])
            : amount;
    }

    private static void MoveDamage(AbilityEffect.MoveDamage movement, AbilityEffect node, Cast cast)
    {
        var from = Find(movement.From, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot find the character damage moves from");
        var to = Find(movement.To, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot find the enemy damage moves to");
        long amount = Math.Min(from.Damage, Amount(movement.Amount, cast));
        if (amount <= 0 || !cast.Abilities.CanTakeDamage(cast.World, to, cast.Source))
        {
            return;
        }

        Damage.Heal(
            cast.World, cast.World.Facts, from, amount,
            cast.Trigger, "Move_Damage", cast.Events);
        if (Damage.DealOutcome(
            cast.World, cast.World.Facts, cast.Source, to, amount,
            cast.Trigger, "Attack", cast.Events) == Damage.Outcome.Suspended)
        {
            SuspendAfterProcedure(node, cast);
        }
    }

    /// <summary>Damage from an attack event performed by the resolving identity.</summary>
    private static void DealAttackDamage(AbilityEffect.AttackDamage damage, AbilityEffect node, Cast cast)
    {
        var attacker = cast.PowerActor
            ?? cast.AbilityActor
            ?? cast.World.Seats[Resolver(cast)].IdentityCard;
        ContinuousEffect? temporaryOverkill = null;
        if (damage.Overkill)
        {
            temporaryOverkill = new ContinuousEffect(
                EffectSource.LastingEffect,
                Kind: Keywords.Overkill,
                Amount: 1,
                Card: cast.Source.ObjectId,
                Affects: attacker.ObjectId,
                Lasts: new Duration(Uses: 1));
            cast.World.Effects.Register(temporaryOverkill);
        }

        var attackModifiers = EventModifierEffects(cast, "attackDamage");
        long amount = SaturatingSum(
            Amount(damage.Amount, cast),
            [EventModifier(cast, "eventDamage"),
             SaturatingSum(0, attackModifiers.Select(effect => effect.Amount))]);
        bool suspended = false;
        foreach (var target in DamageTargets(damage.Cards, cast))
        {
            var damaged = Damage.Attack(
                cast.World, cast.World.Facts, attacker, cast.Source, target,
                amount, cast.Trigger, "Attack", cast.Events,
                retaliate: false);
            cast.Attacked.Add(target);
            if (damaged.Characters.Count > 0)
            {
                cast.Occurrence.Also(Steps.DamageDealt);
            }
            suspended |= damaged.Suspended;
        }
        // Inside an attack wrapper, every damage instance belongs to the same
        // attack and the wrapper consumes these after its whole effect. A
        // direct dealAttackDamage node is itself the attack and consumes here.
        foreach (var modifier in cast.Power == BasicPowers.AttackVerb
                     ? []
                     : attackModifiers)
        {
            cast.World.Effects.Use(modifier);
        }

        if (temporaryOverkill is not null)
        {
            cast.World.Effects.Use(temporaryOverkill);
        }
        if (suspended)
        {
            SuspendAfterProcedure(node, cast);
        }
    }

    private static void MoveAttackDamage(AbilityEffect.MoveDamage movement, AbilityEffect node, Cast cast)
    {
        var from = Find(movement.From, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot find the character damage moves from");
        var to = Find(movement.To, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot find the enemy damage moves to");
        cast.Attacked.Add(to);
        long amount = Math.Min(from.Damage, Amount(movement.Amount, cast));
        if (amount <= 0 || !cast.Abilities.CanTakeDamage(cast.World, to, cast.Source))
        {
            return;
        }

        Damage.Heal(
            cast.World, cast.World.Facts, from, amount,
            cast.Trigger, "Move_Damage", cast.Events);
        var damaged = Damage.Attack(
            cast.World,
            cast.World.Facts,
            cast.PowerActor
                ?? cast.AbilityActor
                ?? cast.World.Seats[Resolver(cast)].IdentityCard,
            cast.Source,
            to,
            amount,
            cast.Trigger,
            BasicPowers.AttackVerb,
            cast.Events,
            retaliate: false);
        if (damaged.Characters.Count > 0)
        {
            cast.Occurrence.Also(Steps.DamageDealt);
        }
        if (damaged.Suspended)
        {
            SuspendAfterProcedure(node, cast);
        }
    }

    private void SchedulePower(AbilityEffect node, Cast cast, string power)
    {
        var target = Find(EffectOf<AbilityEffect.Power>(node, cast).Target!, cast)
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

    /// <summary>"Place N threat on …" — <c>rr:threat</c>.</summary>
    /// <remarks>
    /// Through <see cref="Threat.Place"/>, which checks
    /// <c>rr:main-scheme-main-scheme-deck.2</c> afterwards: threat that reaches
    /// a main scheme's target completes it whatever put it there, and a card
    /// placing threat is one of the things that can.
    /// </remarks>
    private static void PlaceThreat(AbilityEffect.PlaceThreat threat, Cast cast)
    {
        // "On each side scheme" and "here" are the same node with a different
        // query: `Every` answers one card or many, so a card that names one
        // scheme and a card that names all of them read alike.
        var schemes = Every(threat.Schemes, cast);
        if (schemes.Count == 0)
        {
            // The ability has initiated, but its named game element can leave
            // before resolution. `rr:resolve-as-much-as-possible` resolves the
            // remaining effect with no target rather than recreating the card
            // or treating an absent target as an engine gap.
            return;
        }

        long amount = Amount(threat.Amount, cast);
        if (amount <= 0)
        {
            return;
        }

        if (cast.HasContinuation)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' places threat before its ability has finished; "
                + "the continuation must be preserved across the threat interrupt window");
        }

        Threat.Schedule(
            cast.World, schemes, cast.Source, amount,
            ThreatCause.CardAbility, cast.Trigger, cast.Player,
            cast.ResolutionAbility, cast.Occurrence);
        cast.Suspend();
    }

    private static void PreventThreat(AbilityEffect.PreventThreat prevention, Cast cast)
    {
        var placement = cast.ImminentThreat ?? cast.Occurrence.Threat
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would prevent threat that is not imminent");
        placement.Prevent(Amount(prevention.Amount, cast));
    }

    private static void ReplaceThreatWithDamage(AbilityCardSelection card, AbilityEffect node, Cast cast)
    {
        var placement = cast.Occurrence.Threat
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would replace threat that is not imminent");
        long damage = placement.Remaining;
        var target = Find(card, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' replaces threat with damage to a card that is not there");
        placement.Replace();
        cast.ResolveEffect();
        if (Damage.DealOutcome(
            cast.World, cast.World.Facts, cast.Source, target, damage,
            cast.Trigger, "Deal_Damage", cast.Events) == Damage.Outcome.Suspended)
        {
            SuspendAfterProcedure(node, cast);
        }
    }

    private static void RemoveThreat(AbilityEffect.RemoveThreat removal, Cast cast, long multiplier = 1)
    {
        var schemes = Every(removal.Schemes, cast);
        if (schemes.Count == 0)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would remove threat from a scheme that is not there");
        }

        foreach (var scheme in schemes)
        {
            // `rr:crisis-icon.1`: player cards cannot remove threat from the main
            // scheme while a crisis icon is in play. Encounter effects are not
            // player cards and remain able to do so.
            if (!removal.IgnoresCrisis
                && scheme.Area.Type == DeckType.MainSchemesArea
                && IsPlayerCard(cast)
                && MainScheme.Crisis(cast.World, cast.World.Facts))
            {
                continue;
            }

            Threat.Remove(
                cast.World,
                cast.World.Facts,
                cast.Abilities,
                scheme,
                SaturatingSum(
                    SaturatingMultiply(Amount(removal.Amount, cast), multiplier),
                    [EventModifier(cast, "eventThreatRemoval")]),
                cast.Trigger,
                "Remove_Threat",
                cast.Events,
                by: Resolver(cast),
                overridesCannotFrom: removal.OverridesCannotFrom is { } source
                    ? Find(source, cast)?.ObjectId ?? -1 : -1);
        }
    }

    private static long EventModifier(Cast cast, string kind) =>
        SaturatingSum(0, EventModifierEffects(cast, kind).Select(effect => effect.Amount));

    private static IReadOnlyList<ContinuousEffect> EventModifierEffects(
        Cast cast, string kind)
    {
        if (cast.World.Facts.Kind(cast.Source.FaceId) != CardKind.Event)
        {
            return [];
        }

        return [.. cast.World.Effects.Active().Where(effect =>
            string.Equals(effect.Kind, kind, StringComparison.Ordinal)
            && effect.Affects == cast.Source.ObjectId)];
    }

    /// <summary>
    /// "Each player places a random card from their hand facedown here."
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Placed, not discarded.</b> The card is still a card and comes back —
    /// Highway Robbery's "When Defeated" returns each one to its owner's hand.
    /// So it goes onto the host as an attachment, which is what
    /// <c>rr:attachment</c> makes "here" mean, and it goes <b>facedown</b>:
    /// nobody may look at it while it is there.
    /// </para>
    /// <para>
    /// One selection from the game's single random stream per card taken, in player
    /// order, for the same reason <c>discardAtRandom</c> takes them that way —
    /// the order is what the stream sees.
    /// </para>
    /// </remarks>
    private static void PlaceAtRandom(AbilityEffect.PlaceAtRandom placement, Cast cast)
    {
        var host = Find(placement.Host, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' places cards on a card that is not there");

        var onto = cast.World.AreaOf(
            DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId, host.Area.CardOwner);
        long count = Amount(placement.Count, cast);

        foreach (int seat in Seats(placement.Players, cast))
        {
            var hand = cast.World.Seats[seat].Hand;
            for (long placed = 0; placed < count && hand.Cards.Count > 0; placed++)
            {
                var card = cast.World.Random.Choice(hand.Cards);
                var from = card.Area;
                World.MoveToTop(card, onto);
                card.TurnFaceDown();

                cast.Events.Add(new CardsMoved(
                    Places.Reference(from), Places.Reference(onto),
                    [new Landing(card.ObjectId, onto.Cards.Count - 1)])
                {
                    Trigger = cast.Trigger, Verb = "Place",
                });
                cast.Events.Add(new CardAttached(card.ObjectId, host.ObjectId)
                {
                    Trigger = cast.Trigger, Verb = "Place",
                });
            }
        }
    }

    /// <summary>"Return each … to its owner's hand."</summary>
    /// <remarks>
    /// To <b>its owner's</b> hand and not the resolving player's: a card placed
    /// by each player comes back to each player. Ownership is the card's, which
    /// is why <c>Card.Owner</c> decides rather than whoever defeated the
    /// scheme.
    /// </remarks>
    private static void ReturnToHand(AbilityCardSelection selection, Cast cast)
    {
        foreach (var card in Every(selection, cast))
        {
            var from = card.Area;
            var hand = cast.World.Seats[card.Owner].Hand;
            var constantsEnding = cast.World.Effects.PreflightConstantsEnding(card);
            using var departure = constantsEnding.Begin();
            if (DeckTypes.IsInPlay(from.Type))
            {
                Rules.Play.Discard.Attachments(
                    cast.World, card, cast.Trigger, cast.Events);
            }
            World.MoveToTop(card, hand);
            card.TurnFaceUp();

            cast.Events.Add(new CardsMoved(
                Places.Reference(from), Places.Reference(hand),
                [new Landing(card.ObjectId, hand.Cards.Count - 1)])
            {
                Trigger = cast.Trigger, Verb = "Return",
            });
            constantsEnding.Complete(cast.Trigger, cast.Events);
            cast.Events.Add(new CardDetached(card.ObjectId, from.Host)
            {
                Trigger = cast.Trigger, Verb = "Return",
            });
        }
    }

    /// <summary>
    /// "Discard N cards at random from … hand".
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The draw is a wire format.</b> One MT19937 stream runs the whole
    /// game, so how many numbers this takes and in what order decides every
    /// later shuffle and every later random card. <c>EngineRandom.Choice</c>
    /// uses masked rejection, so one selection can consume multiple words.
    /// Each discarded card gets one selection from the hand as it stands
    /// after the previous discard; see docs/rng-contract.md.
    /// </para>
    /// <para>
    /// "From <b>each</b> player's hand" goes in player order —
    /// <c>rr:in-player-order</c> — because the order is what the stream sees.
    /// A player with an empty hand discards nothing and takes no draw.
    /// </para>
    /// <para>
    /// What it records is <c>result.resourceTypes</c>: how many <i>different</i>
    /// resource types went, which is what "for each different resource type
    /// discarded this way" counts. A card printing two of one letter is one
    /// type, and a card printing none is none.
    /// </para>
    /// </remarks>
    private static void DiscardAtRandom(AbilityEffect.DiscardAtRandom discard, Cast cast)
    {
        long count = Amount(discard.Count, cast);
        var types = new SortedSet<char>();
        long discarded = 0;

        foreach (int seat in Seats(discard.Players, cast))
        {
            var hand = cast.World.Seats[seat].Hand;
            for (long gone = 0; gone < count && hand.Cards.Count > 0; gone++)
            {
                var card = cast.World.Random.Choice(hand.Cards);
                types.UnionWith(Resources.GeneratedBy(card.FaceId, cast.World.Facts));
                Marvel.Rules.Play.Discard.Card(cast.World, card, cast.Trigger, cast.Events);
                discarded += 1;
            }
        }

        cast.Results["discarded"] = discarded;
        cast.Results["resourceTypes"] = types.Count;
    }

    /// <summary>
    /// "Discard cards from the top of the encounter deck until a … is
    /// discarded" — <c>rr:discard.4</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "If multiple cards are discarded from a deck by a singular effect, place
    /// those cards in the appropriate discard pile <b>one at a time (without
    /// changing the order)</b>", and <c>.4.1</c> makes them simultaneous all
    /// the same. So this takes the top card each time rather than counting
    /// ahead — and through <see cref="EncounterDeck.TakeTop"/>, so a deck that
    /// empties mid-search reshuffles instead of ending the search.
    /// </para>
    /// <para>
    /// <b>Bounded, and the bound is a rule and not a fear.</b> A search for a
    /// card that is in neither the deck nor the discard pile would otherwise
    /// reshuffle for ever. The bound is how many cards there are, so a card
    /// that exists is always found and one that does not ends the search
    /// instead of the game.
    /// </para>
    /// </remarks>
    private static void DiscardUntil(AbilityEffect.DiscardUntil discard, Cast cast)
    {
        var found = EncounterDeck.DiscardUntil(
            cast.World, cast.World.Facts, discard.Kind, cast.Trigger, cast.Events, discard.Trait);
        if (found is null)
        {
            return;
        }

        if (discard.PutIntoPlayForFirstPlayer)
        {
            PutIntoPlay(found, cast.World.FirstPlayer, cast);
        }
        else
        {
            RevealCard(found, cast);
        }
    }

    private static void DiscardTop(AbilityEffect.DiscardTop discard, Cast cast)
    {
        long count = Amount(discard.Count, cast);
        if (discard.Players is null && discard.From == AbilitySearchArea.EncounterDeck)
        {
            cast.Discarded.AddRange(EncounterDeck.DiscardTop(
                cast.World, count, cast.Trigger, cast.Events));
            return;
        }
        IEnumerable<Area> decks = discard.Players is { } players
            ? Seats(players, cast).Select(player => cast.World.Seats[player].Deck)
            : [Area(discard.From, cast)];
        foreach (var deck in decks)
        {
            if (deck.Type == DeckType.PlayerDeck && deck.PlayArea.IsPlayers)
            {
                cast.Discarded.AddRange(PlayerDeck.DiscardTop(
                    cast.World, deck.PlayArea.Player, count, cast.Trigger, cast.Events));
                continue;
            }

            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' discards from unsupported deck {deck.Type}");
        }
    }

    private static void RecoverDiscardedByResource(AbilityEffect.RecoverDiscardedByResource recovery, Cast cast)
    {
        var hand = cast.World.Seats[cast.Player].Hand;
        foreach (var card in cast.Discarded.Where(card =>
            Resources.GeneratedBy(card.FaceId, cast.World.Facts).Contains(recovery.Resource)).ToList())
        {
            var from = card.Area;
            World.MoveToTop(card, hand);
            cast.Events.Add(new CardsMoved(
                Places.Reference(from), Places.Reference(hand),
                [new Landing(card.ObjectId, hand.Cards.Count - 1)])
            {
                Trigger = cast.Trigger, Verb = "Add_To_Hand",
            });
        }
    }

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
    /// Which kind of thing a card means by how something was defeated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The word in the data names <b>the rule</b>; what comes back is the verb
    /// the event stream records damage under, which is what
    /// <c>Defeated.How</c> holds. Keeping the two apart is the point: a card
    /// says "consequential damage" because <c>rr:consequential-damage</c> is
    /// what it means, and the day the stream spells that verb differently the
    /// card does not have to change.
    /// </para>
    /// <para>
    /// <b>One word, because one card asks.</b> Gene Pool's "by anything other
    /// than consequential damage" is the whole of it. Anything else is refused
    /// by name rather than guessed at, so a card reaching for a cause this
    /// engine cannot tell apart says so instead of quietly answering false.
    /// </para>
    /// </remarks>
    private static string Cause(string named, Cast cast) => named switch
    {
        "consequentialDamage" => "Consequential_Damage",
        _ => throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' asks whether a card was defeated by '{named}', and this "
            + "engine can only tell consequential damage from everything else"),
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
            ? Find(named, cast)?.ObjectId ?? -1
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
        var enemies = ActivationCandidates(instruction, cast).ToList();
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
        [.. Every(instruction.Enemies, cast).Where(enemy => !instruction.Dynamic
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
        Cast.CardBinding chosen, Dictionary<string, long> results)
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

    /// <summary>All allies in player discard piles, in player and pile order.</summary>
    private static IReadOnlyList<Card> AlliesInPlayerDiscards(World world) =>
    [
        .. world.PlayerOrder.SelectMany(player => world.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player).Cards)
            .Where(card => world.Facts.Kind(card.FaceId) == CardKind.Ally),
    ];

    private static bool CanMakeTheCall(Cast cast)
        => AlliesInPlayerDiscards(cast.World).Any(ally => Resources.Pays(
            string.Concat(MakeTheCallSources(
                    cast.World, cast.Player, cast.Source, ally)
                .Select(source => source.Generates)),
            Resources.Cost(ally.FaceId, cast.World.Facts, cast.World.Players) ?? 0,
            Resources.Required(cast.World, ally, cast.World.Facts)));

    /// <summary>The resources available while paying one Make the Call candidate's cost.</summary>
    private static IReadOnlyList<ResourceSource> MakeTheCallSources(
        World world, int player, Card source, Card ally) =>
    [
        .. CardPlay.Generators(world, world.Facts, world.Seats[player], payingFor: ally)
            .Where(generator => generator.Effect != source.ObjectId),
    ];

}
