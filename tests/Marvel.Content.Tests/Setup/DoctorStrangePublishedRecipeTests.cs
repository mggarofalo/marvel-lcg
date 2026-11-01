using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class DoctorStrangePublishedRecipeTests
{
    [Fact]
    public void CommittedDividerRecipeHasFortyOrdinaryCardsAndFiveSeparateInvocations()
    {
        // datasets/rules-packs/mc08/UPSTREAM.md pins the FFG divider:
        // 15 signature, 17 Protection and 8 Basic, with five separate Invocations.
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var hero = setup.Hero("doctor_strange");
        Assert.Equal(DoctorStrangeSetupFixture.Signature, hero.HeroDeck);
        Assert.Equal(DoctorStrangeSetupFixture.Extras, hero.PlayerDeck);
        Assert.Equal(DoctorStrangeSetupFixture.Invocations, hero.InvocationDeck!.Cards);
        Assert.Equal(["09001a,09001b"], hero.Hero);
        Assert.Equal(["09027"], hero.Obligations);
        Assert.Equal(["09028", "09029", "09030", "09030", "09031"], hero.NemesisSet);
        Assert.Equal("doctor_strange_nemesis", hero.NemesisSetIcon);
        var cards = DoctorStrangeSetupFixture.PrintedCards;
        var admitted = RuntimeProductAdmission.Discover(setup, cards, AuthoredCards.Book.Authored);
        Assert.Equal(40, admitted.Playable.Count(face => cards.Pack(face) == "drs"));
        Assert.Equal(17, admitted.Setup.HeroNames.Count);
        Assert.Equal(39, admitted.Setup.CampaignNames.Count);
        Assert.Equal(15, admitted.Setup.EncounterSetNames.Count);
        Assert.Equal(40, PlayerDeckAdmission.Admit(new([.. hero.HeroDeck, .. hero.PlayerDeck]),
            hero.HeroDeck, 0, cards, hero.Hero[0]).Count);
    }
}
