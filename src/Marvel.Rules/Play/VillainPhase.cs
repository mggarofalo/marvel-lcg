using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>
/// What a card does when it is revealed from the encounter deck.
/// </summary>
/// <remarks>
/// <para>
/// <b>The seam where rules stop and cards begin.</b> Everything in
/// <see cref="VillainPhase"/> is the Rules Reference — threat, activation,
/// boost, dealing and revealing — and none of it needs to know what any
/// particular card says. This is the one thing it does need, and it is
/// deliberately an interface so the card DSL interpreter can satisfy it without
/// the villain phase changing.
/// </para>
/// <para>
/// <c>docs/card-dsl.md</c> designs that interpreter and opens with "nothing here
/// is implemented". Until it exists, <c>Marvel.Content</c> supplies the handful
/// of cards a recorded transition actually reaches, and this interface is what
/// stops that being a parallel path: there is one place a card's behaviour can
/// enter the engine, and the interpreter replaces what is behind it.
/// </para>
/// </remarks>
public interface ICardAbilities : IWindowAbilities
{
    /// <summary>Resolves a revealed encounter card's "When Revealed" ability.</summary>
    /// <param name="world">The world.</param>
    /// <param name="card">The card being revealed.</param>
    /// <param name="player">The seat it was dealt to.</param>
    /// <returns>What changed.</returns>
    IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player);


}

/// <summary>Nothing has an ability. What an engine with no cards ported does.</summary>
public sealed class NoCardAbilities : ICardAbilities
{
    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player) => [];

    /// <inheritdoc/>
    public IReadOnlyList<PendingAbility> Waiting(
        World world, Occurrence occurrence, WindowKind window) => [];

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Resolve(
        World world, Occurrence occurrence, PendingAbility ability) =>
        throw new RulesNotImplementedException(
            "nothing is waiting in any window, so nothing can be resolved from one");

    /// <inheritdoc/>
    public Prompts.Affordance Describe(World world, PendingAbility ability) =>
        throw new RulesNotImplementedException(
            "nothing is waiting in any window, so nothing can be described from one");
}

/// <summary>
/// The villain phase, step by step, as <c>rr:villain-phase</c> lists them.
/// </summary>
/// <remarks>
/// <para>
/// The steps are numbered here as the Rules Reference numbers them, so a
/// divergence can be argued against the published text rather than against this
/// file. What is implemented is what the recorded milestone game reaches; the
/// rest throws rather than silently doing nothing, because a villain phase that
/// quietly skipped minion activation would produce a plausible board that is
/// wrong.
/// </para>
/// <para>
/// <b>The order is the whole thing.</b> The boost card is drawn before the
/// encounter card and discarded before it, which is why the recorded discard
/// pile holds the boost card at index 0 and the encounter card at index 1. Draw
/// them the other way round and every subsequent card in the encounter deck
/// shifts.
/// </para>
/// </remarks>
public static class VillainPhase
{
    /// <summary>Schedule the villain phase's six steps.</summary>
    /// <remarks>
    /// <para>
    /// <c>rr:villain-phase</c> lists six, and they are six values here rather
    /// than the order of six method calls. That is not tidiness: a window may
    /// hold an ability somebody has to be asked about, and a phase that is a
    /// call has nowhere to stop. See <see cref="Agenda"/>.
    /// </para>
    /// <para>
    /// Steps 2 and 4 are headings rather than occurrences, so they open no
    /// windows of their own; what happens under them — one activation, one card
    /// revealed — is scheduled when they are reached.
    /// </para>
    /// </remarks>
    /// <param name="agenda">What the game still has to do.</param>
    /// <param name="round">Which round this is.</param>
    public static void Schedule(Agenda agenda, int round)
    {
        ArgumentNullException.ThrowIfNull(agenda);
        agenda.Add(new PhaseStep(Steps.PlaceThreat, round, 1));
        agenda.Add(new PhaseStep(Steps.EnemiesActivate, round, 2, Plan: true));
        agenda.Add(new PhaseStep(Steps.DealEncounterCards, round, 3));
        agenda.Add(new PhaseStep(Steps.RevealEncounterCards, round, 4, Plan: true));
        agenda.Add(new PhaseStep(Steps.PassFirstPlayerToken, round, 5));
        agenda.Add(new PhaseStep(Steps.EndVillainPhase, round, 6));
    }

