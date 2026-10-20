using Marvel.Content.Setup;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

/// <summary>Synthetic campaign removals exercise construction without admitting expansion scenarios.</summary>
public sealed class CampaignDeckExclusionTests
{
    [Rule("rr:modes-of-play.10")]
    [Fact]
    public void RemovedSignatureAllyCanBeReplacedWithoutRelaxingMinimumDeckSize()
    {
        // "that card can no longer be used during the rest of the campaign."
        var (setup, cards, draft) = Fixture();
        string[] replacement = draft.Where(face => face != "01002").Append("01093").ToArray();
        var request = new DealRequest("rhino", ["spider_man"], PlayerDecks: [new(replacement)])
        { CampaignExcludedAllies = ["01002"] };
        var dealt = Dealer.Deal(setup, request, cards);
        Assert.DoesNotContain(dealt, card => card.Spec == "01002");
        Assert.Equal(40, dealt.Count(card => card.Source is CreationSource.HeroDeck or CreationSource.PlayerDeck));
        Assert.Throws<ArgumentException>(() => Dealer.Deal(setup,
            request with { PlayerDecks = [new(replacement.SkipLast(1).ToArray())] }, cards));
        Assert.Throws<ArgumentException>(() => Dealer.DealOrder(setup, "rhino", ["spider_man"],
            facts: cards, playerDecks: [new(replacement)]));
    }

    [Fact]
    public void ExclusionsCannotRemoveNonAlliesOrReintroduceBannedAllies()
    {
        var (setup, cards, draft) = Fixture();
        var request = new DealRequest("rhino", ["spider_man"], PlayerDecks: [new(draft)])
        { CampaignExcludedAllies = ["01002"] };
        Assert.Throws<ArgumentException>(() => Dealer.Deal(setup, request, cards));
        Assert.Throws<ArgumentException>(() => Dealer.Deal(setup,
            request with { CampaignExcludedAllies = ["01005"] }, cards));
        Assert.Throws<ArgumentException>(() => Dealer.Deal(setup,
            request with { PlayerDecks = null }, cards));
    }

    private static (SetupCatalog Setup, CardCatalog Cards, string[] Draft) Fixture()
    {
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var hero = setup.Hero("spider_man");
        return (setup, cards, [.. hero.HeroDeck, .. hero.PlayerDeck]);
    }
}
