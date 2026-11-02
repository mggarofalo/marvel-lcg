using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.HammerThrowEncounterDealStageTests;

namespace Marvel.Content.Tests.Play;

/// <summary>Hammer Throw (06005) defeats Goblin Soldier (02023) before returning Mjolnir (06009).</summary>
public sealed class HammerThrowGoblinSoldierDefeatTests
{
    // Hammer Throw: "deal 8 damage to an enemy and return Mjolnir to your hand. This attack
    // gains overkill." Goblin Soldier: "When Defeated: Deal 1 damage to the engaged player."
    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, 0, false)]
    [InlineData(0, 1, true)]
    [Rule("rr:overkill.1")]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Rule("rr:player-elimination.5.1")]
    [Rule("ruling:cef917d276a31694")]
    public void TheSoldiersDefeatResolvesBeforeMjolnirReturnsOnTheLiveBoard(int engaged, int thorHealthLeft, bool eliminated)
    {
        // rr:overkill.1: "If this attack defeats a minion, deal the damage beyond its hit
        // points to the villain." rr:when-defeated-abilities.2.1: "A defeated card leaves
        // play after its 'When Defeated' ability is resolved". ruling:cef917d276a31694 orders
        // Goblin Soldier's damage before the next instruction: "first the enemy is dealt 4
        // damage, then the enemy's 'When Defeated' resolves and deals 1 damage". Mjolnir is
        // then found on the board that damage left. rr:player-elimination.5.1: "If a player
        // is eliminated in the middle of an attack, that attack ends", and Thor's removed
        // Mjolnir is not returned.
        var runner = AuthoredCards.Runner();
        var game = Prepare(runner, "01094", "01095", players: 2);
        var thor = game.World.Seats[0].IdentityCard;
        if (thorHealthLeft > 0) thor.TakeDamage(DamagePlacement.Health(game.World, game.World.Facts, thor) - thorHealthLeft);
        long thorDamage = thor.Damage;
        var soldier = game.World.CreateCard("02023", game.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(engaged)));
        long villainDamage = game.Villain.Damage;

        var action = Assert.Single(runner.Actions(game.World, 0), a => a.Card == game.Event.ObjectId);
        runner.ExecuteAction(game.World, new PlayerAction(action, game.Resources, []));
        var target = Sequence.Work(game.World, game.World.Facts, runner, [])!;
        Sequence.Answer(game.World, game.World.Facts, runner, target, Decision.Take(soldier.ObjectId), []);
        Sequence.Finish(game.World, game.World.Facts, runner, []);

        Assert.Equal(DeckType.EncounterDiscardPile, soldier.Area.Type);
        Assert.Equal(villainDamage + 3, game.Villain.Damage);
        Assert.Equal(eliminated, game.World.Seats[0].Eliminated);
        Assert.False(game.World.Seats[1].Eliminated);
        if (eliminated) Assert.Equal(DeckType.RemovedArea, game.Hammer.Area.Type);
        else Assert.Same(game.World.Seats[0].Hand, game.Hammer.Area);
        if (!eliminated)
        {
            Assert.Equal(thorDamage + (engaged == 0 ? 1 : 0), thor.Damage);
            Assert.Equal(engaged == 1 ? 1 : 0, game.World.Seats[1].IdentityCard.Damage);
        }
        Assert.Empty(game.World.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:when-defeated-abilities.2.1")]
    public void AReadAfterTheReturnStillRaisesBeforeTheSoldierIsDamaged()
    {
        // Synthetic continuation: a damage instruction after the return reads the board the
        // soldier's unprojected When Defeated leaves, so the power refuses before it begins.
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        book["cards"]!.AsArray().Single(row => row!["card"]!.GetValue<string>() == "06005")!
            ["abilities"]![0]!["effect"]!["chooseCard"]!["effect"]!["attack"]!["effect"]!["seq"]!.AsArray()
            .Add(JsonNode.Parse("""{"dealDamage":{"cards":"you","amount":1}}"""));
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var game = Prepare(runner, "01094", "01095");
        var soldier = game.World.CreateCard("02023", game.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var action = Assert.Single(runner.Actions(game.World, 0), a => a.Card == game.Event.ObjectId);
        runner.ExecuteAction(game.World, new PlayerAction(action, game.Resources, []));
        var target = Sequence.Work(game.World, game.World.Facts, runner, [])!;
        var before = Snapshot(game.World);

        var error = Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(
            game.World, game.World.Facts, runner, target, Decision.Take(soldier.ObjectId), []));

        Assert.Contains("unprojected defeat window", error.Message);
        Assert.Equal(before, Snapshot(game.World));
        Assert.Equal(0, soldier.Damage);
    }
}
