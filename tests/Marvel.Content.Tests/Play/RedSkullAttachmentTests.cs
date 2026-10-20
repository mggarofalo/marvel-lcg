using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RedSkullAttachmentTests
{
    [Theory]
    [InlineData("04132", "piercing")]
    [InlineData("04132", "ranged")]
    [InlineData("04133", "retaliate")]
    public void WeaponsAttachToRedSkullAndGrantTheirPrintedKeywords(string face, string keyword)
    {
        var f = RedSkullFixture.Create();
        var attachment = f.Reveal(face);
        f.Finish();
        Assert.Equal(f.Villain.ObjectId, attachment.Area.Host);
        Assert.True(Keywords.Has(f.World, f.Villain, keyword, f.World.Facts));
        World.MoveToTop(attachment, f.World.AreaOf(DeckType.EncounterDiscardPile));
        Assert.False(Keywords.Has(f.World, f.Villain, keyword, f.World.Facts));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InfinitePowerSelectsOnlyTheRevealingPlayersForm(bool hero)
    {
        var f = RedSkullFixture.Create(hero: hero);
        Statuses.Give(f.World, f.Villain, Statuses.Stunned);
        Statuses.Give(f.World, f.Villain, Statuses.Confused);
        f.Reveal("04138");
        f.Finish();
        Assert.True(Statuses.Has(f.World, f.Villain, Statuses.Tough));
        Assert.Equal(!hero, Statuses.Has(f.World, f.Villain, Statuses.Stunned));
        Assert.Equal(hero, Statuses.Has(f.World, f.Villain, Statuses.Confused));
    }

    [Fact]
    public void HydraReinforcementsOffersOnlyNonEliteMinionsToTheDefeatingPlayer()
    {
        var f = RedSkullFixture.Create(players: 2);
        var minion = f.World.CreateCard("01101", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var elite = f.World.CreateCard("04130", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var scheme = f.Reveal("04143");
        f.Finish();
        Agendas.Happening(f.World);
        Defeat.Scheme(f.World, f.World.Facts, scheme, "test", [], by: 1);
        int choices = 0;
        f.Finish(prompt =>
        {
            choices++;
            Assert.Equal(1, prompt.Player);
            var option = Assert.Single(prompt.Affordances);
            Assert.Equal(minion.ObjectId, option.Id);
            return Decision.Take(option.Id);
        });
        Assert.Equal(1, choices);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, elite.Area.Type);
    }
    [Fact]
    public void MasterStrategistMaximumLeavesTheOtherCopyForTheNextActivation()
    {
        var f = RedSkullFixture.Create(hero: false);
        var first = f.Reveal("04134");
        f.Finish();
        var second = f.Reveal("04134");
        f.Finish();
        // Zero-icon, no-star cards isolate the number of dealt boosts.
        for (int copy = 0; copy < 6; copy++) f.World.CreateCard("01101", f.World.AreaOf(DeckType.EncounterDeck));
        int deckBefore = f.World.AreaOf(DeckType.EncounterDeck).Cards.Count;
        f.World.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2, Subject: f.Villain.ObjectId, Seat: 0));
        f.Finish(prompt => Decision.Take(prompt.Affordances[0].Id));
        Assert.Equal(1, new[] { first, second }.Count(card => card.Area.Type == DeckType.EncounterDiscardPile));
        Assert.Equal(1, new[] { first, second }.Count(card => card.Area.Host == f.Villain.ObjectId));
        Assert.Equal(deckBefore - 2, f.World.AreaOf(DeckType.EncounterDeck).Cards.Count);
        f.World.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 2, 2, Subject: f.Villain.ObjectId, Seat: 0));
        f.Finish();
        Assert.All(new[] { first, second }, card => Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type));
        Assert.Equal(deckBefore - 4, f.World.AreaOf(DeckType.EncounterDeck).Cards.Count);
    }

    [Fact]
    public void TwistedRealityIncitesAndDealsFirstPlayerAnEncounterCardOnHostDefeat()
    {
        var f = RedSkullFixture.Create(players: 2);
        long before = f.Main.Tokens.GetValueOrDefault("k_threat");
        var house = Assert.Single(f.World.AreaOf(DeckType.SideSchemesArea).Cards);
        var attachment = f.Reveal("04135", 1);
        f.Finish();
        Assert.Equal(house.ObjectId, attachment.Area.Host);
        Assert.Equal(before + 1, f.Main.Tokens["k_threat"]);
        Agendas.Happening(f.World);
        Defeat.Scheme(f.World, f.World.Facts, house, "test", [], by: 1);
        f.Finish();
        Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
    }

}
