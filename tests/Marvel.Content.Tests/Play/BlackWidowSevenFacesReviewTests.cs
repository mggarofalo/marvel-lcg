using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowSevenFacesReviewTests
{
    private static readonly string[] NestedCompletionFaces = ["08033", "08018"];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cancel.4")]
    [Rule("rr:surge.1")]
    public void ActualSpycraftCancelsOriginalExhaustionButEspionageInterruptsReplacementSurge(bool hero)
    {
        // Canceled treachery is "still placed in the encounter discard pile."
        // Surge: "Deal yourself 1 facedown encounter card." Actual 08018 replaces
        // the canceled Exhaustion; only the replacement's Surge can trigger 08033.
        var world = WidowEncounterFixture.Board(hero);
        var runner = WidowEncounterFixture.Runner();
        world.Abilities = runner;
        var identity = world.Seats[1].IdentityCard;
        var spycraft = WidowEncounterFixture.Upgrade(world, "08018");
        var espionage = WidowEncounterFixture.Upgrade(world, "08033");
        var extra = world.CreateCard("01182", world.AreaOf(DeckType.EncounterDeck));
        var replacement = world.CreateCard("01121", world.AreaOf(DeckType.EncounterDeck));
        var original = new RedSkullFixture(world, runner).Reveal("01191", 1);
        int surges = 0;
        bool canceled = false;
        var completions = new List<string>();
        for (int step = 0; step < 30; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            Assert.True(step < 29, "nested reveal did not finish");
            var spy = prompt.Affordances.SingleOrDefault(offer => offer.AnchorId == spycraft.ObjectId);
            var espionageOffer = prompt.Affordances.SingleOrDefault(offer => offer.AnchorId == espionage.ObjectId);
            if (spy is not null)
            {
                Assert.False(canceled);
                Assert.Equal(1, prompt.Player);
                Assert.Equal(Steps.CardRevealed, prompt.Trigger);
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(spy.Id), []);
                canceled = true;
            }
            else if (espionageOffer is not null)
            {
                surges++;
                Assert.Equal(1, prompt.Player);
                Assert.Equal("WhenSurgeWouldResolve", prompt.Trigger);
                Assert.Equal(DeckType.EncounterDiscardPile, original.Area.Type);
                Assert.Equal(replacement.ObjectId, world.Agenda.Occurrence!.Subject);
                Assert.Equal(DeckType.EngagedEnemiesArea, replacement.Area.Type);
                Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(espionageOffer.Id), []);
            }
            else
            {
                if (prompt.Trigger == "WhenPreparationAbilityResolved")
                {
                    var receipt = Assert.IsType<ResolvedPreparationAbility>(world.Agenda.Occurrence!.ActorFacts!.PreparationAbility);
                    Assert.Equal(1, receipt.Controller);
                    Assert.Equal(receipt.Face == "08018" ? spycraft.ObjectId : espionage.ObjectId, receipt.Source);
                    completions.Add(receipt.Face);
                }
                Assert.True(prompt.Cancellable);
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
            }
        }
        Assert.True(canceled);
        Assert.Equal(1, surges);
        Assert.Equal(hero ? NestedCompletionFaces : [], completions);
        Assert.True(identity.Ready);
        Assert.Equal(2, world.Seats[1].Hand.Cards.Count);
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Equal(DeckType.DiscardPile, spycraft.Area.Type);
        Assert.Equal(DeckType.DiscardPile, espionage.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, original.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, replacement.Area.Type);
        Assert.Equal(1, replacement.Area.PlayArea.Player);
        Assert.Same(extra, Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards));
        Assert.False(extra.FaceUp);
        Assert.Empty(world.AreaOf(DeckType.RevealingArea).Cards);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.3")]
    [Rule("rr:guard.1")]
    public void ActualDanceRechecksGuardAfterItsFirstAttackDefeatsTheGuard()
    {
        // "Each of those instances is considered a separate attack."
        // Guard: "cannot attack the villain" while engaged with this minion.
        var world = BlackWidowEventCompositionFixture.Board();
        var runner = world.Abilities!;
        var guard = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        guard.TakeDamage(2);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var source = BlackWidowEventCompositionFixture.PlayEvent(world, "08004");
        var first = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.DoesNotContain(first.Affordances, option => option.AnchorId == villain.ObjectId);
        Sequence.Answer(world, world.Facts, runner, first, Decision.Take(guard.ObjectId), []);
        for (int attack = 1; attack < 3; attack++)
        {
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
            Assert.Equal(1, prompt.Player);
            Assert.Equal(DeckType.EncounterDiscardPile, guard.Area.Type);
            Assert.DoesNotContain(prompt.Affordances, option => option.AnchorId == guard.ObjectId);
            Assert.Contains(prompt.Affordances, option => option.AnchorId == villain.ObjectId);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(villain.ObjectId), []);
        }
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(5, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
    }
}
