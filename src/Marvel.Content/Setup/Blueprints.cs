using Marvel.Rules.State;

namespace Marvel.Content.Setup;

/// <summary>Turns a deal order into something the rules can lay out.</summary>
/// <remarks>
/// The content layer knows <i>why</i> a card exists; the rules layer only needs
/// to know <i>where it goes</i>. Several sources collapse into one slot — a
/// hero's signature cards and their aspect cards are two different
/// deck-building questions and the same answer about placement.
/// </remarks>
public static class Blueprints
{
    /// <summary>The deal order, as blueprints.</summary>
    /// <param name="dealt">The creations, in allocation order.</param>
    public static IReadOnlyList<CardBlueprint> From(IReadOnlyList<Creation> dealt)
    {
        ArgumentNullException.ThrowIfNull(dealt);
        return [.. dealt.Select(creation => new CardBlueprint(
            creation.Spec, SlotFor(creation.Source), creation.Player))];
    }

    private static SetupSlot SlotFor(CreationSource source) => source switch
    {
        CreationSource.Rules => SetupSlot.Rules,
        CreationSource.Challenge => SetupSlot.Challenge,
        CreationSource.Identity => SetupSlot.Identity,
        CreationSource.Obligation => SetupSlot.Obligation,
        CreationSource.Nemesis => SetupSlot.Nemesis,
        CreationSource.HeroDeck or CreationSource.PlayerDeck => SetupSlot.PlayerDeck,
        CreationSource.MainScheme => SetupSlot.MainScheme,
        CreationSource.Villain => SetupSlot.Villain,
        CreationSource.Encounter or CreationSource.EncounterSet => SetupSlot.Encounter,
        _ => throw new ArgumentOutOfRangeException(nameof(source)),
    };
}
