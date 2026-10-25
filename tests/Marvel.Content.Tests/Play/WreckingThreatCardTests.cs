using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Canonical printed-card contracts in arranged worlds, without pack admission.</summary>
public sealed class WreckingThreatCardTests
{
    [Theory]
    [InlineData(0, false, 7)]
    [InlineData(0, true, 4)]
    [InlineData(2, false, 2)]
    [InlineData(2, true, 2)]
    public void MysticalLinkBoostOnlyIncreasesWreckersCurrentActivation(
        int attacker, bool placeThreat, int expectedDamage)
    {
        // 07015: "Wrecker gets +3 ATK for this activation unless you place 2 threat on his side scheme."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07015", 0);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        // Synthetic cross-deck placement exercises active-deck dealing while
        // preserving the printed card's physical ownership by Wrecker's deck.
        World.MoveToTop(source, groups[1].Deck);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: groups[attacker].Villain.ObjectId, Seat: 0));

        Finish(world, runner, placeThreat);

        Assert.Equal(expectedDamage, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(placeThreat ? 2 : 0, groups[0].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.All(groups.Skip(1), group => Assert.Equal(0, group.Scheme.Tokens.GetValueOrDefault("k_threat")));
        Assert.Same(groups[1], world.EncounterTopology!.Active);
        Assert.Same(groups[0].Discard, source.Area);
        Assert.DoesNotContain(world.Effects.Active(), effect => effect.Card == source.ObjectId);
        Assert.Equal(2, StateFields.Modified(world, groups[0].Villain, "attack", world.Facts, 1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MysticalLinkBoostDuringSchemeDoesNotIncreaseSchemeValueOrLeakAttackBonus(bool placeThreat)
    {
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07015", 0, "01001b");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        World.MoveToTop(source, groups[1].Deck);
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2,
            Subject: groups[0].Villain.ObjectId, Seat: 0));

        Finish(world, runner, placeThreat);

        Assert.Equal(placeThreat ? 4 : 2, groups[0].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.All(groups.Skip(1), group => Assert.Equal(0, group.Scheme.Tokens.GetValueOrDefault("k_threat")));
        Assert.DoesNotContain(world.Effects.Active(), effect => effect.Card == source.ObjectId);
        Assert.Same(groups[1], world.EncounterTopology!.Active);
    }

    private static void Finish(World world, AbilityRunner runner, bool placeThreat)
    {
        bool chose = false;
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            Assert.True(step < 15, prompt.Description);
            Decision answer;
            if (prompt.Asking == Question.Option)
            {
                Assert.False(chose);
                Assert.Contains(prompt.Affordances, option => (option.Description ?? option.Label).Contains("+3 ATK", StringComparison.Ordinal));
                Assert.Contains(prompt.Affordances, option => (option.Description ?? option.Label).Contains("2 threat", StringComparison.Ordinal));
                chose = true;
                answer = Decision.Take(placeThreat ? 1 : 0);
            }
            else
            {
                Assert.True(prompt.Cancellable, prompt.Description);
                answer = Decision.Decline;
            }
            Sequence.Answer(world, world.Facts, runner, prompt, answer, []);
        }
        Assert.True(chose);
    }
}
