using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class HulkPublishedRecipeTests
{
    [Fact]
    public void CommittedDividerRecipeHasFortyCardsAndSeparateEncounterComponents()
    {
        // mc09/UPSTREAM.md pins the FFG divider: 15 signature + 17 Aggression + 8 Basic.
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var hero = setup.Hero("hulk");
        Assert.Equal(["10001a,10001b"], hero.Hero);
        Assert.Equal(["10025"], hero.Obligations);
        Assert.Equal(["10026", "10027", "10028", "10028", "10028"], hero.NemesisSet);
        Assert.Equal("hulk_nemesis", hero.NemesisSetIcon);
        Assert.Equal(Signature, hero.HeroDeck);
        Assert.Equal(Customization, hero.PlayerDeck);
        Assert.Null(hero.InvocationDeck);
        var admitted = RuntimeProductAdmission.Discover(setup, cards, AuthoredCards.Book.Authored);
        Assert.Equal(33, admitted.Playable.Count(face => cards.Pack(face) == "hlk"));
        Assert.Equal(18, admitted.Setup.HeroNames.Count);
        Assert.Equal(39, admitted.Setup.CampaignNames.Count);
        Assert.Equal(15, admitted.Setup.EncounterSetNames.Count);
        Assert.Equal(40, PlayerDeckAdmission.Admit(new([.. hero.HeroDeck, .. hero.PlayerDeck]),
            hero.HeroDeck, 0, cards, hero.Hero[0]).Count);
        Assert.DoesNotContain(hero.PlayerDeck, face => face is "10029" or "10030" or "10031" or "10032");
    }

    private static readonly string[] Signature =
    [
        "10002", "10002", "10003", "10003", "10004", "10004", "10005", "10005",
        "10006", "10006", "10007", "10007", "10008", "10009", "10010",
    ];
    private static readonly string[] Customization =
    [
        "10011", "10012", "10013", "10014", "10014", "10014", "10015", "10015", "10015",
        "10016", "10016", "10016", "10017", "10017", "10018", "10018", "10018",
        "10019", "10019", "10019", "10020", "10021", "10022", "10023", "10024",
    ];
}
