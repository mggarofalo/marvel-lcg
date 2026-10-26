using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MutagenHysteriaTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    public void EachHysteriaAddsOneBoostToItsAttachedVillainsActivation(bool scheme, int copies)
    {
        // 02020: "When Green Goblin schemes or attacks, give him 1 additional boost card for that activation."
        var world = Hero("01001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var villain = world.CreateCard("02014", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("02018b", world.AreaOf(DeckType.MainSchemesArea));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        for (int i = 0; i < 10; i++) world.CreateCard("01186", deck);
        var f = new RedSkullFixture(world, runner);
        for (int i = 0; i < copies; i++)
        {
            var attachment = f.Reveal("02020", 1);
            f.Finish();
            Assert.Equal(villain.ObjectId, attachment.Area.Host);
        }
        AgendaOperation operation = scheme ? new EnemySchemeOperation() : new EnemyAttackOperation();
        world.Agenda.Add(new PhaseStep(operation, 1, 2, Subject: villain.ObjectId, Seat: 1));
        f.Finish(prompt => Decision.Take(prompt.Affordances[0].Id));
        Assert.Equal(10 - 1 - copies, deck.Cards.Count);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(copies, world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, villain.ObjectId).Cards.Count);
    }
}
