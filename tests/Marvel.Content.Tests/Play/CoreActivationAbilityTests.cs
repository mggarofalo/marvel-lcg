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

    [Rule("rr:activation.7")]
    [Rule("rr:attack-enemy-activation.step.6")]
    [Fact]
    public void KlawsVengeanceWaitsToSeeWhetherTheAttackDealtDamage()
    {
        var world = Board("01001a", "01113");
        var scheme = world.CreateCard("01116a", world.AreaOf(DeckType.MainSchemesArea));
        var card = world.CreateCard("01122", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();

        runner.WhenRevealed(world, card, 0);
        int activation = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.Attack).ActivationId;

        runner.ActivationCompleted(
            world, new EnemyActivation(world.TheCardIn(DeckType.VillainArea)!.ObjectId,
                0, Attacking: true, activation, Made: true, DamageDealt: 2));

        var placement = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.PlaceThreatEffect);
        Assert.Equal(scheme.ObjectId, placement.Placement!.Scheme);
        Assert.Equal(1, placement.Placement.Assigned);
    }

    [Rule("rr:activation.7")]
    [Fact]
    public void RageOfUltronUsesOnlyTheCompletedSchemesThreat()
    {
        var world = Board("01001b", "01134");
        var first = world.CreateCard("01002", world.Seats[0].Deck);
        var second = world.CreateCard("01003", world.Seats[0].Deck);
        var third = world.CreateCard("01004", world.Seats[0].Deck);
        var card = world.CreateCard("01145", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();

        runner.WhenRevealed(world, card, 0);
        int activation = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.Scheme).ActivationId;

        runner.ActivationCompleted(
            world, new EnemyActivation(world.TheCardIn(DeckType.VillainArea)!.ObjectId,
                0, Attacking: false, activation, Made: true, ThreatPlaced: 2));

        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        Assert.Single(world.Seats[0].Deck.Cards);
        Assert.Equal(2, discard.Cards.Count);
        Assert.Contains(first, world.Seats[0].Deck.Cards.Concat(discard.Cards));
        Assert.Contains(second, world.Seats[0].Deck.Cards.Concat(discard.Cards));
        Assert.Contains(third, world.Seats[0].Deck.Cards.Concat(discard.Cards));
    }

    private static World Board(string identity, string villain)
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard(identity, seat.Hero);
        world.CreateCard(villain, world.AreaOf(DeckType.VillainArea));
        return world;
    }
}
