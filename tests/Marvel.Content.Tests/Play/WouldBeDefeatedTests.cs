using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public abstract class WouldBeDefeatedTestBase
{
    protected const string Mercenary = "01101";
    protected const string Sandman = "01102";
    protected const string Modok = "01184";
    protected static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    protected static (World World, Card Minion, Card Upgrade) Board()
    {
        var world = Empty();
        var minion = world.CreateCard(Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var upgrade = world.CreateCard(AuthoredCards.BiomechanicalUpgrades, world.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId));
        return (world, minion, upgrade);
    }

    protected static World Empty(string? abilities = null)
    {
        var world = new World(Cards, players: 1);
        world.CreateSeat("p0");
        world.Abilities = abilities is null ? AuthoredCards.Runner() : new AbilityRunner(AbilityCatalog.Parse(abilities));
        return world;
    }
}
