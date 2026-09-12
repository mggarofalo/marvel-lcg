using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
internal static class ActionAbilityTypedAreaProjectionFixtures
{
    internal const string RemoveDiscardedMercenary = """
        {"removeFromGame":{"cardsIn":{"area":"encounterDiscardPile","title":"Hydra Mercenary"}}}
        """;
    internal static (World World, Card Source, Card Minion, Card Discarded) AreaProjectionBoard(bool tough = false)
    {
        Card? source = null;
        Card? minion = null;
        Card? discarded = null;
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 1);
            discarded = board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        if (tough)
            Statuses.Give(world, minion!, Statuses.Tough);
        return (world, source!, minion!, discarded!);
    }

    internal static (AbilityRunner Runner, Dictionary<string, AbilityValue> Fields) MutableAreaSequenceRunner(string sequence, int index)
    {
        var parsed = AbilityCatalog.Parse($$"""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},"effect":{"seq":{{sequence}} }
            }]}]}
            """);
        var steps = ((AbilityValue.List)parsed.Abilities[0].Effect.Argument).Values.ToList();
        var step = AbilityNode.Of(steps[index]);
        var fields = ((AbilityValue.Map)step.Argument).Entries.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
        steps[index] = new AbilityValue.Map(new Dictionary<string, AbilityValue> { [step.Kind] = new AbilityValue.Map(fields), });
        return (new AbilityRunner(new AbilityBook([parsed.Abilities[0] with { Effect = new AbilityNode("seq", new AbilityValue.List(steps)) }], parsed.Authored)), fields);
    }
}
