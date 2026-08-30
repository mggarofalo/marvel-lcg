using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    /// <summary>Whether one ability answers this occurrence, in this window.</summary>
    private bool Answers(
        World world, CardAbility ability, Card card, Occurrence occurrence, WindowKind window,
        int? initiatingPlayer = null)
    {
        // A constant ability names no condition at all -- `rr:ability.5` -- so
        // it answers no occurrence and appears in no window. What it does is
        // read off the board by `Constant` instead.
        if (ability.Trigger.Event is not { } condition || !occurrence.Is(condition))
        {
            return false;
        }

        // **The rest of the sentence, when the rest of the sentence is about
        // the occurrence.** `rr:triggering-condition.2` gives one window to an
        // occurrence that created several conditions, so a card whose triggering
        // condition is "Unus **attacks and** defeats an ally" is answering a
        // moment that carries both -- and an occurrence carrying only the defeat
        // has not met it. `rr:forced.1` is why this gates the trigger rather
        // than guarding the effect: a forced ability must resolve when its
        // condition is met, so an ability that initiates and does nothing has
        // already been the wrong answer -- it takes a place in `rr:forced.5`'s
        // ordering question that it should never have been offered.
        if (ability.Trigger.Also is { } also && !occurrence.Is(also))
        {
            return false;
        }

        bool belongs = window switch
        {
            WindowKind.Interrupt => AbilityTypes.IsInterrupt(ability.Trigger.Timing),
            WindowKind.Response => AbilityTypes.IsResponse(ability.Trigger.Timing),
            _ => false,
        };

        int? restricted = initiatingPlayer ?? RestrictedPlayer(world, ability, card);
        return belongs
            && Subject(world, ability.Trigger.Subject, card, occurrence, restricted)
            && Role(world, ability.Trigger.Actor, card, occurrence.ActorFacts, restricted)
            && Role(world, ability.Trigger.Target, card, occurrence.TargetFacts, restricted)
            && Player(world, ability.Trigger.Player, card, occurrence, restricted);
    }

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
        World world, CardAbility ability, Card card, Occurrence occurrence) =>
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
    private int? RestrictedPlayer(World world, CardAbility ability, Card card)
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
    private bool UsesYouOrYour(CardAbility ability, Card card) =>
        ability.Trigger.Subject == AbilitySubjects.You
        || ability.Trigger.Actor == AbilityRoles.You
        || ability.Trigger.Target == AbilityRoles.You
        || ability.Trigger.Player == AbilityPlayers.You
        || ContainsYouOrYour(ability.Effect)
        || (ability.Cost is { } cost && ContainsYouOrYour(cost))
        || (ability.When is { } when && ContainsYouOrYour(when))
        || (book.Attaches(card.FaceId) is { } attachment
            && ContainsYouOrYour(attachment));

    private static bool ContainsYouOrYour(AbilityNode node) =>
        IsYouOrYourBinding(node.Kind) || ContainsYouOrYour(node.Argument);

    private static bool ContainsYouOrYour(AbilityValue value) => value switch
    {
        AbilityValue.Word word => IsYouOrYourBinding(word.Value),
        AbilityValue.List list => list.Values.Any(ContainsYouOrYour),
        AbilityValue.Map map => map.Entries.Any(entry =>
            IsYouOrYourBinding(entry.Key) || ContainsYouOrYour(entry.Value)),
        _ => false,
    };

    /// <summary>Whether one DSL identifier is relative to the resolving player.</summary>
    private static bool IsYouOrYourBinding(string value)
    {
        if (string.Equals(value, "you", StringComparison.Ordinal)
            || value.StartsWith("your", StringComparison.Ordinal))
        {
            return true;
        }

        // Compound DSL identifiers use camel case: alliesYouControl,
        // isYourIdentity, identitySpecificInYourHand. A printed title can also
        // contain the word “You”, so only identifier-shaped values count.
        return value.All(character => char.IsLetterOrDigit(character) || character == '.')
            && (value.Contains("You", StringComparison.Ordinal)
                || value.Contains("Your", StringComparison.Ordinal));
    }

    /// <summary>Whether this player is permitted to initiate the ability.</summary>
    private bool MayInitiate(World world, CardAbility ability, Card card, int player)
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

    private static void Run(CardAbility ability, Cast cast)
    {
        var labels = ability.Labels ?? [];
        if (labels.Count > 0)
        {
            if (!cast.LabelsPreflighted)
            {
                if (!CanInitiateLabels(ability, cast))
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

    private static void Run(AbilityNode node, Cast cast)
    {
        int eventsBefore = cast.Events.Count;
        var agendaOwner = cast.World.Agenda.Current;
        var agendaOccurrence = cast.World.Agenda.Occurrence;
        var healthBefore = cast.World.Effects.CaptureCharacterHealth();
        switch (node.Kind)
        {
            case "seq":
                Sequence(node, cast, from: 0);
                break;

            case "and":
                // `rr:and` makes the effects simultaneous and independent;
                // `rr:first-player.3` gives their order to the first player.
                var simultaneous = Nodes(node.Argument).ToList();
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

            case "generate":
                // `rr:resource-ability` -- a resource ability is *read* while a
                // cost is being paid rather than run like an effect, so nothing
                // happens here. `ResourceAbilities` takes its letters and
                // `UseResource` counts the use; running it would be a second
                // way to generate the same resource.
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' generates a resource, which is read while a "
                    + "cost is paid rather than resolved as an effect");

            case "changeForm":
                ChangeForm(node, cast);
                break;

            case "removeFromGame":
                RemoveFromGame(node, cast);
                break;

            case "soakDamage":
                Soak(node, cast);
                break;

            case "exhaust":
                Exhaust(node, cast);
                break;

            case "ready":
                Ready(node, cast);
                break;

            case "drawToHandSize":
                DrawToHandSize(node, cast);
                break;

            case "drawToPrintedHandSize":
                DrawToPrintedHandSize(node, cast);
                break;

            case "removeCounters":
                RemoveCounters(node, cast);
                break;

            case "placeCounters":
                var counter = Word(node.Require("counter"));
                var counterCard = Find(node.Require("card"), cast)
                    ?? throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' cannot find the card receiving counters");
                long beforeCounters = counterCard.Tokens.GetValueOrDefault("c_" + counter);
                long placedCounters = Amount(node.Require("count"), cast);
                if (placedCounters < 0)
                {
                    throw new AbilityException("'placeCounters' needs a non-negative 'count'");
                }
                if (placedCounters == 0)
                {
                    break;
                }
                counterCard.PlaceTokens("c_" + counter, placedCounters);
                cast.Events.Add(new FieldSet(
                    counterCard.ObjectId, "c_" + counter,
                    beforeCounters, beforeCounters + placedCounters)
                {
                    Trigger = cast.Trigger, Verb = "Place_Counters",
                });
                break;

            case "advanceMainScheme":
                AdvanceMainScheme(node, cast);
                break;

            case "preventDamage":
                PreventDamage(node, cast);
                cast.ResolveEffect();
                break;

            case "cancelWhenRevealed":
                CancelWhenRevealed(cast);
                cast.ResolveEffect();
                break;

            case "dealEncounterCards":
                DealEncounterCards(node, cast);
                break;

            case "dealEncounterCard":
                Rules.Play.Deal.EncounterCard(
                    cast.World,
                    Find(node.Require("card"), cast)
                        ?? throw new RulesNotImplementedException(
                            $"'{cast.Source.FaceId}' cannot find the encounter card to deal"),
                    Seat(node.Require("player"), cast),
                    cast.Trigger,
                    cast.Events);
                break;

            case "revealTop":
                RevealCard(TopOfTheEncounterDeck(cast), cast);
                break;

            case "reveal":
                RevealCard(Find(node.Argument, cast), cast);
                break;

            case "placeAtRandom":
                PlaceAtRandom(node, cast);
                break;

            case "returnToHand":
                ReturnToHand(node, cast);
                break;

            case "addToHand":
            {
                var added = Find(node.Argument, cast)
                    ?? throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' cannot find the card added to hand");
                var oldArea = added.Area;
                var newHand = cast.World.Seats[cast.Player].Hand;
                var addedConstantsEnding = cast.World.Effects.PreflightConstantsEnding(added);
                using var addedDeparture = addedConstantsEnding.Begin();
                if (DeckTypes.IsInPlay(oldArea.Type))
                {
                    Rules.Play.Discard.Attachments(
                        cast.World, added, cast.Trigger, cast.Events);
                }
                if (!Characteristics.IsLost(cast.World, added, "linked")
                    && cast.World.Facts.Attributes(added.FaceId).ContainsKey("Linked"))
                {
                    // rr:linked-card-title.4 changes ownership at the moment
                    // the player takes control. A linked ally added from the
                    // set-aside area reaches their hand before it enters play.
                    added.TransferLinkedOwnership(cast.Player);
                }
                World.MoveToTop(added, newHand);
                cast.Events.Add(new CardsMoved(
                    Places.Reference(oldArea), Places.Reference(newHand),
                    [new Landing(added.ObjectId, newHand.Cards.Count - 1)])
                {
                    Trigger = cast.Trigger, Verb = "Add_To_Hand",
                });
                addedConstantsEnding.Complete(cast.Trigger, cast.Events);
                break;
            }

            case "returnOwnedToHand":
            {
                var returned = Find(node.Argument, cast)
                    ?? throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' cannot find the card returned to hand");
                if (returned.Owner < 0)
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' returns a card with no owning player");
                }
                var returnedFrom = returned.Area;
                var ownersHand = cast.World.Seats[returned.Owner].Hand;
                var returnedConstantsEnding =
                    cast.World.Effects.PreflightConstantsEnding(returned);
                using var returnedDeparture = returnedConstantsEnding.Begin();
                if (DeckTypes.IsInPlay(returnedFrom.Type))
                {
                    Rules.Play.Discard.Attachments(
                        cast.World, returned, cast.Trigger, cast.Events);
                }
                World.MoveToTop(returned, ownersHand);
                cast.Events.Add(new CardsMoved(
                    Places.Reference(returnedFrom), Places.Reference(ownersHand),
                    [new Landing(returned.ObjectId, ownersHand.Cards.Count - 1)])
                {
                    Trigger = cast.Trigger, Verb = "Return",
                });
                returnedConstantsEnding.Complete(cast.Trigger, cast.Events);
                break;
            }

            case "discardAtRandom":
                DiscardAtRandom(node, cast);
                break;

            case "discardHandWithResource":
                string wantedResource = Word(node.Argument);
                foreach (var card in cast.World.Seats[cast.Player].Hand.Cards
                             .Where(card => Resources.GeneratedBy(
                                 card.FaceId, cast.World.Facts).Contains(
                                     wantedResource, StringComparison.Ordinal))
                             .ToList())
                {
                    Rules.Play.Discard.Card(cast.World, card, cast.Trigger, cast.Events);
                    cast.Discarded.Add(card);
                }
                cast.Results["discarded"] = cast.Discarded.Count;
                break;

            case "discardUntil":
                DiscardUntil(node, cast);
                break;

            case "discardTop":
                DiscardTop(node, cast);
                break;

            case "recoverDiscardedByResource":
                RecoverDiscardedByResource(node, cast);
                break;

            case "shuffleInto":
                ShuffleInto(node, cast);
                break;

            case "search":
                Search(node, cast);
                break;

            case "choose":
            case "chooseCard":
                Choose(node, cast);
                break;

            case "resolveSpecials":
                if (Every(node.Require("cards"), cast).Count > 0)
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
                    (int)Number(node.Require("count"))).Count == 0)
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

                if (!((AbilityRunner)cast.Abilities).activationEffects.TryGetValue(
                    current.Id, out var waiting))
                {
                    ((AbilityRunner)cast.Abilities).activationEffects[current.Id] = waiting = [];
                }
                waiting.Add(new ActivationEffect(
                    cast.Source.ObjectId, cast.Player, cast.Tier,
                    Tree(node.Require("effect")),
                    cast.Altered?.ObjectId ?? -1,
                    cast.AbilityActor?.ObjectId ?? -1));
                cast.ResolveEffect();
                break;

            case "if":
                var branch = Test(Tree(node.Require("test")), cast) ? "then" : "else";
                if (node.Field(branch) is { } taken)
                {
                    RunChild(Tree(taken), $"if:{branch}", cast);
                }

                break;

            case "forEach":
                ForEach(node, cast);
                break;

            case "eachTime":
                EachTime(node, cast);
                break;

            case "giveStatus":
                GiveStatus(node, cast);
                break;

            case "giveAdditionalBoost":
                Attack.GiveAdditionalBoostCard(
                    cast.World,
                    Find(node.Require("enemy"), cast)
                        ?? throw new AbilityException(
                            $"'{cast.Source.FaceId}' cannot find the enemy receiving an additional boost card"),
                    cast.Trigger,
                    cast.Events);
                break;

            case "alsoAttackEachOtherHero":
                Attack.AlsoResolveAgainstEachOtherHero(cast.World);
                cast.ResolveEffect();
                break;

            case "declareDefender":
                var declared = Find(node.Require("card"), cast)
                    ?? throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' cannot find the character it declares as defender");
                Attack.DeclareByAbility(
                    cast.World, cast.World.Facts, declared,
                    ReplaceableDefenseDefender(cast));
                cast.ResolveEffect();
                break;

            case "attachTo":
                AttachTo(node, cast);
                break;

            case "grantUntil":
                GrantUntil(node, cast);
                cast.ResolveEffect();
                break;

            case "grantCharactersControlledBy":
                foreach (string field in Values(node.Require("fields")).Select(Word))
                {
                    cast.World.Effects.GrantToCharactersControlledBy(
                        cast.Source, Seat(node.Require("player"), cast), field,
                        Amount(node.Require("amount"), cast),
                        Word(node.Require("until")));
                }
                cast.ResolveEffect();
                break;

            case "reduceNextCardCost":
                CardPlay.ReduceNextCardCost(
                    cast.World, cast.Source, Seat(node.Require("player"), cast),
                    Amount(node.Require("amount"), cast));
                cast.ResolveEffect();
                break;

            case "delayUntil":
                DelayUntil(node, cast);
                cast.ResolveEffect();
                break;

            case "discard":
                Discard(node, cast);
                break;

            case "gainSurge":
                // `rr:surge`: "the player resolving the card deals themself a
                // facedown encounter card from the top of the encounter deck",
                // and `.1` writes it as "**When Revealed**: deal yourself 1
                // facedown encounter card". A card that *gains* surge does the
                // same thing the keyword would have.
                //
                // `rr:keywords.1` makes every additional non-numeric instance
                // inert. Printed and continuously granted Surge already ran in
                // `Reveal.Keywords`; multiple nodes and a value greater than one
                // are multiple gained instances inside this reveal. All four
                // shapes therefore produce at most one deal between them.
                if (Number(node.Argument) > 0
                    && StateFields.Modified(
                        cast.World, cast.Source, "surge", cast.World.Facts,
                        cast.World.Players) <= 0
                    && cast.GainedKeywords.Add("surge"))
                {
                    RememberGainedSurge(cast.World, cast.Source.ObjectId);
                    Deal.EncounterCard(
                        cast.World, cast.Player, cast.Trigger, cast.Events);
                }

                // `.2` finishes the original card first, which the villain
                // phase's reveal queue does without anything else here.
                break;

            case "heal":
                Heal(node, cast);
                break;

            case "indirectDamage":
                Indirect(node, cast);
                break;

            case "makeAttackIndirect":
                Attack.MakeIndirect(cast.World);
                break;

            case "dealDamage":
                DealDamage(node, cast);
                break;

            case "moveDamage":
                MoveDamage(node, cast);
                break;

            case "dealAttackDamage":
                DealAttackDamage(node, cast);
                break;

            case "moveAttackDamage":
                MoveAttackDamage(node, cast);
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
                    RunChild(Tree(node.Require("effect")), "defense:effect", cast);
                }
                break;

            case "thwart":
                ((AbilityRunner)cast.Abilities).SchedulePower(node, cast, BasicPowers.ThwartVerb);
                break;

            case "thwartSchemes":
                var schemes = Every(node.Require("schemes"), cast);
                if (schemes.Count > 0)
                {
                    cast.Choose(schemes[0]);
                    ((AbilityRunner)cast.Abilities).SchedulePower(
                        Tree(node.Require("power")), cast, BasicPowers.ThwartVerb,
                        schemes[0], schemes, -1);
                }
                break;

            case "placeThreat":
                PlaceThreat(node, cast);
                break;

            case "placeAccelerationToken":
                EncounterDeck.PlaceAccelerationToken(cast.World, cast.Trigger, cast.Events);
                break;

            case "preventThreat":
                PreventThreat(node, cast);
                cast.ResolveEffect();
                break;

            case "replaceThreatWithDamage":
                ReplaceThreatWithDamage(node, cast);
                break;

            case "removeThreat":
                RemoveThreat(node, cast);
                break;

            case "enemyAttacks":
                Activate(node, cast, Steps.Attack);
                break;

            case "enemySchemes":
                Activate(node, cast, Steps.Scheme);
                break;

            case "putIntoPlay":
                PutIntoPlay(node, cast);
                break;

            case "createDrones":
                CreateDrones(node, cast);
                break;

            case "shuffle":
                // `rr:search.3` -- "if any portion of a deck is searched, upon
                // completion of that game step, game function, or card ability,
                // shuffle that entire deck." A step of the card rather than
                // part of the search, because "upon completion" is after the
                // player has answered which card they took.
                if (cast.World.Shuffle(Area(Word(node.Argument), cast)))
                {
                    cast.ResolveEffect();
                }
                break;

            case "draw":
                foreach (int player in Seats(node.Require("player"), cast))
                {
                    if (CanDraw(cast.World, player))
                    {
                        Draw.Cards(
                            cast.World, player,
                            (int)Number(node.Require("count")),
                            cast.Trigger, cast.Events);
                    }
                }
                break;

            default:
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' uses the effect node '{node.Kind}', "
                    + "which is not implemented");
        }
        if (cast.Events.Count > eventsBefore && EventMeansEffectApplied(node.Kind))
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

    private static bool EventMeansEffectApplied(string kind) => kind is not (
        "seq" or "and" or "then" or "otherwise" or "eachPlayer" or "if"
        or "forEach" or "eachTime" or "choose" or "chooseCard"
        or "resolveSpecials" or "payOrExhaust" or "payOrEffect"
        or "chooseTopForHand" or "chooseDiscardToShuffle"
        or "thwartDifferentSchemes" or "makeTheCall" or "legalPractice"
        or "attack" or "defense" or "thwart" or "thwartSchemes"
        or "placeThreat" or "enemyAttacks" or "enemySchemes");

    private static bool Test(AbilityNode node, Cast cast) => node.Kind switch
    {
        "and" => Nodes(node.Argument).All(each => Test(each, cast)),
        "or" => Nodes(node.Argument).Any(each => Test(each, cast)),
        "not" => !Test(Tree(node.Argument), cast),
        "finalStep" => cast.FinalStep,
        "paidWithResource" => PaidWith(cast, Word(node.Argument)),
        "threatCause" => cast.Occurrence.Threat?.Cause == (Word(node.Argument) switch
            {
                "villainPhase" => ThreatCause.VillainPhase,
                "enemyScheme" => ThreatCause.EnemyScheme,
                "incite" => ThreatCause.Incite,
                "cardAbility" => ThreatCause.CardAbility,
                var cause => throw new AbilityException($"'{cause}' is not a threat cause"),
            }),
        // Through `Every` and not `Find`: "is there one" is a question about a
        // set, and a query that names many -- "an upgrade or support you
        // control" -- has to be answerable by it. `Every` falls back to `Find`
        // for the queries that name one, so both shapes go through here.
        "exists" => Every(node.Argument, cast).Count > 0,
        "canMakeTheCall" => CanMakeTheCall(cast),
        "canLegalPractice" => cast.World.Seats[cast.Player].Hand.Cards.Any(
                card => card.ObjectId != cast.Source.ObjectId)
            && Every(node.Argument, cast).Any(
                scheme => scheme.Tokens.GetValueOrDefault("k_threat") > 0),
        "canAutomaticThwart" => Find(node.Argument, cast) is { } scheme
            && BasicPowers.CanAutomaticallyThwart(
                cast.World, cast.World.Facts, cast.Player, scheme),

        // "If Vulture is in play". `rr:identity.2` makes a title name one
        // card -- "if a card refers to a hero or alter-ego by title, it refers
        // only to the identity with that title" -- so this compares titles and
        // not printed ids, and asks only of the places `rr:in-play-and-out-of-play`
        // calls in play.
        "titleInPlay" => cast.World.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Any(card => string.Equals(
                cast.World.Facts.Title(card.FaceId), Word(node.Argument),
                StringComparison.Ordinal)),

        // "If no damage was healed this way" and its family: a comparison
        // against what an earlier action in this ability actually did.
        "atLeast" => Amount(node.Require("value"), cast) >= Amount(node.Require("count"), cast),

        // `rr:form` -- "(Hero)" and "(Alter-Ego)" on a card gate the ability by
        // which form the player is in. Not a boolean: `Forms.Of` answers with a
        // set, because a hero can print more than two faces.
        "inForm" => Forms.In(
            cast.World,
            cast.World.Seats[Seat(node.Require("player"), cast)],
            cast.World.Facts,
            Word(node.Require("form"))),

        "activationIs" => cast.World.Activation is { } activation
            && activation.Attacking == (Word(node.Argument) switch
            {
                "attack" => true,
                "scheme" => false,
                var kind => throw new AbilityException(
                    $"'{kind}' is not an enemy activation kind"),
            }),

        // "After [enemy] attacks **and damages** you". Two facts, and
        // `rr:attack-enemy-activation.step.6.a` lists them as one trigger
        // shape -- but the abilities it lists all run in the window *after* the
        // attack, by which time the damage is on a dial that had damage on it
        // before. So the attack carries what it did, and this reads it.
        //
        // A test rather than a triggering condition of its own, because the two
        // are indistinguishable for a forced ability: it is in the same window
        // either way, and does nothing when the attack did not land. A card
        // whose trigger is optional would be able to tell them apart -- the
        // prompt would appear -- and that is the case to change this for.
        "attackDamaged" => cast.World.FinishedAttack is { Damaged: true } landed
            && landed.Enemy == cast.Source.ObjectId,

        // `rr:modes-of-play` -- "in expert mode" on a card, which 86 cards in
        // the pool print. Not a property of the card or of anything on the
        // board: it is how the game was set up, so the board carries it.
        //
        // The argument is the mode's own name and is checked, so that a card
        // reaching for one of the other three -- heroic, skirmish, campaign --
        // is a card that says so rather than one that quietly reads "expert".
        "inExpertMode" => Word(node.Argument) == "expert"
            ? cast.World.Expert
            : throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' asks about the '{Word(node.Argument)}' mode, and "
                + "rr:modes-of-play names four of which only 'expert' is modelled"),

        "hasStatus" => Find(node.Require("card"), cast) is { } host
            && Statuses.Has(cast.World, host, Word(node.Require("status"))),

        "hasTrait" => Find(node.Require("card"), cast) is { } traitHolder
            && Rules.State.Traits.Has(
                cast.World, traitHolder, Word(node.Require("trait")), cast.World.Facts),

        "cardSet" => Find(node.Require("card"), cast) is { } setCard
            && string.Equals(
                cast.World.Facts.EncounterSet(setCard.FaceId), Word(node.Require("set")),
                StringComparison.Ordinal),

        "isTitle" => Find(node.Require("card"), cast) is { } titled
            && string.Equals(
                cast.World.Facts.Title(titled.FaceId), Word(node.Require("title")),
                StringComparison.Ordinal),

        "discardedWithResource" => cast.Discarded.Any(card =>
            Resources.GeneratedBy(card.FaceId, cast.World.Facts).Contains(
                Word(node.Argument), StringComparison.Ordinal)),

        "defeatedByYou" => cast.Occurrence.Defeat is { By: >= 0 } defeatedByYou
            && defeatedByYou.By < cast.World.Cards.Count
            && ControllerOf(cast.World, cast.World.Cards[defeatedByYou.By]) == Resolver(cast),

        "wasDefeated" => Find(node.Argument, cast) is { } defeatedCard
            && cast.Occurrence.Defeats.Any(defeat => defeat.Card == defeatedCard.ObjectId),

        "heroDefended" => cast.World.FinishedAttack is { BasicDefense: true } defended
            && defended.Defender == cast.World.Seats[Resolver(cast)].IdentityCard.ObjectId,

        "undefendedAttack" => cast.World.Attack is { IsDefended: false },

        // "After **an ally** is defeated". A card type, asked of a card the
        // ability has already named -- `rr:defeat` is one rule for every kind
        // of card and the cards are the ones that narrow it.
        "isKind" => Find(node.Require("card"), cast) is { } subject
            && cast.World.Facts.Kind(subject.FaceId) == Kind(Word(node.Require("kind"))),

        // Damage occurrences name the exact recipient. That differs from the
        // declared attack target for indirect damage, whose assignment can
        // name any friendly hero or ally after defense is settled.
        "isYourIdentity" => Find(node.Argument, cast)?.ObjectId
            == cast.World.Seats[Resolver(cast)].IdentityCard.ObjectId,

        // "Defeated **by anything other than consequential damage**." What did
        // it is carried on the occurrence's record of the defeat, and the word
        // here names the rule rather than the engine's spelling of it -- see
        // `Cause`.
        "defeatedBy" => cast.Occurrence.Defeat is { } defeat
            && string.Equals(defeat.How, Cause(Word(node.Argument), cast), StringComparison.Ordinal),


        _ => throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' uses the test node '{node.Kind}', "
            + "which is not implemented"),
    };

    private static bool PaidWith(Cast cast, string resource) =>
        cast.Payment.Contains(resource[0])
        || cast.World.Effects.Active().Any(effect =>
            effect.Card == cast.Source.ObjectId
            && string.Equals(effect.Kind, "paid:" + resource, StringComparison.Ordinal));

    /// <summary>
    /// "Deal each player a facedown encounter card" — <c>rr:deal</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Dealt is not revealed.</b> <c>rr:deal</c> puts the card facedown in
    /// front of the player and leaves it there; step 4 of the villain phase is
    /// what turns it over, and it drains the whole queue however the cards got
    /// into it. So a card dealt during setup waits for the first villain phase,
    /// which is what it does at a table.
    /// </para>
    /// <para>
    /// In player order, because <c>rr:in-player-order</c> is what "each player"
    /// means whenever the order is observable — and it is here, since the deck
    /// can empty part-way round.
    /// </para>
    /// </remarks>
    private static void DealEncounterCards(AbilityNode node, Cast cast)
    {
        long each = node.Field("count") is { } count ? Number(count) : 1;
        IReadOnlyList<int> players = node.Field("player") is { } player
            ? [Seat(player, cast)]
            : [.. cast.World.PlayerOrder];
        for (long dealt = 0; dealt < each; dealt++)
        {
            foreach (int seat in players)
            {
                if (Rules.Play.Deal.EncounterCard(
                        cast.World, seat, cast.Trigger, cast.Events) is null)
                {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// "Put the top card of your deck into play facedown … as a Drone minion."
    /// </summary>
    private static void CreateDrones(AbilityNode node, Cast cast)
    {
        long count = node.Field("count") is { } amount ? Number(amount) : 1;
        foreach (int player in Seats(node.Require("player"), cast))
        {
            for (long created = 0; created < count; created++)
            {
                FacedownDrones.EngageTop(
                    cast.World, player, cast.Trigger, "Create_Drone", cast.Events);
            }
        }
    }

    private static bool CanCreateDrones(AbilityNode node, Cast cast) =>
        (node.Field("count") is not { } count || Number(count) > 0)
        && Seats(node.Require("player"), cast).Any(player =>
            cast.World.Seats[player].Deck.Cards.Count > 0
            || cast.World.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player).Cards.Count > 0);

    /// <summary>
    /// "Put it into play engaged with you" — <c>rr:play-put-into-play</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Put into play is not revealed.</b>
    /// <c>rr:when-revealed-abilities.2</c>: "if an encounter card with a
    /// '<b>When Revealed</b>' ability is put into play without being revealed,
    /// the '<b>When Revealed</b>' ability does not trigger." So this moves the
    /// card and stops, where <c>Steps.RevealEncounterCard</c> would have run
    /// the card's own text — and the difference is the whole reason the two are
    /// separate words here.
    /// </para>
    /// <para>
    /// <b>The keywords still fire.</b> <c>rr:enters-play</c> is "any time when
    /// a card transitions from an out-of-play area into play", which a card put
    /// into play does — so toughness and uses X apply, and only the "When
    /// Revealed" is skipped.
    /// </para>
    /// <para>
    /// "Engaged with you" is the only destination any authored card asks for.
    /// <c>rr:engage.1</c> makes it a place: "when a minion engages a player, it
    /// is placed in that player's play area", so engagement is where the card
    /// sits rather than a flag on it.
    /// </para>
    /// </remarks>
    private static void PutIntoPlay(AbilityNode node, Cast cast)
    {
        var card = Find(node.Require("card"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would put a card into play that is not there");

        string where = Word(node.Require("where"));
        if (!string.Equals(where, "engagedWithYou", StringComparison.Ordinal))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' puts a card into play '{where}', "
                + "which is not implemented");
        }

        PutIntoPlay(card, cast.Player, cast);
    }

    /// <summary>Puts one exact minion into play engaged with a named player.</summary>
    private static void PutIntoPlay(Card card, int player, Cast cast)
    {
        var into = cast.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player));
        var from = card.Area;

        // Moving into an in-play area turns the card faceup in `Card.MovedTo`;
        // a second `TurnFaceUp` here would be an equivalent state write.
        World.MoveToTop(card, into);
        cast.Events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(into),
            [new Landing(card.ObjectId, into.Cards.Count - 1)])
        {
            Trigger = cast.Trigger, Verb = "Put_Into_Play",
        });

        Reveal.EnterPlay(cast.World, cast.World.Facts, card, cast.Events);
    }

    private static void GiveStatus(AbilityNode node, Cast cast)
    {
        // "Stun **each hero**" and "stun your hero" are the same node with a
        // different query, the way `placeThreat` names one scheme or all of
        // them: `Every` answers both.
        foreach (var host in Every(node.Require("card"), cast))
        {
            GiveStatus(node, cast, host);
        }
    }

    private static void GiveStatus(AbilityNode node, Cast cast, Card host)
    {
        string what = Word(node.Require("status"));

        // Through the rules rather than straight at `Statuses.Give`:
        // `rr:status-cards.1` caps how many a character can hold,
        // `rr:stalwart` makes that cap zero, and `rr:vulnerable` discards the
        // character. A card giving a status does not get to skip any of them.
        var status = Reveal.Afflict(
            cast.World, cast.World.Facts, host, what, cast.Trigger, cast.Events);
        if (status is null)
        {
            return;
        }

        cast.Events.Add(new CardAttached(status.ObjectId, host.ObjectId)
        {
            Trigger = cast.Trigger, Verb = "Give_Status",
        });
    }

    // `rr:attachment` -- "when an attachment enters play, it attaches to another
    // card or game element".
    private static void AttachTo(AbilityNode node, Cast cast)
    {
        var host = Find(node.Argument, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' attaches to a card that is not there");

        var onto = cast.World.AreaOf(
            DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId, host.Area.CardOwner);
        var from = cast.Source.Area;
        World.MoveToTop(cast.Source, onto);

        cast.Events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(onto),
            [new Landing(cast.Source.ObjectId, onto.Cards.Count - 1)])
        {
            Trigger = cast.Trigger, Verb = "Attach",
        });
        cast.Events.Add(new CardAttached(cast.Source.ObjectId, host.ObjectId)
        {
            Trigger = cast.Trigger, Verb = "Attach",
        });
    }

    // The rules define the role, not this persisted result-key spelling. The
    // value survives a suspended printed sequence so only this defense ability
    // may replace the provisional defender it established.
    private static int ReplaceableDefenseDefender(Cast cast) =>
        checked((int)cast.Results.GetValueOrDefault("defenseAbilityDefender", -1));

}
