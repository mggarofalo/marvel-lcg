namespace Marvel.Rules.State;

/// <summary>Places cards at a specified end of an area's ordered contents.</summary>
public static class CardPlacement
{
    /// <summary>Moves a card to the bottom without shuffling or changing the other cards' order.</summary>
    public static void MoveToBottom(Card card, Area destination)
    {
        World.MoveToTop(card, destination);
        // Engine choice: bottom-first storage gives the landing index zero.
        destination.Replace(new[] { card }.Concat(destination.Cards.Where(other => other != card)).ToArray());
    }
}
