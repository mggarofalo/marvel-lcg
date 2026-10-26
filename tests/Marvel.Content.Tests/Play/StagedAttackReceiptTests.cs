using System.Text.Json.Nodes;
using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic receipt observers and actual full-book Melee cover attack settlement.
public sealed class StagedAttackReceiptTests
{
    private const string Melee = """
        {"card":"05030","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "effect":{"chooseCard":{"from":{"query":"attackableEnemies"},"effect":{
            "attack":{"target":"chosen","effect":{"seq":[
              {"dealAttackDamage":{"cards":"chosen","amount":3}},
              {"attackAnotherEnemy":{"amount":3}}
            ]}}}}}
        }]}
        """;

    [Rule("rr:attack-player-ability-type.2.1")]
    [Rule("rr:attack-player-ability-type.5")]
    [Rule("rr:triggering-condition.2")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EndWindowRetainsBothRecipientsAndFirstDefeat(bool defeated)
    {
        // "a single attack"; "attacked each of those enemies". Ruling
        // f32c53356db718a6 delays attack responses until all of Melee resolves.
        var runner = new AbilityRunner(AbilityCatalog.Parse("{\"cards\":[" + Melee + "," + """
            {"card":"01007","abilities":[{
              "trigger":{"event":"WhenAttackEnds","timing":"ForcedResponse","target":"enemy"},
              "effect":{"placeCounters":{"card":"you","counter":"recipient","count":1}}
            }]},
            {"card":"01092","abilities":[{
              "trigger":{"event":"WhenAttackEnds","timing":"ForcedResponse","subject":"game"},
              "effect":{"placeCounters":{"card":"you","counter":"whole","count":1}}
            }]},
            {"card":"01093","abilities":[{
              "trigger":{"event":"WhenAttackEnds","alsoHappened":"WhenCardDefeated","timing":"ForcedResponse","target":"enemy"},
              "effect":{"placeCounters":{"card":"you","counter":"defeated","count":1}}
            }]}
            """ + "]}"));
        Card? first = null;
        Card? second = null;
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = PayableMelee(board);
            first = board.CreateCard(defeated ? "01101" : "01130",
                board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            second = board.CreateCard("01130", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            InPlay(board, "01007");
            InPlay(board, "01092");
            InPlay(board, "01093");
        }, hero: true, abilities: runner);
        Pay(game, world, source!);
        game.Resolve(Decision.Take(first!.ObjectId));
        var hero = world.Seats[0].IdentityCard;
        Assert.Equal(0, hero.Tokens.GetValueOrDefault("c_whole"));
        Assert.Equal(0, hero.Tokens.GetValueOrDefault("c_recipient"));
        Assert.Equal("Choose another enemy", game.Pending!.DisplayQuestion);
        game.Resolve(Decision.Take(second!.ObjectId));
        Assert.Equal(Question.Order, game.Pending!.Asking);
        var occurrence = world.Agenda.Occurrence!;
        Assert.Equal(new[] { first.ObjectId, second.ObjectId }, occurrence.Attacks.Select(item => item.Card));
        Assert.All(occurrence.Attacks, item => Assert.Equal(3, item.DamageTaken));
        Assert.Equal(defeated ? 1 : 0, occurrence.Defeats.Count);
        if (defeated) Assert.Equal(first.ObjectId, Assert.Single(occurrence.Defeats).Card);
        while (game.Pending!.Asking == Question.Order)
            game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));
        Assert.Equal(2, hero.Tokens["c_recipient"]);
        Assert.Equal(1, hero.Tokens["c_whole"]);
        Assert.Equal(defeated ? 1 : 0, hero.Tokens.GetValueOrDefault("c_defeated"));
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:villain-defeat.3.1")]
    [Fact]
    public void NewStageOfFirstVillainIsExcludedAfterSettlement()
    {
        // "The new stage of the villain is considered the same character".
        Card? source = null;
        Card? minion = null;
        Card? next = null;
        var runner = AuthoredCards.Runner();
        var (game, world) = Playing(board =>
        {
            source = PayableMelee(board);
            minion = board.CreateCard("01130", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            next = board.CreateCard("01095", board.AreaOf(DeckType.VillainDeck));
            var villain = board.TheCardIn(DeckType.VillainArea)!;
            villain.TakeDamage(board.Facts.PrintedValue(villain.FaceId, "HP", board.Players) - 3);
        }, hero: true, abilities: runner);
        var first = world.TheCardIn(DeckType.VillainArea)!;
        Pay(game, world, source!);
        game.Resolve(Decision.Take(first.ObjectId));
        Assert.Same(next, world.TheCardIn(DeckType.VillainArea));
        Assert.DoesNotContain(game.Pending!.Affordances, item => item.AnchorId == next!.ObjectId);
        Assert.Equal(minion!.ObjectId, Assert.Single(game.Pending.Affordances).AnchorId);
        game.Resolve(Decision.Take(minion.ObjectId));
        Assert.Equal(0, next!.Damage);
        Assert.Equal(3, minion.Damage);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
    }

    [Rule("rr:event.5")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActualEmbiggenSurvivesSecondChoiceAndExpiresAtDisposal(bool accept)
    {
        // "each of those instances is modified"; ruling a43333f9f9d3634a:
        // Embiggen increases both Melee instances to 5.
        var runner = AuthoredCards.Runner();
        Card? source = null;
        Card? minion = null;
        Card? upgrade = null;
        var (game, world) = Playing(board =>
        {
            source = PayableMelee(board);
            minion = board.CreateCard("01130", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            upgrade = board.CreateCard("05010", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Pay(game, world, source!);
        Assert.Equal(upgrade!.ObjectId, Assert.Single(game.Pending!.Affordances).AnchorId);
        game.Resolve(accept ? Decision.Take(game.Pending.Affordances[0].Id) : Decision.Decline);
        game.Resolve(Decision.Take(villain.ObjectId));
        Assert.Equal(accept ? 5 : 3, villain.Damage);
        Assert.Equal(!accept, upgrade.Ready);
        Assert.Equal("Choose another enemy", game.Pending!.DisplayQuestion);
        if (accept) Assert.Contains(world.Effects.Registered, effect => effect.Kind == "eventDamage");
        game.Resolve(Decision.Take(minion!.ObjectId));
        Assert.Equal(accept ? 5 : 3, minion.Damage);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind == "eventDamage");
        Assert.False(world.Agenda.IsBusy);
    }

    private static Card PayableMelee(World world)
    {
        var source = world.CreateCard("05030", world.Seats[0].Hand);
        world.CreateCard("01088", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Hand);
        return source;
    }

    private static void Pay(Game game, World world, Card source)
    {
        int[] payment = [.. world.Seats[0].Hand.Cards.Where(card => card.FaceId is "01088" or "01089")
            .Select(card => card.ObjectId)];
        game.Resolve(PaidDecision.Take(Assert.Single(game.Pending!.Affordances,
            item => item.AnchorId == source.ObjectId), payment));
    }
}
