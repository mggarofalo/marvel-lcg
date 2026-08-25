using Marvel.Rules.Events;
using Marvel.Rules.Fold;
using Marvel.Rules.State;

namespace Marvel.Content.Cards;

/// <summary>
/// The card behaviour a recorded transition actually reaches, written out.
/// </summary>
/// <remarks>
/// <para>
/// <b>A placeholder with an expiry date, and it is worth being blunt about
/// that.</b> <c>docs/migration.md</c> settled that cards become data rather
/// than code, and <c>docs/card-dsl.md</c> designs the interpreter that runs
/// them — opening with "nothing here is implemented". This is what stands in
/// until it exists.
/// </para>
/// <para>
/// It is not a parallel path, because there is exactly one way a card's
/// behaviour enters the fold — <see cref="ICardAbilities"/> — and the
/// interpreter replaces what is behind it without the villain phase or the fold
/// changing. It is also deliberately tiny: <b>one card</b>, the one the recorded
/// milestone game reveals in round one. Every card added here before the
/// interpreter exists is a card that has to be removed again, so the rule is to
/// add one only when a recorded step reaches it.
/// </para>
/// <para>
/// Ported by reading <c>py_src/cards/pack/core/rhino/01105.py</c>, which is the
/// oracle's own implementation.
/// </para>
/// </remarks>
public sealed class CoreSetAbilities : ICardAbilities
{
    /// <summary>The printed id of "I'm Tough".</summary>
    public const string ImTough = "01105";

    /// <summary>The printed id of "Charge".</summary>
    public const string Charge = "01099";

    /// <summary>The cards this knows about.</summary>
    /// <remarks>
    /// Public so a test can state the coverage gap as a set rather than
    /// discovering it as a failure. A recorded step that reveals anything else
    /// resolves to nothing here, which is why <see cref="WhenRevealed"/> throws
    /// rather than returning empty.
    /// </remarks>
    public static IReadOnlySet<string> Implemented { get; } =
        new HashSet<string>(StringComparer.Ordinal) { ImTough, Charge };

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        return card.FaceId switch
        {
            ImTough => ImToughRevealed(world, card),
            Charge => ChargeRevealed(world, card),
            _ => throw new RulesNotImplementedException(
                $"card '{card.FaceId}' was revealed and no ability is written for it; "
                + $"this engine knows {Implemented.Count} card(s), pending the interpreter"),
        };
    }

    // "Charge" (01099). Printed: "Attach to Rhino." The Python is
    // `AbilityFactory.AttachToFaceWhenPutIntoPlay(CardFinder(name="Rhino"))`,
    // which is the declarative half of the ability -- the imperative handler is
    // the Forced Interrupt that fires when Rhino *attacks*, and no recorded step
    // reaches it because the hero never leaves alter-ego form.
    //
    // The +3 attack is not here. It is `ATK+ 3` in the printed data, and
    // `StateFields` reads it off whatever is attached: a card that prints a
    // modifier does not need an ability to apply it.
    private static IReadOnlyList<GameEvent> ChargeRevealed(World world, Card card)
    {
        var villain = world.TheCardIn(DeckType.VillainArea);
        if (villain is null)
        {
            throw new RulesNotImplementedException(
                "\"Charge\" attaches to Rhino and there is no villain in play");
        }

        var onto = world.AreaOf(
            DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId,
            villain.Area.CardOwner);
        var from = card.Area;
        World.MoveToTop(card, onto);

        return
        [
            new CardsMoved(
                Places.Reference(from), Places.Reference(onto),
                [new Landing(card.ObjectId, onto.Cards.Count - 1)])
            {
                Trigger = "WhenCardRevealed", Verb = "Attach",
            },
            new CardAttached(card.ObjectId, villain.ObjectId)
            {
                Trigger = "WhenCardRevealed", Verb = "Attach",
            },
        ];
    }

    // "I'm Tough" (01105). The Python is:
    //
    //     villain = Worlds.FindCardOnField(effect, name="Rhino", card_type=Villain)
    //     if villain and not villain.IsTough():
    //         Faces.GiveStatus([villain], "Tough", effect)
    //     else:
    //         this.GainSurge(1, effect)
    //
    // The card names Rhino because it belongs to Rhino's encounter set and no
    // other villain can be on the table with it. Matching on "the villain in
    // play" rather than on the name is the same answer here and does not need a
    // name lookup the dataset spells differently.
    private static IReadOnlyList<GameEvent> ImToughRevealed(World world, Card card)
    {
        var villain = world.TheCardIn(DeckType.VillainArea);
        if (villain is null || Statuses.Has(world, villain, Statuses.Tough))
        {
            // Surge means "reveal an additional encounter card", which is a
            // villain-phase rule and not this card's. Left out rather than
            // guessed: the recorded round one takes the other branch, and a
            // surge that silently did nothing would deal one card too few for
            // the rest of the game.
            throw new RulesNotImplementedException(
                "\"I'm Tough\" would surge because the villain is already Tough; "
                + "surge is not implemented");
        }

        var status = Statuses.Give(world, villain, Statuses.Tough);
        return
        [
            new CardsCreated(
                Places.Reference(status.Area),
                [new CreatedCard(status.ObjectId, status.FaceId)])
            {
                Trigger = "WhenCardRevealed", Verb = "Give_Status",
            },
            new CardAttached(status.ObjectId, villain.ObjectId)
            {
                Trigger = "WhenCardRevealed", Verb = "Give_Status",
            },
        ];
    }
}
