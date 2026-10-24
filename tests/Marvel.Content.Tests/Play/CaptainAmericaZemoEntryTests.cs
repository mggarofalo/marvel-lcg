using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaZemoEntryTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [Rule("rr:quickstrike")]
    [Rule("rr:quickstrike.1")]
    public void RevealedZemoAttacksOnlyTheEngagedHero(int player, bool hero)
    {
        // "After a minion with the quickstrike keyword engages a player whose
        // identity is in hero form, that minion attacks that player."
        // Prepared reveal scene using printed cards and the canonical ability book.
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, players: 2);
        var captain = world.CreateSeat("Steve Rogers");
        captain.IdentityCard = world.CreateCard("03001a,03001b", captain.Hero);
        var carol = world.CreateSeat("Carol Danvers");
        carol.IdentityCard = world.CreateCard("01010a,01010b", carol.Hero);
        var engaged = world.Seats[player];
        if (!hero) engaged.IdentityCard.TurnTo(player == 0 ? "03001b" : "01010b");
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        var zemo = world.CreateCard("03028", world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player)));
        var runner = AuthoredCards.Runner();
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            Round: 1, Number: 4, Subject: zemo.ObjectId, Seat: player));
        bool offeredDefense = false;
        for (int decisions = 0; ; decisions++)
        {
            var prompt = Sequence.Work(world, cards, runner, events);
            if (prompt is null) break;
            Assert.True(decisions < 10, prompt.Label);
            Assert.Equal(Question.Defender, prompt.Asking);
            offeredDefense = true;
            Sequence.Answer(world, cards, runner, prompt, Decision.Decline, events);
        }

        Assert.Equal(hero, offeredDefense);
        Assert.Equal(hero ? 3 : 0, engaged.IdentityCard.Damage);
        Assert.Equal(0, world.Seats[1 - player].IdentityCard.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, zemo.Area.Type);
        Assert.Equal(player, zemo.Area.PlayArea.Player);
        Assert.False(ThwartPermissions.CanThwart(world, engaged.IdentityCard));
        Assert.True(ThwartPermissions.CanThwart(world, world.Seats[1 - player].IdentityCard));
        Assert.Null(world.Attack);
    }
}
