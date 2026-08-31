using Marvel.Content.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class DeckConstructionRulesTests
{
    [Rule("rr:identity-specific-card.1")]
    [Rule("rr:identity-specific-card.2")]
    [Fact]
    public void AnIdentitySpecificCardMustShareItsIdentitySetIcon()
    {
        // The deck “must include each identity-specific card”, and those cards
        // “may only be used alongside an identity if [they] share a set icon”.
        var facts = Cards()
            .Card("identity", CardKind.AlterEgo, "Hero", "hero")
            .Card("wrong", CardKind.Event, "Wrong", "other", ("Class", "Hero"));

        var refused = Assert.Throws<ArgumentException>(() => DeckConstruction.Validate(
        [
            new Creation("identity", CreationSource.Identity, 0),
            new Creation("wrong", CreationSource.HeroDeck, 0),
        ], facts));

        Assert.Contains("does not share the identity set icon", refused.Message);
    }

    [Rule("rr:aspect-card.2")]
    [Rule("rr:basic-card.2")]
    [Rule("rr:campaign-specific-card.1")]
    [Rule("rr:campaign-specific-card.3")]
    [Fact]
    public void CampaignCardsAreNotPlayerDeckCustomizationCards()
    {
        // Aspect and Basic are the printed player-deck classifications.
        // Campaign cards can only be used by their campaign instead.
        var facts = Cards()
            .Card("identity", CardKind.AlterEgo, "Hero", "hero")
            .Card("campaign", CardKind.Upgrade, "Reward", "campaign", ("Class", "Campaign"));

        var refused = Assert.Throws<ArgumentException>(() => DeckConstruction.Validate(
        [
            new Creation("identity", CreationSource.Identity, 0),
            new Creation("campaign", CreationSource.PlayerDeck, 0),
        ], facts));

        Assert.Contains("non-player classification 'Campaign'", refused.Message);
    }

    [Rule("rr:team-up.1")]
    [Fact]
    public void ATeamUpCardMustNameTheChosenIdentity()
    {
        // “You cannot include this card in your deck unless your alter-ego or
        // hero title matches name 1 or name 2.”
        var facts = Cards()
            .Card("identity", CardKind.AlterEgo, "Peter Parker", "spider_man")
            .Card("team", CardKind.Event, "Swarm Tactics", "", ("Class", "Basic"),
                ("TeamUp", "Ant-Man;Wasp"));

        Assert.Throws<ArgumentException>(() => DeckConstruction.Validate(
        [
            new Creation("identity", CreationSource.Identity, 0),
            new Creation("team", CreationSource.PlayerDeck, 0),
        ], facts));
    }

    [Rule("rr:permanent.3")]
    [Fact]
    public void PermanentCardsDoNotCountTowardDeckSize()
    {
        // “Permanent cards do not count towards a player’s minimum or maximum
        // deck size.” One ordinary card and one Permanent therefore count one.
        var facts = Cards()
            .Card("ordinary", CardKind.Event, "Ordinary", "", ("Class", "Basic"))
            .Card("permanent", CardKind.Upgrade, "Permanent", "", ("Class", "Basic"),
                ("Permanent", "1"));

        Assert.Equal(1, DeckConstruction.DeckSize(
        [
            new Creation("ordinary", CreationSource.PlayerDeck, 0),
            new Creation("permanent", CreationSource.PlayerDeck, 0),
        ], facts));
    }

    [Rule("rr:unique-icon.2")]
    [Fact]
    public void ADeckCannotContainMatchingUniqueCards()
    {
        // “During deckbuilding, a player cannot include multiple matching
        // cards in their deck. The identity is included in this evaluation.”
        var facts = Cards()
            .Card("hero", CardKind.Hero, "Spider-Man", "spider", ("Unique", "1"))
            .Card("alter", CardKind.AlterEgo, "Peter Parker", "spider", ("Unique", "1"))
            .Card("ally", CardKind.Ally, "Peter Parker", "", ("Class", "Basic"),
                ("Unique", "1"));

        Assert.Throws<ArgumentException>(() => DeckConstruction.Validate(
        [
            new Creation("hero,alter", CreationSource.Identity, 0),
            new Creation("ally", CreationSource.PlayerDeck, 0),
        ], facts));
    }

    [Rule("rr:unique-icon.3")]
    [Fact]
    public void PlayersCannotSelectMatchingIdentities()
    {
        var facts = Cards()
            .Card("left", CardKind.AlterEgo, "Peter Parker", "a", ("Unique", "1"))
            .Card("right", CardKind.AlterEgo, "Peter Parker", "b", ("Unique", "1"));

        Assert.Throws<ArgumentException>(() => DeckConstruction.Validate(
        [
            new Creation("left", CreationSource.Identity, 0),
            new Creation("right", CreationSource.Identity, 1),
        ], facts));
    }

    [Rule("rr:unique-icon.3.1")]
    [Fact]
    public void AnIdentityMayMatchTheVillain()
    {
        // The identity and villain exception permits both cards even though
        // ordinary unique-card matching says their bare titles match.
        var facts = Cards()
            .Card("identity", CardKind.AlterEgo, "Nebula", "nebula", ("Unique", "1"))
            .Card("villain", CardKind.EncounterVillain, "Nebula", "", ("Unique", "1"));
        Creation[] dealt =
        [
            new Creation("identity", CreationSource.Identity, 0),
            new Creation("villain", CreationSource.Villain, -1),
        ];

        Assert.True(Uniqueness.Matches(facts, ["identity"], ["villain"]));
        DeckConstruction.Validate(dealt, facts);
    }

    private static Facts Cards() => new();

    private sealed class Facts : ICardFacts
    {
        private readonly Dictionary<string, (CardKind Kind, string Title, string Set)> cards = [];
        private readonly Dictionary<string, Dictionary<string, string>> attributes = [];

        public Facts Card(
            string id, CardKind kind, string title, string set,
            params (string Key, string Value)[] printed)
        {
            cards[id] = (kind, title, set);
            attributes[id] = printed.ToDictionary(pair => pair.Key, pair => pair.Value,
                StringComparer.Ordinal);
            return this;
        }

        public CardKind Kind(string faceId) => cards[faceId].Kind;
        public string Title(string faceId) => cards[faceId].Title;
        public string EncounterSet(string faceId) => cards[faceId].Set;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => attributes[faceId];
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? value)
            && long.TryParse(value, out long number) ? number : fallback;
    }
}
