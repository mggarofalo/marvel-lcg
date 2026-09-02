namespace Marvel.Cards.Extract;

/// <summary>
/// One card face, as <c>datasets/cards/cards.json</c> records it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Flat, and only what is read.</b> The dataset this replaces nested the
/// engine-facing half under an <c>engine</c> key beside a copy of MarvelSDB's
/// own record, and the two disagreed about eleven things. There is one set of
/// facts about a printed card, so there is one place for each of them.
/// </para>
/// <para>
/// <see cref="Text"/> keeps upstream's markup and <see cref="Plain"/> does not.
/// Both, because they answer different questions: <c>CardCatalog</c> reads the
/// plain text for "does this card print a Boost ability", and somebody
/// authoring a card needs the bold markers that say which ability is which.
/// </para>
/// </remarks>
/// <param name="Id">The face id, which is MarvelSDB's card code.</param>
/// <param name="Name">The printed title — <c>rr:identity.2</c> makes it name one card.</param>
/// <param name="Subname">The subtitle, which <c>rr:team-up.2</c> also matches on.</param>
/// <param name="Kind">The engine's name for the card type.</param>
/// <param name="Traits">The printed traits, upper-cased.</param>
/// <param name="Attributes">Everything printed on the card the engine reads.</param>
/// <param name="LinkedTo">Exact face ids resolved from a printed Linked title.</param>
/// <param name="Text">The text box, with upstream's markup.</param>
/// <param name="Pack">The product it was printed in.</param>
/// <param name="Set">The hero or encounter set it belongs to.</param>
internal sealed record Card(
    string Id,
    string Name,
    string Subname,
    string Kind,
    IReadOnlyList<string> Traits,
    IReadOnlyDictionary<string, string> Attributes,
    IReadOnlyList<string> LinkedTo,
    string Text,
    string Pack,
    string Set)
{
    /// <summary>The text box with the markup taken out.</summary>
    public string Plain => Printed.Plain(Text);
}
