using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

// Actual-row nonpublic setup for a two-seat split followed by a printed II defeat.
public sealed class KangReunionFixture : CoreCardTestBase
{
    public static (World World, Card Kang, Card Side, Card[] Minions) Board(int first, bool expert, bool observer = false, uint seed = 42)
    {
        var world = new World(Cards, 2, seed) { FirstPlayer = first };
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == 0 ? "01001a,01001b" : "01019a,01019b", seat.Hero);
        }
        var scheme = world.CreateCard("11007a,11007b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.TurnTo("11007b");
        var kang = world.CreateCard(expert ? "11034" : "11001", world.AreaOf(DeckType.VillainArea));
        var aside = world.AreaOf(DeckType.AsideDeck);
        world.CreateCard("11008a,11008b", world.AreaOf(DeckType.MainSchemesDeck));
        for (int index = 0; index < 4; index++)
        {
            world.CreateCard($"{11009 + index}a,{11009 + index}b", aside);
            world.CreateCard((expert ? 11035 + index : 11002 + index).ToString(System.Globalization.CultureInfo.InvariantCulture), aside);
        }
        var side = world.CreateCard("01098", world.AreaOf(DeckType.SideSchemesArea));
        Card[] minions = [world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))),
            world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)))];
        for (int count = 0; count < 6; count++) world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        world.Abilities = BuildAbilities(kang.FaceId, expert, observer);
        return (world, kang, side, minions);
    }

    private static AbilityRunner BuildAbilities(string stageOneFace, bool expert, bool observer)
    {
        var rows = new List<object>();
        rows.Add(Row(stageOneFace, "WhenDefeated", "captureKangStageOneDefeat"));
        rows.Add(Row("11008a", "WhenRevealed", "splitKangStageTwo"));
        rows.Add(new { card = "11008b", abilities = Array.Empty<object>() });
        for (int index = 0; index < 4; index++)
        {
            rows.Add(Row((expert ? 11035 + index : 11002 + index).ToString(System.Globalization.CultureInfo.InvariantCulture), "WhenDefeated", "captureKangStageTwoDefeat"));
            rows.Add(Row($"{11009 + index}a", "WhenRevealed", "createKangGameArea"));
            rows.Add(new { card = $"{11009 + index}b", abilities = Array.Empty<object>() });
        }
        if (observer) rows.Add(new { card = "01099", abilities = new[] { new {
            trigger = new { @event = "WhenCardDefeated", timing = "ForcedInterrupt", subject = "attachedTo" },
            effect = new { placeThreat = new { scheme = new { query = "mainScheme" }, amount = 1 } } } } });
        return new AbilityRunner(AbilityCatalog.Parse(JsonSerializer.Serialize(new { cards = rows })));
    }

    private static object Row(string card, string timing, string effect) => new { card, abilities = new[] { new {
        trigger = new { @event = timing == "WhenDefeated" ? "WhenCardDefeated" : "WhenCardRevealed", timing, subject = "this" },
        effect = new Dictionary<string, string> { [effect] = "this" } } } };
}
