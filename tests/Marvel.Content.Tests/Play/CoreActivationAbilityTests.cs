using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreActivationAbilityTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:boost-boost-icon.4")]
    [Rule("rr:boost-boost-icon.6")]
    [Fact]
    public void KlawGetsAnAdditionalFacedownBoostCardWhenHeAttacks()
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        var klaw = world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));
        var boost = world.CreateCard("01118", world.AreaOf(DeckType.EncounterDeck));
        var runner = AuthoredCards.Runner();
        var occurrence = Occurrence.ForAttack(
            1, [Steps.AttackInitiated], world, Cards,
            klaw.ObjectId, seat.IdentityCard.ObjectId, 0);

        var pending = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            ability => ability.Card == klaw.ObjectId);
        runner.Resolve(world, occurrence, pending, [], []);

        var waiting = world.AreaOf(
            DeckType.BoostCardsDeck, klaw.Area.PlayArea, host: klaw.ObjectId);
        Assert.Equal(boost.ObjectId, Assert.Single(waiting.Cards).ObjectId);
        Assert.False(boost.FaceUp);
    }
}
