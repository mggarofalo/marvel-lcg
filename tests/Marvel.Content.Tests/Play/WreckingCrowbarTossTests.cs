using Marvel.Content.Tests.Cards;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingCrowbarTossTests
{
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [Rule("rr:then.1")]
    [Rule("rr:then.2")]
    [Rule("rr:stun-stunned.7")]
    public void CounterMovesOnlyAfterWreckersActivationFullyResolves(bool hero, bool cancelled, bool tied)
    {
        // 07012: "Wrecker attacks you [schemes]. Then, move the active villain counter
        // to the villain whose side scheme has the least threat."
        // rr:then.2: incomplete pre-then text means the post-then text "does not attempt to resolve".
        // rr:stun-stunned.7: the character "is not considered to have attacked".
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07012", 0, hero ? "01001a" : "01001b");
        SetThreat(groups, tied);
        if (cancelled) Statuses.Give(world, groups[0].Villain, hero ? Statuses.Stunned : Statuses.Confused);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        runner.ExecuteReveal(world, source, 0);
        int choices = ResolveChoices(world, groups, runner, hero, tied && !cancelled);
        Assert.Equal(tied && !cancelled ? 1 : 0, choices);
        AssertOutcome(world, groups, hero, cancelled);
    }

    private static void SetThreat(EncounterDeckGroup[] groups, bool tied)
    {
        for (int index = 0; index < groups.Length; index++)
            groups[index].Scheme.PlaceTokens("k_threat", index == 3 || tied && index == 2 ? 0 : 5);
    }

    private static int ResolveChoices(World world, EncounterDeckGroup[] groups, AbilityRunner runner,
        bool hero, bool expectChoice)
    {
        int choices = 0;
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            Assert.True(step < 10);
            if (prompt.Cancellable)
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
            else
            {
                Assert.True(expectChoice);
                Assert.Equal(0, prompt.Player);
                Assert.Equal(hero ? 4 : 0, world.Seats[0].IdentityCard.Damage);
                Assert.Equal(hero ? 5 : 7, groups[0].Scheme.Tokens.GetValueOrDefault("k_threat"));
                Assert.Equal(new[] { groups[2].Villain.ObjectId, groups[3].Villain.ObjectId },
                    prompt.Affordances.Select(option => option.Id));
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(groups[3].Villain.ObjectId), []);
                choices++;
            }
        }
        return choices;
    }

    private static void AssertOutcome(World world, EncounterDeckGroup[] groups, bool hero, bool cancelled)
    {
        Assert.Same(groups[cancelled ? 1 : 3], world.EncounterTopology!.Active);
        Assert.Equal(hero && !cancelled ? 4 : 0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(!hero && !cancelled ? 7 : 5, groups[0].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(cancelled ? 2 : 1, groups[1].Deck.Cards.Count);
    }
}
