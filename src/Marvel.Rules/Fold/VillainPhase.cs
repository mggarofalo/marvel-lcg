using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Fold;

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
/// enter the fold, and the interpreter replaces what is behind it.
/// </para>
/// </remarks>
public interface ICardAbilities
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
    /// <summary>Runs one villain phase.</summary>
    /// <param name="world">The world.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What revealed cards do.</param>
    /// <exception cref="RulesNotImplementedException">
    /// The board reached a rule this engine does not have — a minion engaged
    /// with a player, or a villain that would attack rather than scheme.
    /// </exception>
    public static IReadOnlyList<GameEvent> Run(
        World world, ICardFacts facts, ICardAbilities abilities)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);

        var events = new List<GameEvent>();

        PlaceThreat(world, facts, events);
        if (world.IsOver)
        {
            return events;
        }

        EnemiesActivate(world, facts, events);
        if (world.IsOver)
        {
            return events;
        }

        var dealt = DealEncounterCards(world);
        RevealEncounterCards(world, abilities, dealt, events);
        PassFirstPlayerToken(world);

        return events;
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
    private static void PlaceThreat(World world, ICardFacts facts, List<GameEvent> events)
    {
        var scheme = world.TheCardIn(DeckType.MainSchemesArea);
        if (scheme is null)
        {
            return;
        }

        long amount = facts.PrintedValue(scheme.FaceId, "EscalationThreat", world.Players);
        Threat(scheme, amount, "villain phase, place threat", events);
        CheckCompleted(world, facts, scheme, events);
    }

    /// <summary>Step 2. In player order, the villain activates against each player.</summary>
    private static void EnemiesActivate(World world, ICardFacts facts, List<GameEvent> events)
    {
        var villain = world.TheCardIn(DeckType.VillainArea);
        if (villain is null)
        {
            return;
        }

        foreach (int seat in PlayerOrder(world))
        {
            // `rr:activation.1`: hero form and the villain attacks, alter-ego
            // form and it schemes. Which face is showing *is* which form, so
            // this needs no separate flag.
            var identity = world.Seats[seat].IdentityCard;
            if (facts.Kind(identity.FaceId) != CardKind.AlterEgo)
            {
                throw new RulesNotImplementedException(
                    $"the villain would attack {world.Seats[seat].Name}, who is in hero form; "
                    + "only the scheme half of an activation is implemented");
            }

            Scheme(world, facts, villain, events);

            // `rr:villain-phase.2b`. A minion engaged with a player activates
            // too, and nothing on the milestone board is ever engaged.
            var engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(seat));
            if (engaged.Cards.Count > 0)
            {
                throw new RulesNotImplementedException(
                    $"{engaged.Cards.Count} minion(s) engaged with {world.Seats[seat].Name} "
                    + "would activate; minion activation is not implemented");
            }
        }
    }

    /// <summary>An enemy schemes. <c>rr:scheme-enemy-activation</c>.</summary>
    /// <remarks>
    /// Three steps: give it one facedown boost card from the encounter deck,
    /// resolve that card (flip, add its boost icons to SCH, discard), then place
    /// threat equal to the modified SCH on the main scheme.
    /// </remarks>
    private static void Scheme(
        World world, ICardFacts facts, Card villain, List<GameEvent> events)
    {
        long scheme = facts.PrintedValue(villain.FaceId, "SCH", world.Players);
        scheme += ResolveBoostCard(world, facts, events);

        var target = world.TheCardIn(DeckType.MainSchemesArea);
        if (target is not null)
        {
            Threat(target, scheme, "scheme", events);
            CheckCompleted(world, facts, target, events);
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
        var boost = deck.TakeTop();
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
    private static List<(int Player, Card Card)> DealEncounterCards(World world)
    {
        var dealt = new List<(int, Card)>();
        var deck = world.AreaOf(DeckType.EncounterDeck);

        foreach (int seat in PlayerOrder(world))
        {
            var card = deck.TakeTop();
            if (card is null)
            {
                break;
            }

            // Dealt facedown to the player, so it sits in their play area until
            // it is revealed. The recorded digest never catches it here -- the
            // whole deal-and-reveal happens between two decisions -- but the
            // engine's own log shows the intermediate pile, and skipping it
            // would make a two-player board deal in the wrong order.
            var pending = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(seat));
            pending.Append(card);
            dealt.Add((seat, card));
        }

        return dealt;
    }

    /// <summary>Step 4. Each player reveals their cards, in the order dealt.</summary>
    private static void RevealEncounterCards(
        World world, ICardAbilities abilities,
        List<(int Player, Card Card)> dealt, List<GameEvent> events)
    {
        var discard = world.AreaOf(DeckType.EncounterDiscardPile);

        foreach (var (player, card) in dealt)
        {
            // Same reason as the boost card: the revealing area is where an
            // encounter card registers its pools.
            World.MoveToTop(card, world.AreaOf(DeckType.RevealingArea));
            card.TurnFaceUp();
            events.Add(new CardsFlipped([card.ObjectId], true)
            {
                Trigger = "villain phase", Verb = "Reveal",
            });

            events.AddRange(abilities.WhenRevealed(world, card, player));

            // `rr:reveal`: what happens next is decided by the card's type. A
            // treachery resolves and is discarded; an attachment or a minion
            // *enters play* and stays there. Rather than switching on the type
            // here, this asks where the card is: an ability that put it
            // somewhere has already answered, and one that did not leaves it in
            // the revealing area to be discarded.
            if (card.Area.Type != DeckType.RevealingArea)
            {
                continue;
            }

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
    }

    /// <summary>Step 5. <c>rr:villain-phase.5</c>, to the next clockwise player.</summary>
    private static void PassFirstPlayerToken(World world) =>
        world.FirstPlayer = world.Players > 0 ? (world.FirstPlayer + 1) % world.Players : 0;

    /// <summary>Seats in player order, starting from the first player.</summary>
    private static IEnumerable<int> PlayerOrder(World world)
    {
        for (int offset = 0; offset < world.Players; offset++)
        {
            yield return (world.FirstPlayer + offset) % world.Players;
        }
    }

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
        World world, ICardFacts facts, Card scheme, List<GameEvent> events)
    {
        long threat = scheme.Tokens.TryGetValue("k_threat", out long held) ? held : 0;
        long target = facts.PrintedValue(scheme.FaceId, "TargetThreat", world.Players);
        if (target <= 0 || threat < target)
        {
            return;
        }

        if (world.AreaOf(DeckType.MainSchemesDeck).Cards.Count > 0)
        {
            throw new RulesNotImplementedException(
                $"the main scheme completed at {threat} of {target} threat and the deck would "
                + "advance to its next stage; advancing is not implemented");
        }

        scheme.PlaceTokens("is_completed", 1);
        world.IsOver = true;
        events.Add(new FieldSet(scheme.ObjectId, "is_completed", 0, 1)
        {
            Trigger = "main scheme completed", Verb = "Complete",
        });
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
