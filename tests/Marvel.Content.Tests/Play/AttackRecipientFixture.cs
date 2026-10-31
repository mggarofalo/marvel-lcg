using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Boards for attacks with several or later recipients: Shield Toss and Giant Stomp.</summary>
internal static class AttackRecipientFixture
{
    /// <summary>
    /// Captain America plays Shield Toss (03006) discarding two cards: two recipients,
    /// 4 damage each. The villain carries <paramref name="shields"/> copies of
    /// <paramref name="attachment"/>; <paramref name="villainFace"/> replaces it when named.
    /// </summary>
    internal static (Game Game, World World, Card Villain, Card Minion, Card[] Attached) ShieldToss(
        string attachment, int shields, string? villainFace = null)
    {
        Card? minion = null, villain = null;
        Card[] held = [], attached = [];
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            var seat = board.Seats[0];
            board.CreateCard("03006", seat.Hand);
            board.CreateCard("03009", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), seat.IdentityCard.ObjectId, 0));
            held = [.. Enumerable.Range(0, 2).Select(_ => board.CreateCard("03003", seat.Hand))];
            minion = board.CreateCard("01102", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            villain = board.TheCardIn(DeckType.VillainArea)!;
            if (villainFace is not null)
            {
                World.MoveToTop(villain, board.AreaOf(DeckType.RemovedArea));
                villain = board.CreateCard(villainFace, board.AreaOf(DeckType.VillainArea));
            }
            attached = [.. Enumerable.Range(0, shields).Select(_ => board.CreateCard(attachment,
                board.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, villain.ObjectId)))];
        }, twoPlayers: true);
        var play = Assert.Single(game.Pending!.Affordances,
            offer => offer.PlaysCard && world.Cards[offer.AnchorId].FaceId == "03006");
        game.Resolve(new Decision(play.Id, [.. held.Select(card => card.ObjectId)]));
        return (game, world, villain!, minion!, attached);
    }

    /// <summary>Giant Ant-Man (12001c) with Giant Stomp (12003) and three Energy to pay for it.</summary>
    internal static (World World, RedSkullFixture Fixture, Card Stomp, int[] Payment) AntMan()
    {
        var (world, f) = KangTemporalFixture.Board(players: 1);
        var seat = world.Seats[0];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea));
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        seat.IdentityCard.TurnTo("12001c");
        var stomp = world.CreateCard("12003", seat.Hand);
        int[] payment = [.. Enumerable.Range(0, 3).Select(_ => world.CreateCard("01087", seat.Hand).ObjectId)];
        return (world, f, stomp, payment);
    }

    internal static void Execute(RedSkullFixture f, Card stomp, int[] payment)
    {
        var action = Assert.Single(f.Runner.Actions(f.World, 0), item => item.Card == stomp.ObjectId);
        f.Runner.ExecuteAction(f.World, new PlayerAction(action, payment, []));
    }
}
