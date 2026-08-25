namespace Marvel.Rules.Timing;

/// <summary>
/// Keywords, as <see cref="ContinuousEffect.Kind"/> spells them.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:keywords</c>: "keywords are abilities that have a specific game
/// effect", and <c>rr:overkill.1</c> writes one out as the ability it is
/// equivalent to. So a keyword granted to something is a continuous effect
/// naming it, not a flag: a card that grants one for the duration of an attack
/// registers it with that duration and it expires on its own.
/// </para>
/// <para>
/// Only the ones something in this engine grants. A keyword printed on a card is
/// read from the printed data instead — nothing needs to register what is
/// already on the card.
/// </para>
/// </remarks>
public static class Keywords
{
    /// <summary>
    /// Overkill — <c>rr:overkill</c>. Excess damage from an attack that defeats
    /// an ally goes to that ally's controller, and from one that defeats a
    /// minion to the villain.
    /// </summary>
    public const string Overkill = "overkill";
}
