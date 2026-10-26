using System.Text.Json.Nodes;
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

// Synthetic composition through the durable factory seam; no public pack admission.
internal sealed class CounterattackReplayReviewFactory(bool promote) : IDurableGameFactory
{
    public SessionCompatibility Compatibility { get; } = DatasetGameFactory.Load(RepositoryPaths.Root).Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Counter { get; private set; }
    internal int Suit { get; private set; }
    internal int Action { get; private set; }
    internal int Enemy { get; private set; }
    internal int Successor { get; private set; } = -1;

    public OpenedGame Create(GameSpecification specification)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var book = JsonNode.Parse("""
            {"cards":[
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
                "cost":{"exhaust":"this"},
                "effect":{"enemyAttacks":{"enemies":{"query":"villain"},"dealBoostCards":"false"}}
              }]},
              {"card":"08030","abilities":[{
                "trigger":{"event":"WhenDamageDealt","timing":"Response","actor":"enemy","target":"you","form":"hero"},
                "when":{"atLeast":{"value":{"receivedAttackDamage":"game"},"count":1}},
                "labels":["attack"],"cost":{"discard":"this"},
                "effect":{"attack":{"target":"trigger.actor","effect":{
                  "dealAttackDamage":{"cards":"trigger.actor","amount":{"receivedAttackDamage":"game"}}
                }}}
              }]},
              {"card":"08009","abilities":[{
                "trigger":{"event":"WhenPreparationAbilityResolved","timing":"Response","subject":"you"},
                "cost":{"exhaust":"this"},"effect":{"ready":"you"}
              }]}
            ]}
            """)!;
        var authored = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        foreach (var row in authored["cards"]!.AsArray())
        {
            string face = row!["card"]!.GetValue<string>();
            if (face is not ("01092" or "08009" or "08030"))
                book["cards"]!.AsArray().Add(row.DeepClone());
        }
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var world = WorldSetup.DealWithoutCardAbilities(cards,
            Blueprints.From(Dealer.DealOrder(setup, "rhino", ["spider_man"]), cards),
            [setup.Hero("spider_man").Name], specification.Seed);
        var hero = world.Seats[0].IdentityCard;
        hero.TurnTo("01001a");
        hero.Exhaust();
        var upgrades = world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0);
        Counter = world.CreateCard("08030", upgrades).ObjectId;
        Suit = world.CreateCard("08009", upgrades).ObjectId;
        Action = world.CreateCard("01092",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0)).ObjectId;
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        Enemy = enemy.ObjectId;
        if (promote)
        {
            Successor = world.AreaOf(DeckType.VillainDeck).Cards.Single(card => card.FaceId == "01095").ObjectId;
            enemy.TakeDamage(Damage.Health(world, cards, enemy) - 1);
        }
        world.Effects.Register(new(EffectSource.LastingEffect, "retaliate", 1, Affects: hero.ObjectId));
        world.Effects.Register(new(EffectSource.LastingEffect, "preventDamage", 1,
            Affects: hero.ObjectId, Lasts: Duration.NextUses(1)));
        Game = Marvel.Rules.Play.Game.Begin(world, cards, runner);
        for (int answer = 0; answer < 30; answer++)
        {
            var prompt = Game.Pending ?? throw new InvalidOperationException("Missing setup prompt");
            if (prompt.PublicKind == PublicDecisionKind.PlayerAction) return new(Game, []);
            Game.Resolve(prompt.Cancellable ? Decision.Decline : Decision.Take(prompt.Affordances[0].Id));
        }
        throw new InvalidOperationException("Synthetic prelude did not reach a player action");
    }
}
