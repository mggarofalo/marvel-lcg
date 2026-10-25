using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ActiveCounterSequenceAdmissionTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:target.3.4")]
    [Rule("rr:move.1")]
    [Rule("rr:activation.8")]
    public void DeferredAttackKeepsCurrentHolderEligibleOnlyWhenItCanBeAffected(
        bool attack, bool tied)
    {
        // Synthetic Boost contract. rr:target.3.4: "at least one of those effects
        // must be able to affect the target." rr:move.1: "it cannot move to its
        // same (current) placement." rr:activation.8: "resolves after the current
        // activation has finished resolving".
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07014", 1);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
        {"cards":[{"card":"07014","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
          "effect":{"chooseCard":{"chooser":"firstPlayer",
            "from":{"query":"villainsWithLeastSchemeThreat"},
            "effect":{"seq":[{"moveActiveCounter":"chosen"},
              {"if":{"test":{"inAttackChain":"attack"},"then":{
                "afterActivation":{"effect":{"enemyAttacks":{
                  "enemies":"chosen","against":"you","dealBoostCards":"false"}}}
              }}}
            ]}}}
        }]}]}
        """));
        world.Abilities = runner;
        SetLeastThreatGroups(groups, tied);
        World.MoveToTop(source, groups[1].Deck);
        world.Agenda.Add(new PhaseStep(attack ? new EnemyAttackOperation() : new EnemySchemeOperation(),
            1, 1, Subject: groups[0].Villain.ObjectId, Seat: 0));
        List<GameEvent> events = [];
        bool chose = Finish(world, groups, runner, events, attack, tied);

        Assert.Equal(attack && tied, chose);
        Assert.Same(groups[!attack && tied ? 2 : 1], world.EncounterTopology!.Active);
        Assert.Equal(attack ? new[] { groups[0].Villain.ObjectId, groups[1].Villain.ObjectId } : [],
            events.OfType<AttackBoundary>().Where(item => item.Started).Select(item => item.Source));
        Assert.All(groups, group => Assert.Equal(2, group.Deck.Cards.Count));
        Assert.Same(groups[1].Discard, source.Area);
        if (attack)
        {
            var boundaries = events.OfType<AttackBoundary>().ToArray();
            Assert.Collection(boundaries,
                item => Assert.True(item.Started), item => Assert.False(item.Started),
                item => Assert.True(item.Started), item => Assert.False(item.Started));
        }
    }

    private static void SetLeastThreatGroups(EncounterDeckGroup[] groups, bool tied)
    {
        for (int index = 0; index < groups.Length; index++)
            groups[index].Scheme.PlaceTokens("k_threat",
                index == 1 || tied && index == 2 ? 0 : 5);
    }
    private static bool Finish(World world, EncounterDeckGroup[] groups,
        AbilityRunner runner, List<GameEvent> events, bool attack, bool tied)
    {
        bool chose = false;
        for (int count = 0; ; count++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            Assert.True(count < 10);
            if (prompt.Asking == Question.Element)
            {
                Assert.True(attack && tied);
                Assert.Equal(new[] { groups[1].Villain.ObjectId, groups[2].Villain.ObjectId },
                    prompt.Affordances.Select(option => option.Id));
                Assert.Equal(world.FirstPlayer, prompt.Player);
                Sequence.Answer(world, world.Facts, runner, prompt,
                    Decision.Take(groups[1].Villain.ObjectId), events);
                chose = true;
            }
            else
            {
                Assert.True(prompt.Cancellable, prompt.Description);
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
            }
        }

        return chose;
    }
}