    /// <summary>Take one step of the villain phase.</summary>
    /// <remarks>
    /// Returns a prompt when the step itself has something to ask, which one of
    /// them does: <c>rr:attack-enemy-activation.step.2</c> asks whether anybody
    /// defends. That is not a window — nobody is using an ability — so it is the
    /// step that stops, and the answer comes back to
    /// <see cref="Answered"/>.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do.</param>
    /// <param name="step">Which step.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <returns>The question the step is waiting on, or null.</returns>
    /// <exception cref="RulesNotImplementedException">
    /// The board reached a rule this engine does not have — a minion engaged
    /// with a player, or an attack that would defeat its target.
    /// </exception>
    public static Prompt? Take(
        World world, ICardFacts facts, ICardAbilities abilities,
        PhaseStep step, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(events);

        switch (step.What)
        {
            case Steps.PlaceThreat:
                PlaceThreat(world, facts, abilities, events);
                break;

            case Steps.EnemiesActivate:
                PlanActivations(world, facts, step);
                break;

            case Steps.Scheme:
                Scheme(world, facts, abilities, world.Cards[step.Subject], events);
                break;

            case Steps.Attack:
                Attack.Initiate(world, facts, step, events);
                break;

            case Steps.GiveBoostCard:
                Attack.GiveBoostCard(world, facts, events);
                break;

            case Steps.DeclareDefender:
                return Attack.DeclareDefender(world, facts);

            case Steps.FlipBoostCards:
                Attack.FlipBoostCards(world, facts, events);
                break;

            case Steps.DealAttackDamage:
                Attack.DealDamage(world, facts, events);
                break;

            case Steps.EndAttack:
                Attack.End(world, events);
                break;

            case Steps.DealEncounterCards:
                DealEncounterCards(world, facts, events);
                break;

            case Steps.RevealEncounterCards:
                RevealNextEncounterCard(world, step);
                break;

            case Steps.RevealEncounterCard:
                RevealEncounterCard(
                    world, facts, abilities, world.Cards[step.Subject], step.Seat,
                    step.Round, events);
                break;

            case Steps.PassFirstPlayerToken:
                PassFirstPlayerToken(world);
                break;

            case Steps.EndVillainPhase:
                PhaseEnd.EndVillainPhase(world, facts, events);
                break;

            case Steps.DrawToHandSize:
                PhaseEnd.DrawToHandSize(world, facts, events);
                break;

            case Steps.ReadyCards:
                PhaseEnd.ReadyCards(world, events);
                break;

            case Steps.EndPlayerPhase:
                PhaseEnd.EndPlayerPhase(world, events);
                break;

            default:
                throw new RulesNotImplementedException(
                    $"the villain phase has no step '{step.What}'");
        }

        return null;
    }

    /// <summary>Give a step the answer it stopped for.</summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="step">The step that asked.</param>
    /// <param name="input">The player's answer.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Answered(
        World world, ICardFacts facts, PhaseStep step, Decision input, List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.DeclareDefender:
                Attack.Defend(world, facts, input, events);
                break;

