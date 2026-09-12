using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public abstract class ResolutionStatusTestBase
{
    protected static readonly SetupCatalog Setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    protected static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    protected static (World World, Card Card, Occurrence Occurrence, AbilityRunner Runner) Revealing(string face, string effect)
    {
        var world = WorldSetup.DealWithoutCardAbilities(Cards, Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards), ["Spider-Man"], 12345);
        var card = world.CreateCard(face, world.AreaOf(DeckType.RevealingArea));
        var occurrence = new Occurrence(1, [Steps.CardRevealed], Subject: card.ObjectId, Player: 0);
        string json = $$"""
            { "cards": [ { "card": "{{face}}", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
              "effect": {{effect}}
            } ] } ] }
            """;
        return (world, card, occurrence, new AbilityRunner(AbilityCatalog.Parse(json)));
    }

    protected static World Deal() => WorldSetup.DealWithoutCardAbilities(Cards, Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards), ["Spider-Man"], 12345);
}
