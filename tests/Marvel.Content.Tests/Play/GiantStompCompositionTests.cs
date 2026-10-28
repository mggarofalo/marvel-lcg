using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nonpublic actual-card composition research. The fixed villain anchor bounds
// these probes to a board without Guard; it is not a proposed production row.
public sealed class GiantStompCompositionTests
{
    private const string Splash = """
        {"dealAttackDamage":{"cards":{"query":"minions"},"amount":1,"simultaneous":1}}
        """;
    private const string LiveChoice = """
        {"chooseCard":{"from":{"query":"attackableEnemies"},"effect":{
          "dealAttackDamage":{"cards":"chosen","amount":8}}}}
        """;
    private const string FixedDamage = """
        {"dealAttackDamage":{"cards":{"query":"villain"},"amount":8}}
        """;

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void DeferredEnemyChoiceReachesPowerSuspensionRatherThanInvalidSelector(int minions)
    {
        var world = Board(Runner(LiveChoice), minions);
        var source = world.Seats[0].Hand.Cards.Single(card => card.FaceId == "12003");
        string before = world.Digest().Canonical();
        var failure = Assert.Throws<RulesNotImplementedException>(() =>
            ((AbilityRunner)world.Abilities).Actions(world, 0));
        Assert.Contains("suspends inside a labelled power", failure.Message, StringComparison.Ordinal);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Same(world.Seats[0].Hand, source.Area);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.All(world.Seats[0].Hand.Cards, card => Assert.Equal(DeckType.HandsArea, card.Area.Type));

        // The exact same cost, source, Giant form, resources and board admit
        // the supported fixed-target control. Payment cannot explain refusal.
        world.Abilities = Runner(FixedDamage);
        Assert.Contains(((AbilityRunner)world.Abilities).Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Contains("12003", AuthoredCards.Book.Authored);
    }

    [Rule("rr:attack-player-ability-type.2.1")]
    [Rule("rr:attack-player-ability-type.5")]
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void FixedTargetControlRetainsEveryAttackRecipient(int minions)
    {
        // "a single attack, even if that attack deals multiple instances of
        // damage"; the attacker "attacked each of those enemies".
        var runner = Runner(FixedDamage);
        var world = Board(runner, minions);
        var source = world.Seats[0].Hand.Cards.Single(card => card.FaceId == "12003");
        var recipients = world.Cards.Where(card => card.FaceId == "01102").ToArray();
        foreach (var recipient in recipients)
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Kind: "retaliate", Amount: 1, Affects: recipient.ObjectId));
        var game = Game.Begin(world, world.Facts, runner);
        while (game.Pending!.Affordances.Any(option => option.Verb == Game.ResolveMulligans))
            game.Resolve(Decision.Take(game.Pending.Affordances.Single(option => option.Verb == Game.ResolveMulligans).Id));
        var action = Assert.Single(game.Pending.Affordances, offer => offer.AnchorId == source.ObjectId);
        int[] paying = world.Seats[0].Hand.Cards.Where(card => card.FaceId is "12021" or "12023")
            .Select(card => card.ObjectId).ToArray();
        game.Resolve(new Decision(action.Id, [], paying,
            Allocations: ResourcePayment.Allocate(Assert.Single(action.Costs!), paying)));
        Assert.Equal(8, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.All(recipients, card => Assert.Equal(1, card.Damage));
        while (game.Pending!.Asking == Question.Order)
            game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_whole"));
        Assert.Equal(1,
            world.TheCardIn(DeckType.VillainArea)!.Tokens.GetValueOrDefault("c_recipient"));
        Assert.All(recipients, card => Assert.Equal(1, card.Tokens.GetValueOrDefault("c_recipient")));
        Assert.Equal(minions, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:attack-player-ability-type.2.1")]
    [Rule("rr:attack-player-ability-type.5")]
    [Rule("rr:retaliate-x.1")]
    [Fact]
    public void RepeatedRecipientHasOneResponseAndRetaliatesOnce()
    {
        // A labelled ability is "a single attack" and retaliate occurs after
        // the character "is attacked". Both sentences affect the same minion.
        const string tail = """
            {"dealAttackDamage":{"cards":{"titled":"Sandman"},"amount":8}}
            """;
        var runner = Runner(tail);
        var world = Board(runner, 1);
        var minion = world.Cards.Single(card => card.FaceId == "01102");
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "retaliate", Amount: 1, Affects: minion.ObjectId));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "health", Amount: 10, Affects: minion.ObjectId));
        var source = world.Seats[0].Hand.Cards.Single(card => card.FaceId == "12003");
        var game = Game.Begin(world, world.Facts, runner);
        while (game.Pending!.Affordances.Any(option => option.Verb == Game.ResolveMulligans))
            game.Resolve(Decision.Take(game.Pending.Affordances.Single(option => option.Verb == Game.ResolveMulligans).Id));
        var action = Assert.Single(game.Pending.Affordances, offer => offer.AnchorId == source.ObjectId);
        int[] paying = world.Seats[0].Hand.Cards.Where(card => card.FaceId is "12021" or "12023")
            .Select(card => card.ObjectId).ToArray();
        game.Resolve(new Decision(action.Id, [], paying,
            Allocations: ResourcePayment.Allocate(Assert.Single(action.Costs!), paying)));
        Assert.Equal(Question.Order, game.Pending!.Asking);
        Assert.Equal(9, Assert.Single(world.Agenda.Occurrence!.Attacks,
            recipient => recipient.Card == minion.ObjectId).DamageTaken);
        while (game.Pending!.Asking == Question.Order)
            game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));
        Assert.Equal(9, minion.Damage);
        Assert.Equal(1, minion.Tokens.GetValueOrDefault("c_recipient"));
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_whole"));
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:prevent.1.3")]
    [Rule("rr:retaliate-x.1")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PreventedDamageStillCountsAsAnAttackOfThatEnemy(bool protectMinion)
    {
        // An attack with all damage prevented is not "attacked and damaged";
        // retaliate triggers after a character "is attacked".
        var runner = Runner(FixedDamage);
        var world = Board(runner, 1);
        var minion = world.Cards.Single(card => card.FaceId == "01102");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var protectedEnemy = protectMinion ? minion : villain;
        Statuses.Give(world, protectedEnemy, Statuses.Tough);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "retaliate", Amount: 1, Affects: protectedEnemy.ObjectId));
        var source = world.Seats[0].Hand.Cards.Single(card => card.FaceId == "12003");
        var game = Game.Begin(world, world.Facts, runner);
        while (game.Pending!.Affordances.Any(option => option.Verb == Game.ResolveMulligans))
            game.Resolve(Decision.Take(game.Pending.Affordances.Single(option => option.Verb == Game.ResolveMulligans).Id));
        var action = Assert.Single(game.Pending.Affordances, offer => offer.AnchorId == source.ObjectId);
        int[] paying = world.Seats[0].Hand.Cards.Where(card => card.FaceId is "12021" or "12023")
            .Select(card => card.ObjectId).ToArray();
        game.Resolve(new Decision(action.Id, [], paying,
            Allocations: ResourcePayment.Allocate(Assert.Single(action.Costs!), paying)));
        Assert.Equal(Question.Order, game.Pending!.Asking);
        Assert.Equal(0, Assert.Single(world.Agenda.Occurrence!.Attacks,
            item => item.Card == protectedEnemy.ObjectId).DamageTaken);
        while (game.Pending!.Asking == Question.Order)
            game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));
        Assert.Equal(0, protectedEnemy.Damage);
        Assert.False(Statuses.Has(world, protectedEnemy, Statuses.Tough));
        Assert.Equal(1, protectedEnemy.Tokens.GetValueOrDefault("c_recipient"));
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    internal static AbilityRunner Runner(string tail, bool targetless = false, bool interrupt = false, bool optionalResponse = false)
    {
        var root = JsonNode.Parse(AuthoredCards.Text)!;
        var rows = root["cards"]!.AsArray();
        rows.Remove(rows.Single(row => row!["card"]!.GetValue<string>() == "01092"));
        rows.Remove(rows.Single(row => row!["card"]!.GetValue<string>() == "01093"));
        if (interrupt) rows.Remove(rows.Single(row => row!["card"]!.GetValue<string>() == "01091"));
        string target = targetless ? "" : "\"target\":{\"query\":\"villain\"},";
        bool actual = targetless && tail == """{"attackAnyEnemy":{"amount":8}}""";
        if (!actual) rows.Remove(rows.Single(row => row!["card"]!.GetValue<string>() == "12003"));
        if (!actual) root["cards"]!.AsArray().Add(JsonNode.Parse($$$$"""
            {"card":"12003","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "when":{"hasTrait":{"card":"you","trait":"GIANT"}},
              "effect":{"attack":{{{{{target}}}}"effect":{"seq":[{{{{Splash}}}},{{{{tail}}}}]}}}
            }]}
            """));
        root["cards"]!.AsArray().Add(JsonNode.Parse("""
            {"card":"01092","abilities":[{
              "trigger":{"event":"WhenAttackEnds","timing":"ForcedResponse","subject":"game"},
              "effect":{"placeCounters":{"card":"you","counter":"whole","count":1}}
            }]}
            """));
        root["cards"]!.AsArray().Add(JsonNode.Parse("""
            {"card":"01093","abilities":[{
              "trigger":{"event":"WhenAttackEnds","timing":"ForcedResponse","target":"enemy"},
              "effect":{"placeCounters":{"card":"trigger.target","counter":"recipient","count":1}}
            }]}
            """));
        if (optionalResponse)
            rows.Single(row => row!["card"]!.GetValue<string>() == "01093")!["abilities"]![0]!["trigger"]!["timing"] = "Response";
        if (interrupt) root["cards"]!.AsArray().Add(JsonNode.Parse("""
            {"card":"01091","abilities":[{
              "trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","target":"minion"},
              "effect":{"choose":{"options":[{"preventDamage":"trigger.target"},{"draw":{"player":"you","count":1}}]}}
            }]}
            """));
        return new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
    }

    internal static World Board(AbilityRunner runner, int minions)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, 1, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        seat.IdentityCard.TurnTo("12001c");
        for (int i = 0; i < 12; i++) world.CreateCard("01087", seat.Deck);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01095", world.AreaOf(DeckType.VillainDeck));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 12; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        for (int i = 0; i < minions; i++)
            world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("12003", seat.Hand);
        world.CreateCard("12021", seat.Hand);
        world.CreateCard("12023", seat.Hand);
        world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01093", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Abilities = runner;
        return world;
    }
}
