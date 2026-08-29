using Marvel.Rules.Timing;

namespace Marvel.Rules.State;

/// <summary>
/// The traits a card has — <c>rr:traits</c>.
/// </summary>
/// <remarks>
/// <para>
/// "Many cards have one or more traits listed at the top of the text box and
/// printed in bold italics", and <c>rr:traits.1</c>: "traits have no inherent
/// effects on the game. Instead, some card abilities reference cards that
/// possess or lack specific traits."
/// </para>
/// <para>
/// <b>Printed is not all of them.</b> Super Strength's attached villain "gains
/// the <b>BRUTE</b> trait", and a card that reads only the printed list would
/// answer that the villain is not a brute while an attachment sitting under it
/// says otherwise. So a trait arrives two ways, exactly as a keyword does
/// (<see cref="Keywords.Has"/>), and this is the one place that knows both.
/// </para>
/// <para>
/// <c>rr:traits.2</c> is why a granted trait is not a card ability of its own:
/// "traits are not considered to be part of a card's printed text box for the
/// purpose of card abilities." Giving one is giving an attribute, so it is a
/// continuous effect naming the trait rather than text being copied.
/// </para>
/// </remarks>
public static class Traits
{
    /// <summary>The <see cref="ContinuousEffect.Kind"/> prefix a granted trait uses.</summary>
    /// <remarks>
    /// A prefix rather than a field of its own, for the reason
    /// <c>ContinuousEffect</c> gives: an entry has to be something that can be
    /// written down, and <c>Kind</c> is the string it is written down as. The
    /// same shape counts an ability's uses.
    /// </remarks>
    public const string Granted = "trait:";

    /// <summary>Every trait a card has, printed or granted.</summary>
    /// <param name="world">The board, for what is granted.</param>
    /// <param name="card">The card.</param>
    /// <param name="facts">The printed card data.</param>
    public static IReadOnlyList<string> Of(World world, Card card, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(facts);

        var printed = FacedownDrones.InherentTraits(card, facts);
        var active = world.Effects.Active();
        var lost = active
            .Where(effect => effect.AppliesTo(world, card)
                && effect.Kind.StartsWith(Characteristics.Lost + Granted, StringComparison.Ordinal))
            .Select(effect => effect.Kind[(Characteristics.Lost + Granted).Length..])
            .ToHashSet(StringComparer.Ordinal);
        List<string>? all = null;

        foreach (var effect in active)
        {
            if (effect.Affects != card.ObjectId
                || !effect.Kind.StartsWith(Granted, StringComparison.Ordinal))
            {
                continue;
            }

            string gained = effect.Kind[Granted.Length..];
            if (lost.Contains(gained))
            {
                continue;
            }
            all ??= [.. printed];
            if (!all.Contains(gained, StringComparer.Ordinal))
            {
                all.Add(gained);
            }
        }

        // The printed list unchanged when nothing was granted, which is the
        // common case by a very long way: one allocation per card per ask would
        // be paid on every trait question in the game.
        if (lost.Count == 0)
        {
            return all ?? printed;
        }

        all ??= [.. printed];
        all.RemoveAll(lost.Contains);
        return all;
    }

    /// <summary>Whether a card has one trait, printed or granted.</summary>
    /// <param name="world">The board.</param>
    /// <param name="card">The card.</param>
    /// <param name="trait">The trait, spelled as the printed data spells it.</param>
    /// <param name="facts">The printed card data.</param>
    public static bool Has(World world, Card card, string trait, ICardFacts facts) =>
        Of(world, card, facts).Contains(trait, StringComparer.Ordinal);
}
