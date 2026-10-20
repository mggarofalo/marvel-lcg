using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EncounterEntryContinuationTests
{
    [Fact]
    public void LaterTextSeesTheCompletedEntry()
    {
        var f = AbsorbingManFixture.Create(extra: """
            {"card":"01006","abilities":[{"trigger":{"timing":"Action","event":"WhenActionTriggered","subject":"this"},"effect":{"seq":[
              {"putIntoPlay":{"card":{"cardsIn":{"area":"encounterDiscardPile","title":"Dense Forest"}},"where":"printedDestination"}},
              {"if":{"test":{"titleInPlay":"Dense Forest"},"then":{"placeCounters":{"card":"this","counter":"finished","count":1}}}}
            ]}}]}
            """);
        var source = InPlay(f.World, "01006");
        var incoming = f.Card("04080", DeckType.EncounterDiscardPile);
        f.Runner.ExecuteAction(f.World, new PlayerAction(Assert.Single(f.Runner.Actions(f.World, 0), action => action.Card == source.ObjectId), [], []));
        Assert.Equal(DeckType.EncounterDiscardPile, incoming.Area.Type);
        Assert.Equal(0, source.Tokens.GetValueOrDefault("c_finished"));
        f.Finish();
        Assert.Equal(DeckType.EnvironmentArea, incoming.Area.Type);
        Assert.Equal(1, source.Tokens["c_finished"]);
    }

    [Theory]
    [InlineData("cancelOccurrence", false)]
    [InlineData("discard", true)]
    public void CancelledOrMovedEntryCompletesWithZeroAndDoesNotReenter(string effect, bool discarded)
    {
        string instruction = effect == "discard" ? "{\"discard\":\"trigger.subject\"}" : "{\"cancelOccurrence\":\"trigger\"}";
        var f = AbsorbingManFixture.Create(extra: $$$$"""
            {"card":"01006","abilities":[{"trigger":{"timing":"ForcedInterrupt","event":"WhenCardEntersPlay","subject":"game"},
              "when":{"isKind":{"card":"trigger.subject","kind":"environment"}},"effect":{{{{instruction}}}}}]}
            """);
        InPlay(f.World, "01006");
        World.MoveToTop(f.Scheme, f.World.AreaOf(DeckType.RemovedArea));
        var incoming = f.Card("04080", DeckType.BoostingArea);
        var receipt = new EffectCompletion(1, 1);
        EncounterEntry.Schedule(f.World, EncounterEntryRequest.For(incoming, 0, new CardEntryPlacement(Verb: "Put_Into_Play")), receipt);
        f.Finish();
        Assert.True(receipt.IsComplete);
        Assert.Equal(0, receipt.Applied);
        Assert.Equal(discarded ? DeckType.EncounterDiscardPile : DeckType.BoostingArea, incoming.Area.Type);
    }
    [Fact]
    public void EntryAlreadyMovedBeforeItsWindowDoesNotDiscardExistingEnvironment()
    {
        var f = AbsorbingManFixture.Create();
        var prior = f.Card("04081", DeckType.EnvironmentArea);
        var incoming = f.Card("04080", DeckType.BoostingArea);
        var receipt = new EffectCompletion(1, 1);
        EncounterEntry.Schedule(f.World, EncounterEntryRequest.For(incoming, 0,
            new CardEntryPlacement(Verb: "Put_Into_Play")), receipt);
        World.MoveToTop(incoming, f.World.AreaOf(DeckType.EncounterDiscardPile));
        f.Finish();
        Assert.Equal(0, receipt.Applied);
        Assert.Equal(DeckType.EnvironmentArea, prior.Area.Type);
    }

    [Fact]
    public void AttachmentDoesNotEnterOnADepartedExplicitHost()
    {
        var f = AbsorbingManFixture.Create();
        var host = f.Card("01100", DeckType.EngagedEnemiesArea);
        var incoming = f.Card("04084", DeckType.EncounterDiscardPile);
        var receipt = new EffectCompletion(1, 1);
        EncounterEntry.Schedule(f.World, EncounterEntryRequest.For(incoming, 0,
            new CardEntryPlacement(AttachmentTarget: host.ObjectId, Verb: "Put_Into_Play")), receipt);
        World.MoveToTop(host, f.World.AreaOf(DeckType.EncounterDiscardPile));
        f.Finish();
        Assert.Equal(0, receipt.Applied);
        Assert.Equal(DeckType.EncounterDiscardPile, incoming.Area.Type);
    }
}
