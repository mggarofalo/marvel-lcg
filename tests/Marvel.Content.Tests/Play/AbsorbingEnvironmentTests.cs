using Marvel.Rules.Play;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AbsorbingEnvironmentTests
{
    [Fact]
    [Rule("rr:when-revealed-abilities.2")]
    public void SetupPutsEnvironmentIntoPlayWithoutSurgeAndShufflesAllDiscardedCardsBack()
    {
        // Putting into play does not reveal. Ruling eaf80919b355833a confirms setup ignores Surge.
        var f = AbsorbingManFixture.Create();
        var oldDiscard = f.Card("04087", DeckType.EncounterDiscardPile);
        var environment = f.Card("04080", DeckType.EncounterDeck);
        var skipped = f.Card("04085", DeckType.EncounterDeck);
        f.Runner.ExecuteSetup(f.World, f.Card("04079a"));
        f.Finish();
        Assert.Equal(DeckType.EnvironmentArea, environment.Area.Type);
        Assert.Equal(DeckType.EncounterDeck, oldDiscard.Area.Type);
        Assert.Equal(DeckType.EncounterDeck, skipped.Area.Type);
        Assert.Empty(f.World.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }

    [Theory]
    [InlineData("04080")]
    [InlineData("04081")]
    [InlineData("04082")]
    [InlineData("04083")]
    [Rule("rr:when-revealed-abilities.2")]
    public void BoostEnvironmentDiscardsEveryOtherEnvironmentButDoesNotSurge(string face)
    {
        // Boost says "Put this card into play", not reveal it.
        var f = AbsorbingManFixture.Create();
        var first = f.Card("04080", DeckType.EnvironmentArea);
        var second = f.Card("04081", DeckType.EnvironmentArea);
        var incoming = f.Card(face, DeckType.BoostingArea);
        f.Runner.ExecuteBoost(f.World, incoming, 0);
        var events = f.Finish();
        int beforeEntry = events.FindIndex(item => item is CardsMoved moved && moved.Cards.Any(card => card.Card == incoming.ObjectId));
        Assert.True(beforeEntry > events.FindIndex(item => item is CardsMoved moved && moved.Cards.Any(card => card.Card == first.ObjectId)));
        Assert.True(beforeEntry > events.FindIndex(item => item is CardsMoved moved && moved.Cards.Any(card => card.Card == second.ObjectId)));
        Assert.Equal(DeckType.EncounterDiscardPile, first.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, second.Area.Type);
        Assert.Same(incoming, Assert.Single(f.World.AreaOf(DeckType.EnvironmentArea).Cards));
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }

    [Fact]
    public void NormalRevealOpensTheSameEntryInterruptBeforePlacementAndDoesSurge()
    {
        var f = AbsorbingManFixture.Create();
        var prior = f.Card("04083", DeckType.EnvironmentArea);
        var incoming = f.Card("04080");
        f.World.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: incoming.ObjectId, Seat: 0));
        var events = f.Finish();
        Assert.Equal(DeckType.EncounterDiscardPile, prior.Area.Type);
        Assert.Equal(DeckType.EnvironmentArea, incoming.Area.Type);
        Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        int discarded = events.FindIndex(item => item is CardsMoved moved && moved.Cards.Any(card => card.Card == prior.ObjectId));
        int entered = events.FindIndex(item => item is CardsMoved moved && moved.Cards.Any(card => card.Card == incoming.ObjectId));
        Assert.True(discarded >= 0 && entered > discarded);
    }
}
