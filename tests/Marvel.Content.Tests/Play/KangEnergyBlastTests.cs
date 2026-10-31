using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.KangObligationFixture;

namespace Marvel.Content.Tests.Play;

// Energy Blast (11026) and the full authored book on a nonpublic Rhino board, where
// Rhino (ATK 2) is the villain in the only game area. Kang's public admission stays
// closed.
public sealed class KangEnergyBlastTests : CoreCardTestBase
{
    private const string EnergyBlast = "11026";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:you-your.10")]
    public void AnAlterEgoDiscardsAChosenAllyOrSupportTheyControl(bool support)
    {
        // 11026: "When Revealed (Alter-Ego): Discard an ally or support you control."
        // Player 1 reveals it; player 0's ally is not offered and nothing surges.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        AlterEgo(world, 1);
        var ally = world.CreateCard("01059", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var owned = world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var other = world.CreateCard("01067", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        int deck = world.AreaOf(DeckType.EncounterDeck).Cards.Count;
        var discarded = support ? owned : ally;
        var askers = new List<int>();
        f.Reveal(EnergyBlast, player: 1);

        f.Finish(prompt =>
        {
            askers.Add(prompt.Player);
            Assert.Equal(new[] { ally.ObjectId, owned.ObjectId }.Order(),
                prompt.Affordances.Select(option => option.AnchorId).Order());
            return Decision.Take(prompt.Affordances.Single(option => option.AnchorId == discarded.ObjectId).Id);
        });

        Assert.Equal([1], askers);
        Assert.Equal(DeckType.DiscardPile, discarded.Area.Type);
        Assert.True(DeckTypes.IsInPlay((support ? ally : owned).Area.Type));
        Assert.Equal(DeckType.AlliesArea, other.Area.Type);
        Assert.Equal(deck, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:surge.1")]
    [Rule("rr:permanent.4")]
    public void AnAlterEgoWithNothingToDiscardSurges(bool permanentOnly)
    {
        // "If you cannot, this card gains surge." Clear Skies is permanent from another
        // set, so it cannot be discarded (rr:permanent.4). rr:surge.1: "Deal yourself 1
        // facedown encounter card": player 1 is dealt the Hydra Mercenary.
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 2);
        var mercenary = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        AlterEgo(world, 1);
        var permanent = permanentOnly
            ? world.CreateCard("36002", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1)) : null;
        world.CreateCard("01067", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        f.Reveal(EnergyBlast, player: 1);

        f.Finish(prompt => throw new InvalidOperationException($"unexpected prompt: {prompt.Label}"));

        Assert.Equal([mercenary], world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        if (permanent is not null) Assert.Equal(DeckType.SupportsArea, permanent.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:reveal.4")]
    [Rule("rr:you-your.2")]
    public void AHeroIsAttackedByKang(int player)
    {
        // "When Revealed (Hero): Kang attacks you." The revealing hero takes Rhino's
        // ATK 2; the boost card prints no icons. Nothing is discarded or surges.
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var ally = world.CreateCard("01059", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player));
        f.Reveal(EnergyBlast, player);

        var events = f.Finish();

        var attack = Assert.Single(events.OfType<AttackCompleted>());
        Assert.Equal(f.Villain.ObjectId, attack.Enemy);
        Assert.Equal(2, world.Seats[player].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[1 - player].IdentityCard.Damage);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player)).Cards);
        Assert.False(world.Agenda.IsBusy);
    }
}
