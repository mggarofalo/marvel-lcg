using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production row on synthetic boards; no public pack admission.
public sealed class WaspSmallButMightyTests : CoreCardTestBase
{
    private const string Mercenary = "01101";

    [Theory]
    [InlineData("13001a", true)]
    [InlineData("13001a", false)]
    [InlineData("13001c", false)]
    [Rule("rr:response.1")]
    public void TinyWaspAnswersAnEventsMinionDefeatWithOneDamageToTheVillain(string face, bool take)
    {
        // rr:response.1: "Players can only trigger response abilities on cards they control or on encounter cards."
        // Printed 13001a: "Response: After Wasp (or an event you play) defeats a minion or side
        // scheme, deal 1 damage to the villain." Giant Wasp (13001c) has no such response.
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board(face);
        var wasp = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var minion = WaspBoards.Engaged(world, Mercenary);
        var seat = world.Seats[0];
        var fray = world.CreateCard("13013", seat.Hand);
        int[] payment = [world.CreateCard("01088", seat.Hand).ObjectId, world.CreateCard("01088", seat.Hand).ObjectId];
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == fray.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, payment, []));
        var target = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, target, Decision.Take(minion.ObjectId), []);
        int offers = 0;
        while (Sequence.Work(world, world.Facts, runner, []) is { } prompt)
        {
            var response = prompt.Affordances.SingleOrDefault(offer => offer.AnchorId == wasp.ObjectId);
            if (response is not null) offers++;
            Sequence.Answer(world, world.Facts, runner, prompt,
                response is not null && take ? Decision.Take(response.Id) : Decision.Decline, []);
        }
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(face == "13001a" ? 1 : 0, offers);
        Assert.Equal(take && face == "13001a" ? 1 : 0, villain.Damage);
    }
}