            default:
                throw new RulesNotImplementedException(
                    $"step '{step.What}' asked nothing and cannot take an answer");
        }
    }

    /// <summary>
    /// Step 2, as one activation per player — <c>rr:villain-phase.step.2</c>,
    /// "in player order, each player resolves".
    /// </summary>
    private static void PlanActivations(World world, ICardFacts facts, PhaseStep step)
    {
        var villain = world.TheCardIn(DeckType.VillainArea);
        if (villain is null)
        {
            return;
        }

        foreach (int seat in world.PlayerOrder)
        {
            // `rr:activation.1`: hero form and the enemy attacks, alter-ego
            // form and it schemes. Which face is showing *is* which form, so
            // this needs no separate flag.
            var identity = world.Seats[seat].IdentityCard;
            bool attacking = facts.Kind(identity.FaceId) != CardKind.AlterEgo;

            // `rr:villain-phase.step.2.a` then `.step.2.b`: "the villain
            // activates against the player", and then "each minion engaged with
            // the player activates against them, in the order of that player's
            // choice".
            //
            // **The order is the player's and this takes it in the order they
            // sit in the play area.** `rr:minion.3` says so outright, and the
            // recorded prompt vocabulary has a verb for asking --
            // `Minion_Activates_Order`, twice in the fixture. Asking is not
            // implemented; the order here is deterministic and stated rather
            // than a silent pick.
            var enemies = new List<int> { villain.ObjectId };
            enemies.AddRange(world
                .AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(seat))
                .Cards
                .Select(minion => minion.ObjectId));

            foreach (int enemy in enemies)
            {
                world.Agenda.Then(new PhaseStep(
                    attacking ? Steps.Attack : Steps.Scheme,
                    step.Round, 2, Index: seat, Subject: enemy, Seat: seat));
            }
        }
    }

    /// <summary>Step 1. Threat from the main scheme's acceleration field.</summary>
    /// <remarks>
    /// <c>rr:villain-phase.1</c>: "Place the amount of threat indicated in the
    /// main scheme's acceleration field onto that scheme." The engine's name for
    /// that field is <c>EscalationThreat</c>, and it is per-player —
    /// <c>1*</c> on <c>01097b</c>, so one threat at one player and three at
    /// three. Acceleration icons and tokens add more; nothing on the milestone
    /// board has one.
    /// </remarks>
    private static void PlaceThreat(
        World world, ICardFacts facts, ICardAbilities abilities, List<GameEvent> events)
    {
        var scheme = world.TheCardIn(DeckType.MainSchemesArea);
        if (scheme is null)
        {
            return;
        }

        // `rr:villain-phase.step.1`: "place the amount of threat indicated in
        // the main scheme's acceleration field. **If any acceleration icons or
        // tokens are active, additional threat equal to the number of such
        // icons and tokens is also placed at this time.**"
        long amount = facts.PrintedValue(scheme.FaceId, "EscalationThreat", world.Players)
            + MainScheme.Acceleration(world, facts);
        Threat(scheme, amount, "villain phase, place threat", events);
        CheckCompleted(world, facts, abilities, scheme, events);
    }

    /// <summary>An enemy schemes. <c>rr:scheme-enemy-activation</c>.</summary>
    /// <remarks>
    /// Three steps: give it one facedown boost card from the encounter deck,
    /// resolve that card (flip, add its boost icons to SCH, discard), then place
    /// threat equal to the modified SCH on the main scheme.
    /// </remarks>
    private static void Scheme(
        World world, ICardFacts facts, ICardAbilities abilities, Card villain,
        List<GameEvent> events)
    {
        // `rr:confuse-confused.1`: "when this character would scheme or thwart,
        // remove each confused status card from it instead." The scheme does
        // not happen, so no boost card is given and no threat is placed.
        if (BasicPowers.Cancelled(world, facts, villain, Statuses.Confused, events))
        {
            return;
        }

        long scheme = facts.PrintedValue(villain.FaceId, "SCH", world.Players);

        // `rr:scheme-enemy-activation.step.1`, the same clause the attack has:
        // a villain always, a minion only with `rr:villainous`.
        if (Keywords.IsBoosted(villain, facts, world.Players))
        {
            scheme += ResolveBoostCard(world, facts, events);
        }

        var target = world.TheCardIn(DeckType.MainSchemesArea);
        if (target is not null)
        {
            Threat(target, scheme, "scheme", events);
            CheckCompleted(world, facts, abilities, target, events);
        }
    }

    /// <summary>Gives the enemy a boost card, resolves it, and returns its icons.</summary>
    /// <remarks>
    /// <para>
    /// <c>rr:boost-boost-icon.1</c>: a star icon is not a boost icon and adds
    /// nothing. The printed <c>Boost</c> attribute is the icon count already —
    /// <c>01186</c> Advance has none and <c>01101</c> Hydra Mercenary has one —
    /// so a star card and a zero-boost card are the same number here and differ
    /// only in having an ability, which the interpreter will run.
    /// </para>
    /// <para>
    /// <c>rr:boost-boost-icon.5</c>: discard it after applying. The boost card
    /// is drawn and discarded <b>before</b> the encounter cards are dealt, which
    /// is why the recorded discard pile has it underneath.
    /// </para>
    /// </remarks>
    private static long ResolveBoostCard(World world, ICardFacts facts, List<GameEvent> events)
    {
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var boost = EncounterDeck.TakeTop(world, "boost", events);
        if (boost is null)
        {
            return 0;
        }

        // Through the boosting area and not straight to the discard. No
        // recorded step catches a card in transit -- the whole activation
        // happens between two decisions -- but passing through is what
        // registers the card's token pools, and the discarded card's
        // `k_threat` key is on the wire. See `DeckTypes.GrantsTokenPool`.
        var boosting = world.AreaOf(DeckType.BoostingArea);
        boosting.Append(boost);

        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        World.MoveToTop(boost, discard);

        events.Add(new CardsMoved(
            Places.Reference(deck),
            Places.Reference(discard),
            [new Landing(boost.ObjectId, discard.Cards.Count - 1)])
        {
            Trigger = "villain phase", Verb = "Boost",
        });

        return facts.PrintedValue(boost.FaceId, "Boost", world.Players);
    }

    /// <summary>Step 3. One encounter card to each player, in player order.</summary>
    /// <remarks>
    /// Hazard icons deal additional cards. Nothing on the milestone board has
    /// one, and a board that did would deal too few here — so it throws.
    /// </remarks>
    /// <summary>Step 3. One card each, plus one per hazard icon in play.</summary>
    /// <remarks>
    /// <c>rr:villain-phase.step.3</c>: "Deal one encounter card to each player.
    /// Deal one additional card for each hazard icon on a card in play. These
    /// additional cards are dealt in player order."
    /// <para>
    /// Nothing here schedules a reveal. Step 4 drains the queue instead, which
    /// is what lets a card dealt at any other moment — by an ability, or by a
    /// player's deck running out mid-turn — be revealed in the same step as the
    /// rest.
    /// </para>
    /// </remarks>
    private static void DealEncounterCards(
        World world, ICardFacts facts, List<GameEvent> events)
    {
        foreach (int seat in world.PlayerOrder)
        {
            if (Deal.EncounterCard(world, seat, "villain phase", events) is null)
            {
                return;
            }
        }

        // `rr:hazard-icon`: "for each hazard icon on cards in play, deal one
        // player one additional card *(not one card per player)*. Additional
        // cards are dealt in player order" -- so these go round the table one
        // at a time, wrapping, rather than one round per icon.
        long icons = Deal.HazardIcons(world, facts);
        for (long dealt = 0; dealt < icons; dealt++)
        {
            int seat = (world.FirstPlayer + (int)(dealt % world.Players)) % world.Players;
            if (Deal.EncounterCard(world, seat, "hazard", events) is null)
            {
                return;
            }
        }
    }

    /// <summary>Step 4, one card at a time, until the queue is empty.</summary>
    private static void RevealNextEncounterCard(World world, PhaseStep step)
    {
        if (Deal.NextToReveal(world) is not { } next)
        {
            return;
        }

        // The reveal is an occurrence with its own windows; this heading is
        // not. Scheduling itself *after* the reveal is what makes step 4 a
        // loop -- a card revealed here can deal another, and `rr:deal.1` puts
        // that one in the same step.
        //
        // **The order of these two calls is the loop's termination.**
        // `Agenda.Then` appends in call order, so the reveal has to be
        // scheduled first; the other way round, this heading runs again with
        // the card still in the queue and schedules itself forever.
        world.Agenda.Then(new PhaseStep(
            Steps.RevealEncounterCard, step.Round, 4,
            Index: step.Index, Subject: next.Card.ObjectId, Seat: next.Player));
        world.Agenda.Then(new PhaseStep(
            Steps.RevealEncounterCards, step.Round, 4, Index: step.Index + 1, Plan: true));
    }

    /// <summary>Step 4. Each player reveals their cards, in the order dealt.</summary>
    private static void RevealEncounterCard(
        World world, ICardFacts facts, ICardAbilities abilities, Card card, int player,
        int round, List<GameEvent> events)
    {
        // Same reason as the boost card: the revealing area is where an
        // encounter card registers its pools.
        World.MoveToTop(card, world.AreaOf(DeckType.RevealingArea));
        card.TurnFaceUp();
        events.Add(new CardsFlipped([card.ObjectId], true)
        {
            Trigger = "villain phase", Verb = "Reveal",
        });

        // `rr:reveal.step.2` -- **where the card goes is decided by its type**,
        // and it happens before step 3's "When Revealed" abilities. A minion
        // that entered play is already engaged when its own ability resolves.
        Reveal.Resolve(world, facts, card, player, events);

        // Step 3. "Resolve each **When Revealed** ability on that card
        // *(including those provided by keywords)*."
        //
        // **The order between them is the first player's choice and this does
        // not ask.** `rr:forced.5`: "if two or more forced abilities would
        // initiate at the same moment, the first player determines the order in
        // which the abilities initiate" -- and a card carrying surge and its own
        // When Revealed text has exactly two. The prompt is not implemented, so
        // the order here is fixed and deterministic rather than chosen. See
        // MARVEL-187.
        Reveal.Keywords(world, facts, card, player, events);
        events.AddRange(abilities.WhenRevealed(world, card, player));

        // `rr:quickstrike.2` puts this after the card's own abilities, and it
        // is the one keyword that does something *after* them rather than
        // beside them.
        Reveal.Quickstrike(world, facts, card, player, round);

        // Step 4. "If the card is a treachery, discard it." Asked as "is it
        // still where step 2 left something not in play", so that an ability
        // that put the card somewhere is not undone.
        if (card.Area.Type != DeckType.RevealingArea)
        {
            return;
        }

        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        var from = card.Area;
        World.MoveToTop(card, discard);
        events.Add(new CardsMoved(
            Places.Reference(from),
            Places.Reference(discard),
            [new Landing(card.ObjectId, discard.Cards.Count - 1)])
        {
            Trigger = "villain phase", Verb = "Reveal",
        });
    }

    /// <summary>Step 5. <c>rr:villain-phase.step.5</c>, to the next clockwise player.</summary>
    private static void PassFirstPlayerToken(World world) =>
        world.FirstPlayer = world.Players > 0 ? (world.FirstPlayer + 1) % world.Players : 0;

    /// <summary>Ends the game when the last main scheme completes.</summary>
    /// <remarks>
    /// <para>
    /// <c>rr:main-scheme-main-scheme-deck.2</c>: "If the amount of threat on a
    /// main scheme is equal to or greater than its target threat value, that
    /// main scheme is completed and the main scheme deck advances. <b>If the
    /// villain completes the final stage of the main scheme deck, the villain
    /// wins the game.</b>"
    /// </para>
    /// <para>
    /// <b>Only the final stage is implemented, because it is the one the
    /// recording reaches.</b> The Rhino scenario's main scheme deck holds one
    /// card, so completing it is the villain winning. Advancing to a next stage
    /// is three more steps of that same rule — remove, resolve, flip and place
    /// starting threat — and it throws rather than being skipped, because a
    /// scheme that completed and did not advance would sit there accumulating
    /// threat forever.
    /// </para>
    /// <para>
    /// Checked after each placement rather than at the end of the phase: the
    /// engine's own log completes the scheme in the middle of the villain's
    /// activation, and never deals the encounter cards that would have followed.
    /// The recorded game ends there, on step 6, which is why the fixture holds
    /// seven steps of a twenty-step request.
    /// </para>
    /// </remarks>
    private static void CheckCompleted(
        World world, ICardFacts facts, ICardAbilities abilities, Card scheme,
        List<GameEvent> events)
    {
        long threat = scheme.Tokens.TryGetValue("k_threat", out long held) ? held : 0;
        long target = facts.PrintedValue(scheme.FaceId, "TargetThreat", world.Players);
        if (target <= 0 || threat < target)
        {
            return;
        }

        // `rr:main-scheme-main-scheme-deck.2`: the scheme is completed either
        // way. `.2.2` is the converse and the reason this flag is set here
        // rather than inside `Advance` -- "if the main scheme advances other
        // than through having threat on it equal to or greater than its target
        // threat value, that main scheme is **not** considered completed."
        scheme.PlaceTokens("is_completed", 1);
        events.Add(new FieldSet(scheme.ObjectId, "is_completed", 0, 1)
        {
            Trigger = "main scheme completed", Verb = "Complete",
        });

        if (world.AreaOf(DeckType.MainSchemesDeck).Cards.Count > 0)
        {
            MainScheme.Advance(world, facts, abilities, scheme, "main scheme completed", events);
            return;
        }

        // `.2.1` -- "if the villain completes the final stage of the main
        // scheme deck, the villain wins the game."
        world.Finish(Outcome.VillainWins);
    }

    private static void Threat(Card scheme, long amount, string trigger, List<GameEvent> events)
    {
        if (amount == 0)
        {
            return;
        }

        long before = scheme.Tokens.TryGetValue("k_threat", out long held) ? held : 0;
        scheme.PlaceTokens("k_threat", amount);
        events.Add(new FieldSet(scheme.ObjectId, "k_threat", before, before + amount)
        {
            Trigger = trigger, Verb = "Place_Threat",
        });
    }
}
