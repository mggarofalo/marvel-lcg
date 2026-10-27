using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Resolves the printed singular Invocation deck independently of the acting player.</summary>
internal static class AbilityInvocationTopQuery
{
    internal static IReadOnlyList<Card> Live(World world)
    {
        // 09001b/09002 say "the Invocation deck", while Wong heals "your identity".
        // pack:mc08:the-invocation-deck binds that deck to Doctor Strange's identity.
        // Engine choice: an ambiguous research board fails closed rather than selecting a seat.
        var decks = world.Seats.Select(seat => seat.InvocationDeck).OfType<InvocationDeck>().ToArray();
        if (decks.Length > 1)
            throw new RulesNotImplementedException("the singular Invocation deck is ambiguous");
        return decks.Length == 1 && decks[0].PublicTop is { } top ? [top] : [];
    }
}
