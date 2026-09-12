using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public abstract class ChoosingCardsTestBase
{
    protected const string Campaign = "rhino";
    protected const uint Seed = 12345;
    protected static readonly SetupCatalog Setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    protected static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    protected static (Card Card, IReadOnlyList<Marvel.Rules.Events.GameEvent> Events) Reveal(World world, string faceId, int player = 0)
    {
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
        return (card, AuthoredCards.Runner().WhenRevealed(world, card, player));
    }

    /// <summary>Nick Fury's core choice, isolated from the rest of his card.</summary>
    protected static AbilityRunner NickFuryRunner() => new(AbilityCatalog.Parse("""
        { "cards": [ { "card": "01084", "abilities": [ {
          "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                       "subject": "this" },
          "effect": { "choose": { "descriptions": [
            "Remove 2 threat from a scheme", "Draw 3 cards"
          ], "options": [
            { "removeThreat": {
                "scheme": { "query": "mainScheme" }, "amount": 2 } },
            { "draw": { "player": "you", "count": 3 } }
          ] } }
        } ] } ] }
        """));
    protected static World Deal(params string[] heroes)
    {
        string[] playing = heroes.Length > 0 ? heroes : ["spider_man"];
        return WorldSetup.DealWithoutCardAbilities(Cards, Blueprints.From(Dealer.DealOrder(Setup, Campaign, playing), Cards), [..playing.Select(hero => Setup.Hero(hero).Name)], Seed);
    }
}
