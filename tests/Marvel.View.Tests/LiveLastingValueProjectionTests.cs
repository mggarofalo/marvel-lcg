using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class LiveLastingValueProjectionTests
{
    [Fact]
    [Rule("rr:lasting-effects.1")]
    [Rule("rr:lasting-effects.3")]
    [Rule("rr:lasting-effects.5")]
    public void LiveCalculationAndPersistentContributionFollowCurrentAmountAndHistoricalSource()
    {
        // "Lasting effects update whenever the game state updates" and continue
        // "whether or not the card that created the lasting effect is in play."
        // Synthetic registration isolates the projection contract, not Arc Reactor's text.
        World world = PersistentFixture.Board();
        Card villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        Card source = world.CreateCard("01035", PersistentFixture.Controlled(world));
        var effect = new ContinuousEffect(EffectSource.LastingEffect, "attack", Card: source.ObjectId,
            Affects: villain.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound))
        { LiveAmount = new ContinuousAmount.ControlledUpgrades(0) };
        world.Effects.Register(effect);
        world.Effects.Register(new(EffectSource.LastingEffect, "attack", 4,
            source.ObjectId, villain.ObjectId, Duration.UntilEndOf(TimingPoints.EndOfRound)));

        AssertValues(world, villain, source, 1);
        world.CreateCard("01057", PersistentFixture.Controlled(world));
        AssertValues(world, villain, source, 2);
        Discard.Card(world, source, "fixture", []);
        CardEffectiveValue historical = Value(world, villain);
        Assert.Equal(7, historical.CurrentValue);
        Assert.Equal([1L, 4L], historical.Calculation.Select(step => step.Amount));
        Assert.All(historical.Calculation, step =>
        {
            Assert.True(step.Source!.Historical);
            Assert.Null(step.Source.CardId);
        });
        Assert.DoesNotContain(PersistentFixture.Visible(world).Areas.SelectMany(area => area.Cards),
            card => card.Id == source.ObjectId && card.Persistent is not null);
        Assert.Equal(2, world.Effects.Expire(TimingPoints.EndOfRound));
        Assert.Equal(2, Value(world, villain).CurrentValue);
        Assert.Empty(Value(world, villain).Calculation);
    }

    private static void AssertValues(World world, Card villain, Card source, long live)
    {
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long consumed = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Current;
        int count = world.Agenda.Count;
        var registrations = world.Effects.Registered.ToArray();
        CardEffectiveValue value = Value(world, villain);
        Assert.Equal(6 + live, value.CurrentValue);
        Assert.Equal([live, 4L], value.Calculation.Select(step => step.Amount));
        Assert.Equal([live, 4L], PersistentFixture.Facts(world, source).Contributions
            .Where(item => item.TargetId == villain.ObjectId).Select(item => item.Amount));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Current);
        Assert.Equal(count, world.Agenda.Count);
        Assert.Equal(registrations, world.Effects.Registered);
    }

    private static CardEffectiveValue Value(World world, Card card) =>
        PersistentFixture.Card(world, card).Face!.EffectiveValues["ATK"];
}
