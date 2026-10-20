using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WeaponMasterTests : CoreCardTestBase
{
    [Theory]
    [InlineData(true, "04149", true, true)]
    [InlineData(false, "04149", true, true)]
    [InlineData(true, "04149", false, false)]
    [InlineData(false, "04149", false, false)]
    [InlineData(true, "04002", true, false)]
    [InlineData(true, "01098", true, false)]
    public void SurgeRequiresAWeaponAttachmentOnTheActivatedVillain(bool hero, string face, bool onVillain, bool surge)
    {
        var world = Hero("01001a,01001b");
        if (!hero) Forms.Change(world.Seats[0], Cards);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var minion = world.CreateCard("01100", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard(face, world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains,
            (onVillain ? villain : minion).ObjectId, cardOwner: face == "04002" ? 0 : -1));
        Statuses.Give(world, villain, Statuses.Stunned);
        Statuses.Give(world, villain, Statuses.Confused);
        var next = world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        var card = world.CreateCard("04150", world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, card, 0);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(!hero, Statuses.Has(world, villain, Statuses.Stunned));
        Assert.Equal(hero, Statuses.Has(world, villain, Statuses.Confused));
        Assert.Equal(surge, next.Area.Type != DeckType.EncounterDeck);
    }
}

