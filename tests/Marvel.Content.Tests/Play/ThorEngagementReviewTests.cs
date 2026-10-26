using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent controls for shared Core timing and retained minions.</summary>
public sealed class ThorEngagementReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:quickstrike.1")]
    [Rule("rr:ability.step.4")]
    public void QuickstrikeFinishesBeforeCoreHawkeyeCanShoot(bool reveal)
    {
        // Quickstrike is a "Forced Response (Hero)"; forced responses precede
        // optional responses. Hawkeye's printed response then damages Vulture.
        var world = Hero("01010a");
        var runner = AuthoredCards.Runner();
        var hawkeye = world.CreateCard("01066",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Reveal.EnterPlay(world, Cards, hawkeye, [], abilities: runner);
        var minion = world.CreateCard("01167", world.AreaOf(DeckType.RevealingArea));
        Schedule(world, minion, 0, reveal);
        var events = new List<GameEvent>();
        int responses = 0;
        for (int step = 0; step < 30; step++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) break;
            var arrow = prompt.Affordances.SingleOrDefault(a => a.AnchorId == hawkeye.ObjectId
                && a.Timing == AbilityType.Response);
            if (arrow is not null)
            {
                responses++;
                Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
                Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
            }
            Sequence.Answer(world, Cards, runner, prompt,
                arrow is not null ? Decision.Take(arrow.Id) : Decision.Decline, events);
        }
        Assert.Equal(1, responses);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(3, hawkeye.Tokens["c_arrow"]);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(2, minion.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:engage.2")]
    [Rule("rr:quickstrike.1")]
    [Rule("rr:when-revealed-abilities.2")]
    public void TransferQuickstrikesWithoutEntryOrRevealAndKeepsTheSameIncarnation(bool interpreted)
    {
        // An instructed engagement counts as the minion engaging that player;
        // "When Revealed abilities do not resolve" without a reveal. Hawkeye's
        // printed entry response applies only when the minion enters play.
        var world = Hero("06001a", players: 2);
        var runner = TransferRunner();
        var hawkeye = world.CreateCard("01066",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Reveal.EnterPlay(world, Cards, hawkeye, [], abilities: runner);
        for (int index = 0; index < 4; index++) world.CreateCard("01088", world.Seats[0].Deck);
        var minion = world.CreateCard("01167",
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        minion.TakeDamage(1);
        minion.Exhaust();
        minion.PlaceTokens("c_test", 3);
        Statuses.Give(world, minion, Statuses.Confused);
        var retained = minion.InstanceState;
        int incarnation = minion.Incarnation;
        var events = new List<GameEvent>();
        if (interpreted)
        {
            var source = world.CreateCard("06003", world.AreaOf(DeckType.RevealingArea,
                PlayArea.Of(0), cardOwner: 0));
            events.AddRange(runner.ExecuteReveal(world, source, 0).Events);
        }
        else Schedule(world, minion, 0, reveal: false);
        int thorResponses = 0;
        for (int step = 0; step < 30; step++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) break;
            Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == hawkeye.ObjectId
                && a.Timing == AbilityType.Response);
            var thor = prompt.Affordances.SingleOrDefault(a =>
                a.AnchorId == world.Seats[0].IdentityCard.ObjectId && a.Timing == AbilityType.Response);
            if (thor is not null)
            {
                thorResponses++;
                Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
                Assert.Equal(minion.ObjectId, world.Agenda.Occurrence!.Subject);
                Assert.Equal(0, world.Agenda.Occurrence.Player);
                Assert.False(world.Agenda.Occurrence.Is(Steps.CardEntersPlay));
                Assert.False(world.Agenda.Occurrence.Is(Steps.CardRevealed));
            }
            Sequence.Answer(world, Cards, runner, prompt,
                thor is not null ? Decision.Take(thor.Id) : Decision.Decline, events);
        }
        Assert.Equal(1, thorResponses);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(4, hawkeye.Tokens["c_arrow"]);
        Assert.Equal(PlayArea.Of(0), minion.Area.PlayArea);
        Assert.Same(retained, minion.InstanceState);
        Assert.Equal(incarnation, minion.Incarnation);
        Assert.Equal(1, minion.Damage);
        Assert.False(minion.Ready);
        Assert.Equal(3, minion.Tokens["c_test"]);
        Assert.True(Statuses.Has(world, minion, Statuses.Confused));
    }

    private static void Schedule(World world, Card minion, int player, bool reveal)
    {
        if (reveal)
            world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
                1, 2, Subject: minion.ObjectId, Seat: player));
        else
            EncounterEntry.Schedule(world, EncounterEntryRequest.For(minion, player,
                new CardEntryPlacement(Verb: "Put_Into_Play")), new EffectCompletion(1, 1));
    }

    private static AbilityRunner TransferRunner()
    {
        // The actual Thor row observes a synthetic transfer without public pack admission.
        // Vulture's synthetic reveal damage distinguishes an accidental reveal.
        var local = AbilityCatalog.Parse("""
            {"cards":[
              {"card":"06003","abilities":[{
                "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
                "effect":{"putIntoPlay":{"card":{"titled":"Vulture"},"where":"engagedWithYou"}}}]},
              {"card":"01167","abilities":[{
                "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
                "effect":{"dealDamage":{"cards":"you","amount":1}}}]}
            ]}
            """);
        return new AbilityRunner(AuthoredCards.Book with
        {
            Abilities = [.. AuthoredCards.Book.Abilities, .. local.Abilities],
            Authored = AuthoredCards.Book.Authored.Union(local.Authored).ToHashSet(StringComparer.Ordinal),
        });
    }
}
