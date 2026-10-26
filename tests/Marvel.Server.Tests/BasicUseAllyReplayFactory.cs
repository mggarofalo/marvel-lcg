using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Session;
using Marvel.Tests;

namespace Marvel.Server.Tests;

/// <summary>A synthetic matching-power scene using the existing durable factory seam.</summary>
internal sealed class BasicUseAllyReplayFactory(int player) : IDurableGameFactory
{
    public SessionCompatibility Compatibility { get; } = DatasetGameFactory.Load(RepositoryPaths.Root).Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Source { get; private set; }
    internal int Ally { get; private set; }

    public OpenedGame Create(GameSpecification specification)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"06032","abilities":[{
              "trigger":{"event":"WhenHeroBasicUseInitiated","timing":"Interrupt","actor":"you","form":"hero","player":"you"},
              "cost":{"exhaustChosen":{"from":{"query":"alliesYouControl"},"count":1,"matchingBasicPower":"true"}},
              "effect":{"seq":[{"choose":{"options":[{"draw":{"player":"you","count":1}},{"draw":{"player":"you","count":2}}]}},{"boostBasicUse":"exhaustedAllyPower"}]}
            }]}]}
            """));
        var world = WorldSetup.DealWithoutCardAbilities(cards,
            Blueprints.From(Dealer.DealOrder(setup, "rhino", ["spider_man", "captain_marvel"]), cards),
            [setup.Hero("spider_man").Name, setup.Hero("captain_marvel").Name], specification.Seed);
        world.FirstPlayer = player;
        Forms.Change(world.Seats[player], cards);
        Source = world.CreateCard("06032", world.Seats[player].Hand).ObjectId;
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player));
        Ally = ally.ObjectId;
        world.Effects.Register(new(EffectSource.LastingEffect, "attack", 3, Card: Ally, Affects: Ally));
        world.Effects.Register(new(EffectSource.LastingEffect, "thwart", 2, Card: Ally, Affects: Ally));
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 10);
        Game = Marvel.Rules.Play.Game.Begin(world, cards, runner);
        for (int answer = 0; answer < 30; answer++)
        {
            var prompt = Game.Pending ?? throw new InvalidOperationException("Missing setup prompt");
            if (prompt.PublicKind == PublicDecisionKind.PlayerAction) return new(Game, []);
            Game.Resolve(prompt.Cancellable ? Decision.Decline : Decision.Take(prompt.Affordances[0].Id));
        }
        throw new InvalidOperationException("The synthetic prelude did not reach a player action");
    }
}

