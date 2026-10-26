using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual 08007 interpretation with synthetic suspension support; no pack admission.</summary>
internal static class PreparationResourceFixture
{
    internal static AbilityRunner Runner() => new(AbilityCatalog.Parse("""
        {"cards":[
          {"card":"08007","abilities":[{
            "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
            "resourceRestriction":"preparationCard","printedResources":"G",
            "cost":{"exhaust":"this"},"effect":{"generate":"G"}}]},
          {"card":"01030","abilities":[{
            "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
            "cost":{"dealDamage":{"cards":"this","amount":2}},"effect":{"generate":"G"}}]},
          {"card":"01092","abilities":[{
            "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
            "effect":{"draw":{"player":"you","count":1}}}]}
        ]}
        """));

    internal static (Game Game, World World, Card First, Card Second, Card Target) Board(
        bool hero = true, bool nested = false, string face = "08008")
    {
        Card first = null!, second = null!, target = null!;
        var (game, world) = Playing(board =>
        {
            foreach (var card in board.Seats[0].Hand.Cards.ToArray())
                World.MoveToTop(card, board.Seats[0].Deck);
            var upgrades = board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0);
            first = board.CreateCard("08007", upgrades);
            second = board.CreateCard("08007", upgrades);
            target = board.CreateCard(face, board.Seats[0].Hand);
            if (nested)
            {
                var generator = board.CreateCard("01030",
                    board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                generator.TakeDamage(2);
                InPlay(board, "01092");
            }
        }, hero: hero, heroes: ["spider_man", "captain_marvel"], abilities: Runner());
        foreach (var card in world.Seats[0].Hand.Cards.Where(card => card != target).ToArray())
            World.MoveToTop(card, world.Seats[0].Deck);
        return (game, world, first, second, target);
    }
}
