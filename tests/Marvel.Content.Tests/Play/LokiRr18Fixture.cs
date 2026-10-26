using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;

namespace Marvel.Content.Tests.Play;

/// <summary>Nonpublic actual Loki interpretation; this does not admit the pack.</summary>
internal static class LokiRr18Fixture
{
    internal static AbilityRunner Runner()
    {
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        var cards = book["cards"]!.AsArray();
        foreach (var row in cards.Where(row => row!["card"]!.GetValue<string>() == "06028").ToArray())
            cards.Remove(row);
        foreach (var row in JsonNode.Parse(Rows)!["cards"]!.AsArray())
            cards.Add(row!.DeepClone());
        return new(AbilityCatalog.Parse(book.ToJsonString()));
    }

    // Printed 06028 plus its mandatory Forced Interrupt erratum.
    // The executable spellings are engine choices. RR1.8 governs overkill.
    private const string Rows = """
        {"cards":[
          {"card":"06028","name":"Loki","abilities":[
            {"trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt","subject":"this"},
             "effect":{"forEachDiscarded":{
               "effect":{"discardTop":{"from":"encounterDeck","count":1}},
               "when":{"isKind":{"card":"that","kind":"treachery"}},
               "then":{"heal":{"card":"this","amount":{"damageOn":"this"}}}
             }}}
          ]}
        ]}
        """;
}
