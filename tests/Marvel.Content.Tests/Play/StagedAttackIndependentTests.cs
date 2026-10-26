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

// Synthetic staged bodies exercise engine contracts without admitting Melee.
public sealed class StagedAttackIndependentTests
{
    [Rule("rr:event.5")]
    [Rule("rr:attack-player-ability-type.2.2")]
    [Fact]
    public void NextPaidEventAfterEmbiggenDisposalReceivesOnlyPrintedDamage()
    {
        // "each instance of damage ... is increased"; event effects resolve,
        // "then place the event card in its owner's discard pile."
        Card? source = null;
        Card? next = null;
        Card? minion = null;
        Card? upgrade = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("05030", board.Seats[0].Hand);
            next = board.CreateCard("05030", board.Seats[0].Hand);
            minion = board.CreateCard("01130", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            upgrade = board.CreateCard("05010", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            AddPayment(board);
            AddPayment(board);
        }, hero: true, abilities: Runner(StagedAttackSafetyTests.Body, authored: true));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Pay(game, world, source!);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        game.Resolve(Decision.Take(villain.ObjectId));
        game.Resolve(Decision.Take(minion!.ObjectId));
        Assert.Equal(5, villain.Damage);
        Assert.Equal(5, minion.Damage);
        Assert.False(upgrade!.Ready);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind == "eventDamage");
        Pay(game, world, next!);
        Assert.Equal(Question.Element, game.Pending!.Asking);
        game.Resolve(Decision.Take(villain.ObjectId));
        Assert.Equal(8, villain.Damage);
        game.Resolve(Decision.Take(minion.ObjectId));
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(DeckType.DiscardPile, next!.Area.Type);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:attack-player-ability-type.2.1")]
    [Rule("rr:attack-player-ability-type.2.2")]
    [Rule("rr:attack-player-ability-type.3.1")]
    [Theory]
    [InlineData(1, 6)]
    [InlineData(2, 8)]
    public void AttackModifierConsumesOneUseForTheWholeStagedAttack(int uses, int finalVillainDamage)
    {
        // "a single attack"; each instance is increased, but "only ... one
        // of that ability's attacks" is increased for each available use.
        string body = """
            {"seq":[BODY,{"attack":{"target":{"query":"villain"},"effect":{
              "dealAttackDamage":{"cards":{"query":"villain"},"amount":1}}
            }}]}
            """.Replace("BODY", StagedAttackSafetyTests.Body, StringComparison.Ordinal);
        Card? source = null;
        Card? minion = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("05030", board.Seats[0].Hand);
            minion = board.CreateCard("01130", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            AddPayment(board);
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "attackDamage", 2,
                Card: source.ObjectId, Affects: source.ObjectId, Lasts: new Duration(Uses: uses)));
        }, hero: true, abilities: Runner(body));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Pay(game, world, source!);
        game.Resolve(Decision.Take(villain.ObjectId));
        Assert.Equal(5, villain.Damage);
        Assert.Equal("Choose another enemy", game.Pending!.DisplayQuestion);
        game.Resolve(Decision.Take(minion!.ObjectId));
        Assert.Equal(5, minion.Damage);
        Assert.Equal(finalVillainDamage, villain.Damage);
        Assert.DoesNotContain(world.Effects.Active(), effect => effect.Kind == "attackDamage");
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    private static AbilityRunner Runner(string body, bool authored = false)
    {
        var root = JsonNode.Parse(authored ? AuthoredCards.Text : "{\"cards\":[]}")!;
        root["cards"]!.AsArray().Add(JsonNode.Parse("""
            {"card":"05030","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "effect":BODY}]}
            """.Replace("BODY", body, StringComparison.Ordinal)));
        return new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
    }

    private static void AddPayment(World world)
    {
        world.CreateCard("01088", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Hand);
    }

    private static void Pay(Game game, World world, Card source)
    {
        int[] payment = [.. world.Seats[0].Hand.Cards.Where(card => card.FaceId is "01088" or "01089")
            .Take(2).Select(card => card.ObjectId)];
        game.Resolve(PaidDecision.Take(Assert.Single(game.Pending!.Affordances,
            option => option.AnchorId == source.ObjectId), payment));
    }
}
