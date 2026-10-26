using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowSpycraftTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [Rule("rr:surge.1")]
    public void ActualSpycraftCancelsOwnRevealAndReplacesItExactlyOnce(int resolver, bool decline)
    {
        // Surge is "Deal yourself 1 facedown encounter card." Canceling the
        // original card's effects cancels its surge and entry, before replacement.
        var world = WidowEncounterFixture.Board();
        var runner = WidowEncounterFixture.Runner();
        world.Abilities = runner;
        var spycraft = WidowEncounterFixture.Upgrade(world, "08018");
        var replacement = world.CreateCard("01182", world.AreaOf(DeckType.EncounterDeck));
        var fixture = new RedSkullFixture(world, runner);
        var original = fixture.Reveal("01121", resolver);
        bool offered = false;
        while (Sequence.Work(world, world.Facts, runner, []) is { } prompt)
        {
            var offer = prompt.Affordances.SingleOrDefault(a => a.AnchorId == spycraft.ObjectId);
            if (offer is not null)
            {
                offered = true;
                Assert.Equal(1, prompt.Player);
                Sequence.Answer(world, world.Facts, runner, prompt,
                    decline ? Decision.Decline : Decision.Take(offer.Id), []);
            }
            else Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        }
        bool canceled = resolver == 1 && !decline;
        Assert.Equal(resolver == 1, offered);
        Assert.Equal(canceled ? DeckType.DiscardPile : DeckType.UpgradesArea, spycraft.Area.Type);
        Assert.Equal(canceled ? DeckType.EncounterDiscardPile : DeckType.EngagedEnemiesArea, original.Area.Type);
        Assert.Equal(canceled ? DeckType.EngagedEnemiesArea : DeckType.DealtEncounterCardsDeck, replacement.Area.Type);
        Assert.Equal(PlayArea.Of(resolver), replacement.Area.PlayArea);
        Assert.Equal(canceled ? 0 : 1, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(resolver)).Cards.Count);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
    }
}
