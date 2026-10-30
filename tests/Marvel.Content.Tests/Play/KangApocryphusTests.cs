using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Apocryphus (11040) and the full authored book on a nonpublic Rhino board;
// Kang's public admission stays closed.
public sealed class KangApocryphusTests : CoreCardTestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:you-your.10")]
    [Rule("rr:permanent.4")]
    public void ApocryphusOffersTheRevealingPlayerEachAllyAndSupportTheyControl(bool discardSupport)
    {
        // 11040: "When Revealed: Discard an ally or support you control."
        // rr:you-your.10: "the player resolving that game step or card ability
        // performs that game function against the card in their play area."
        // Player 1 is not the first player; player 0's ally and player 1's upgrade
        // are not offered. rr:permanent.4: "Permanent cards are not valid targets for
        // card effects that would cause the permanent card to leave play", so Clear
        // Skies, a permanent support from another set, is not offered either.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var ally = world.CreateCard("01059", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var support = world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var upgrade = world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var other = world.CreateCard("01067", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var permanent = world.CreateCard("36002", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var apocryphus = f.Reveal("11040", player: 1);
        var discarded = discardSupport ? support : ally;
        var kept = discardSupport ? ally : support;
        var askers = new List<int>();

        f.Finish(prompt =>
        {
            askers.Add(prompt.Player);
            Assert.Equal(new[] { ally.ObjectId, support.ObjectId }.Order(),
                prompt.Affordances.Select(option => option.AnchorId).Order());
            return Decision.Take(prompt.Affordances.Single(option => option.AnchorId == discarded.ObjectId).Id);
        });

        Assert.Equal([1], askers);
        Assert.Equal(DeckType.DiscardPile, discarded.Area.Type);
        Assert.Equal(PlayArea.Of(1), kept.Area.PlayArea);
        Assert.True(DeckTypes.IsInPlay(kept.Area.Type));
        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
        Assert.Equal(DeckType.AlliesArea, other.Area.Type);
        Assert.Equal(world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1)), permanent.Area);
        Assert.Equal(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)), apocryphus.Area);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:permanent.4")]
    [Rule("rr:ability.3")]
    public void ApocryphusFindsNoTargetWhenTheOnlySupportIsPermanent()
    {
        // rr:permanent.4 removes Clear Skies from the valid targets, and
        // rr:ability.3: "that ability can only be initiated if it has at least one
        // valid target." Nothing is asked and nothing leaves play.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var permanent = world.CreateCard("36002", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var apocryphus = f.Reveal("11040", player: 1);

        f.Finish(prompt => throw new InvalidOperationException($"unexpected prompt: {prompt.Label}"));

        Assert.Equal(world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1)), permanent.Area);
        Assert.Equal(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)), apocryphus.Area);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:ability.3")]
    public void ApocryphusWithNoAllyOrSupportDiscardsNothingAndStillEngages()
    {
        // rr:ability.3: "If an ability specifies one or more targets, that ability
        // can only be initiated if it has at least one valid target." Player 1
        // controls only an upgrade; player 0's ally and support are not theirs.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var upgrade = world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var ally = world.CreateCard("01067", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var support = world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var apocryphus = f.Reveal("11040", player: 1);

        f.Finish(prompt => throw new InvalidOperationException($"unexpected prompt: {prompt.Label}"));

        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(DeckType.SupportsArea, support.Area.Type);
        Assert.Equal(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)), apocryphus.Area);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:boost-boost-icon")]
    [Rule("rr:boost-boost-icon.4")]
    [Rule("rr:you-your.10")]
    public void ApocryphusBoostExhaustsAChosenCharacterAndAddsABoostCard(bool scheme)
    {
        // 11040: "Boost: Exhaust a character you control. Give this enemy another
        // boost card." "You" is player 1, whom Rhino activates against; player 0's
        // ally is not offered. The Hydra Mercenary beneath prints one boost icon.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 0);
        world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("11040", world.AreaOf(DeckType.EncounterDeck));
        var identity = world.Seats[1].IdentityCard;
        var ally = world.CreateCard("01059", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var other = world.CreateCard("01067", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        KangTemporalFixture.Activate(world, f.Villain, scheme, 1);
        var askers = new List<int>();

        var events = f.Finish(prompt =>
        {
            askers.Add(prompt.Player);
            Assert.Equal(new[] { identity.ObjectId, ally.ObjectId }.Order(),
                prompt.Affordances.Select(option => option.AnchorId).Order());
            return Decision.Take(prompt.Affordances.Single(option => option.AnchorId == ally.ObjectId).Id);
        });

        Assert.Equal([1], askers);
        Assert.False(ally.Ready);
        Assert.True(identity.Ready);
        Assert.True(other.Ready);
        Assert.Equal(scheme ? 0 : 3, identity.Damage);
        Assert.Equal(scheme ? 2 : 0, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Single(world.AreaOf(DeckType.EncounterDeck).Cards);
        Assert.Equal(scheme ? 0 : 1, events.OfType<AttackCompleted>().Count());
        Assert.False(world.Agenda.IsBusy);
    }
}
