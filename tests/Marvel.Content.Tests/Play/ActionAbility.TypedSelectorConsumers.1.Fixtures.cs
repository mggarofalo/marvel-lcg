using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;
internal static class ActionAbilityTypedSelectorConsumersFixtures
{
    internal static (string Effect, string Face, DeckType Area) AreaChangeCase(string operation) => operation switch
    {
        "discard" => ("""{"discard":{"titled":"Shocker"}}""", "01103", DeckType.EngagedEnemiesArea),
        "moveDamage" => ("""{"moveDamage":{"from":"you","to":{"titled":"Shocker"},"amount":1}}""", "01103", DeckType.EngagedEnemiesArea),
        "removeThreat" => ("""{"removeThreat":{"scheme":{"query":"sideSchemes"},"amount":1}}""", "01127", DeckType.SideSchemesArea),
        _ => ("""{"putIntoPlay":{"card":{"cardsIn":{"area":"encounterDiscardPile","kind":"Minion"}},"where":"engagedWithYou"}}""", "01103", DeckType.EncounterDiscardPile),
    };
    internal static (Card Source, Card Affected) PrepareAreaChange(World board, string operation, string face, DeckType area)
    {
        var source = InPlay(board, AuthoredCards.AuntMay);
        var destination = area == DeckType.EngagedEnemiesArea ? board.AreaOf(area, PlayArea.Of(0)) : board.AreaOf(area);
        var affected = board.CreateCard(face, destination);
        if (operation == "moveDamage")
        {
            affected.TakeDamage(2);
            board.Seats[0].IdentityCard.TakeDamage(1);
        }

        if (operation == "removeThreat")
            affected.PlaceTokens("k_threat", 1);
        return (source, affected);
    }

    internal static void AssertAreaChangePreserved(string operation, Card affected, World world)
    {
        Assert.Equal(operation == "moveDamage" ? 2 : 0, affected.Damage);
        Assert.Equal(operation == "moveDamage" ? 1 : 0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(operation == "removeThreat" ? 1 : 0, affected.Tokens.GetValueOrDefault("k_threat"));
    }

    internal static (string Target, string Body, string? Cards) PowerCase(string operation, bool choice)
    {
        string target = choice ? "\"chosen\"" : operation == "attack" ? """{"titled":"Shocker"}""" : """{"query":"mainScheme"}""";
        string body = operation == "attack" ? """{"dealAttackDamage":{"cards":"chosen","amount":1}}""" : """{"removeThreat":{"scheme":"chosen","amount":1}}""";
        string? cards = choice ? operation == "attack" ? "minions" : "schemes" : null;
        return (target, body, cards);
    }

    internal static void ResolvePowerChoice(Game game, string operation, bool choice, Card minion, Card main)
    {
        if (!choice)
            return;
        Assert.Equal(Question.Element, game.Pending!.Asking);
        int chosen = operation == "attack" ? minion.ObjectId : main.ObjectId;
        Assert.Equal([chosen], game.Pending.Affordances.Select(option => option.Id));
        game.Resolve(Decision.Take(chosen));
    }

    internal static void AssertPowerOutcome(string operation, long threat, Card minion, Card main)
    {
        Assert.Equal(operation == "attack" ? 1 : 0, minion.Damage);
        Assert.Equal(operation == "thwart" ? threat - 1 : threat, main.Tokens["k_threat"]);
    }
}
