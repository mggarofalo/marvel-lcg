using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingStatusRemovalTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [Rule("rr:status-cards.1")]
    [Rule("rr:surge.1")]
    public void UncannyRemovesBothKindsFromEveryVillainAndSurgesOnlyWhenNothingRemoved(int player, bool afflicted)
    {
        // 07045: "Remove each stunned and confused status card from each villain."
        // "If no status cards were removed this way, this card gains surge."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07045", 2, players: 2);
        var runner = Runner();
        world.Abilities = runner;
        foreach (var group in groups)
        {
            Statuses.Give(world, group.Villain, Statuses.Tough);
            if (!afflicted) continue;
            Statuses.Give(world, group.Villain, Statuses.Stunned);
            Statuses.Give(world, group.Villain, Statuses.Confused);
        }
        Statuses.Give(world, world.Seats[player].IdentityCard, Statuses.Stunned);
        runner.ExecuteReveal(world, source, player);
        Sequence.Finish(world, world.Facts, runner, []);
        foreach (var group in groups)
        {
            Assert.Equal(0, Statuses.Count(world, group.Villain, Statuses.Stunned));
            Assert.Equal(0, Statuses.Count(world, group.Villain, Statuses.Confused));
            Assert.Equal(1, Statuses.Count(world, group.Villain, Statuses.Tough));
        }
        Assert.Equal(1, Statuses.Count(world, world.Seats[player].IdentityCard, Statuses.Stunned));
        Assert.Equal(afflicted ? 0 : 1, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player)).Cards.Count);
        Assert.Equal(afflicted ? 2 : 1, groups[1].Deck.Cards.Count);
    }

    [Fact]
    public void StatusRemovalActionRequiresAStatusItCanActuallyRemove()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01007","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"removeStatuses":{"cards":{"query":"villains"},"statuses":["stunned","confused"]}}
            }]}]}
            """));
        Card? source = null;
        var (_, world) = Playing(board => source = InPlay(board, "01007"), abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, villain, Statuses.Tough);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
        Statuses.Give(world, villain, Statuses.Stunned);
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == source!.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(0, Statuses.Count(world, villain, Statuses.Stunned));
        Assert.Equal(1, Statuses.Count(world, villain, Statuses.Tough));
        Assert.DoesNotContain(runner.Actions(world, 0), offer => offer.Card == source!.ObjectId);
    }

    [Fact]
    [Rule("rr:steady.1")]
    public void EveryPhysicalStatusIsRemovedEvenWhenSteadyRequiresTwo()
    {
        // Steady characters require "two status cards of the same type" to be afflicted.
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07045", 2);
        var runner = Runner();
        world.Abilities = runner;
        var villain = groups[0].Villain;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "steady", Amount: 1,
            Affects: villain.ObjectId));
        for (int count = 0; count < 2; count++)
        {
            Statuses.Give(world, villain, Statuses.Stunned);
            Statuses.Give(world, villain, Statuses.Confused);
        }
        Assert.Equal(2, Statuses.Count(world, villain, Statuses.Stunned));
        Assert.Equal(2, Statuses.Count(world, villain, Statuses.Confused));
        runner.ExecuteReveal(world, source, 0);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(0, Statuses.Count(world, villain, Statuses.Stunned));
        Assert.Equal(0, Statuses.Count(world, villain, Statuses.Confused));
        Assert.Equal(2, groups[1].Deck.Cards.Count);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[\"stunned\",\"stunned\"]")]
    [InlineData("[\"poisoned\"]")]
    [InlineData("\"stunned\"")]
    public void StatusKindsAreAnExplicitDistinctNonemptyVocabulary(string statuses)
    {
        string book = """
            {"cards":[{"card":"07045","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"removeStatuses":{"cards":{"query":"villains"},"statuses":STATUS}}
            }]}]}
            """.Replace("STATUS", statuses, StringComparison.Ordinal);
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(book)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:you-your.5")]
    public void UncannyBoostConfusesTheResolvingPlayersIdentityOnly(int player)
    {
        // Printed 07045 Boost: "You are confused." rr:you-your.5: "If a card ability places a
        // status card on 'you' (such as 'you are stunned'), the player resolving that card
        // ability places that status card on their identity." Its When Revealed removal does not run.
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07045", 2, players: 2);
        var runner = Runner();
        world.Abilities = runner;
        Statuses.Give(world, groups[0].Villain, Statuses.Stunned);
        runner.ExecuteBoost(world, source, player);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.True(Statuses.Has(world, world.Seats[player].IdentityCard, Statuses.Confused));
        Assert.False(Statuses.Has(world, world.Seats[1 - player].IdentityCard, Statuses.Confused));
        Assert.Equal(1, Statuses.Count(world, groups[0].Villain, Statuses.Stunned));
    }

    private static AbilityRunner Runner() => AuthoredCards.Runner();
}
