using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class UnattachedRevealCleanupTests
{
    [Fact]
    [Rule("rr:attach-to.3")]
    [Rule("rr:surge.1")]
    public void FailedPlacementResolvesItsRevealTextThenDiscardsTheAttachment()
    {
        // rr:attach-to.3: "If such a card cannot remain in its prior state or
        // game area, discard it." 01163 gains surge when no minion is in play.
        // rr:surge.1: "deal yourself 1 facedown encounter card".
        var f = RedSkullFixture.Create();
        var attachment = f.Reveal("01163");
        f.Finish();
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
        Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }

    [Fact]
    [Rule("rr:reveal.1")]
    public void AttachmentWithoutAttachToTextRemainsOnTheTableOutOfPlay()
    {
        // rr:reveal.1: "If it does not have 'attach to' text, place it on the
        // table in front of the player revealing it. (It is not in play.)"
        // Synthetic declaration distinguishes absent text from failed placement.
        var f = RedSkullFixture.Create();
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01163","abilities":[]}]}
            """));
        f.World.Abilities = runner;
        f = new(f.World, runner);
        var attachment = f.Reveal("01163");
        f.Finish();
        Assert.Equal(DeckType.RevealingArea, attachment.Area.Type);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }
}
