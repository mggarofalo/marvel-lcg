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
    private const string AdvancedUltronDrone = "01143";
    private const string Beetle = "13028";

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

    [Theory]
    [InlineData(HydraSoldier)]
    [InlineData(AdvancedUltronDrone)]
    [InlineData(Beetle)]
    [Rule("rr:excess-damage")]
    [Rule("rr:when-defeated-abilities.1")]
    [Rule("rr:ability.4")]
    public void IntoTheFrayRemovesExcessThreatAfterTheMinionsDefeatAbilityResolves(string face)
    {
        // rr:excess-damage: "any amount of damage that is dealt to a character beyond
        // that character's remaining hit points." Each minion has 4 hit points, so 2 of
        // the 6 damage is excess. rr:when-defeated-abilities.1: "The 'When Defeated' timing
        // trigger is equivalent to the following trigger: 'Forced Interrupt: When this card
        // is defeated...'" Hydra Soldier (01182) deals an encounter card, Advanced Ultron
        // Drone (01143) engages a facedown Drone, and Beetle (13028) is shuffled into the
        // encounter deck when no physical resource is spent. rr:ability.4: "resolve the
        // ability one sentence at a time", so the removal follows on the settled board.
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001c");
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 5);
        var minion = WaspBoards.Engaged(world, face);
        var seat = world.Seats[0];
        var fray = world.CreateCard("13013", seat.Hand);
        int[] payment = [world.CreateCard("01088", seat.Hand).ObjectId, world.CreateCard("01088", seat.Hand).ObjectId];
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == fray.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, payment, []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(minion.ObjectId), []);
        while (Sequence.Work(world, world.Facts, runner, []) is { } response)
            Sequence.Answer(world, world.Facts, runner, response, Decline(response), []);
        Assert.Equal(face == Beetle ? DeckType.EncounterDeck : DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(face == HydraSoldier ? 1 : 0, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
        Assert.Equal(face == AdvancedUltronDrone ? 1 : 0,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards.Count);
        Assert.Equal(3, main.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, fray.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    // Optional windows are passed; Beetle's forced choice takes the shuffle instead of a payment.
    private static Decision Decline(Prompt prompt) => prompt.Cancellable
        ? Decision.Decline
        : Decision.Take(prompt.Affordances[^1].Id);
}
