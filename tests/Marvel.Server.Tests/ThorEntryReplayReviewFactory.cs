using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;

namespace Marvel.Server.Tests;

/// <summary>Actual rows on a prepared core board through the durable factory seam.</summary>
internal sealed class ThorEntryReplayReviewFactory(string face) : IDurableGameFactory
{
    public SessionCompatibility Compatibility { get; } = DatasetGameFactory.Load(RepositoryPaths.Root).Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Ally { get; private set; }
    internal int Minion { get; private set; }
    internal int[] Payment { get; private set; } = [];

    public OpenedGame Create(GameSpecification specification)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse(File.ReadAllText(
            RepositoryPaths.Dataset("abilities", "abilities.json"))));
        var world = WorldSetup.DealWithoutCardAbilities(cards,
            Blueprints.From(Dealer.DealOrder(setup, "rhino", ["spider_man"]), cards),
            [setup.Hero("spider_man").Name], specification.Seed);
        world.Seats[0].IdentityCard.TurnTo("01001a");
        var hand = world.Seats[0].Hand;
        Ally = world.CreateCard(face, hand).ObjectId;
        Minion = world.CreateCard("01130", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))).ObjectId;
        Payment = face == "06012"
            ? [world.CreateCard("06016", hand).ObjectId, world.CreateCard("01003", hand).ObjectId]
            : Enumerable.Range(0, 5).Select(_ => world.CreateCard("01003", hand).ObjectId).ToArray();
        var deck = world.AreaOf(DeckType.EncounterDeck);
        foreach (var card in deck.Cards.Skip(4).ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.EncounterDiscardPile));
        Game = Marvel.Rules.Play.Game.Begin(world, cards, runner);
        while (Game.Pending is { } prompt && prompt.Affordances.Any(a => a.Verb == Marvel.Rules.Play.Game.ResolveMulligans))
            Game.Resolve(Decision.Take(prompt.Affordances.Single(a => a.Verb == Marvel.Rules.Play.Game.ResolveMulligans).Id));
        return new(Game, []);
    }
}
