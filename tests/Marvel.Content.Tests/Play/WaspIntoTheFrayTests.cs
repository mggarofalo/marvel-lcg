using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production row on synthetic boards; no public pack admission.
public sealed class WaspIntoTheFrayTests : CoreCardTestBase
{
    private const string Mercenary = "01101";
    private const string Whiplash = "01172";
    private const string Modok = "01184";
    private const string HydraSoldier = "01182";

    [Theory]
    [InlineData(Mercenary, false, 3, 2)]
    [InlineData(Whiplash, true, 2, 3)]
    [InlineData(Modok, false, 0, 5)]
    [Rule("rr:excess-damage")]
    public void IntoTheFrayRemovesThreatForExcessDamageDealt(string face, bool tough, long excess, int threat)
    {
        // rr:excess-damage: "any amount of damage that is dealt to a character beyond
        // that character's remaining hit points." Ruling dd353e3498f6b923: Into the Fray
        // "still has its additional effect if the minion is dealt excess damage,
        // regardless of damage taken."
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 5);
        var minion = WaspBoards.Engaged(world, face);
        if (tough) Statuses.Give(world, minion, Statuses.Tough);
        var seat = world.Seats[0];
        var fray = world.CreateCard("13013", seat.Hand);
        int[] payment = [world.CreateCard("01088", seat.Hand).ObjectId, world.CreateCard("01088", seat.Hand).ObjectId];
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == fray.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, payment, []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(minion.ObjectId), []);
        // Small but Mighty (13001a) may answer the defeat; declining keeps the count exact.
        while (Sequence.Work(world, world.Facts, runner, []) is { } response)
            Sequence.Answer(world, world.Facts, runner, response, Decision.Decline, []);
        Assert.Equal(threat, main.Tokens["k_threat"]);
        Assert.Equal(5 - excess, main.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, fray.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        if (tough)
        {
            Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
            Assert.Equal(0, minion.Damage);
            Assert.Equal(0, Statuses.Count(world, minion, Statuses.Tough));
        }
    }

    [Fact]
    [Rule("rr:excess-damage")]
    [Rule("rr:when-defeated-abilities.2")]
    public void IntoTheFrayRemovesExcessThreatAfterTheMinionsWhenDefeatedEncounterDeal()
    {
        // rr:excess-damage: "any amount of damage that is dealt to a character beyond
        // that character's remaining hit points." Printed Hydra Soldier (01182, 4 hit
        // points): "When Defeated: Deal the engaged player an encounter card."
        // rr:when-defeated-abilities.2: "When a ... minion is defeated, all 'When Defeated'
        // abilities on the card resolve." The facedown card changes no threat, so the
        // 2 excess points of the 6 damage remove 2 threat.
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001c");
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 5);
        var soldier = WaspBoards.Engaged(world, HydraSoldier);
        var seat = world.Seats[0];
        var fray = world.CreateCard("13013", seat.Hand);
        int[] payment = [world.CreateCard("01088", seat.Hand).ObjectId, world.CreateCard("01088", seat.Hand).ObjectId];
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == fray.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, payment, []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(soldier.ObjectId), []);
        while (Sequence.Work(world, world.Facts, runner, []) is { } response)
            Sequence.Answer(world, world.Facts, runner, response, Decision.Decline, []);
        Assert.Equal(DeckType.EncounterDiscardPile, soldier.Area.Type);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Equal(3, main.Tokens["k_threat"]);
        Assert.False(world.Agenda.IsBusy);
    }
}
