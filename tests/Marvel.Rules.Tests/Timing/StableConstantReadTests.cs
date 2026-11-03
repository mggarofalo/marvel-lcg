using Marvel.Core.Digest;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Timing;

/// <summary>
/// A read that changes nothing settles the constant abilities once, and a
/// nested check that changes the board inside it settles afresh.
/// </summary>
public sealed class StableConstantReadTests
{
    [Rule("rr:modifiers")]
    [Fact]
    public void ADigestSettlesTheConstantAbilitiesOnce()
    {
        // "The game constantly checks and (if necessary) updates the count of any
        // variable quantity that is being modified." Engine choice: a digest
        // changes nothing, so every field it reads shares one settlement. A
        // settlement walks each card in play until two passes agree -- two passes
        // over the two allies here -- and the next digest settles again.
        var table = Table.Create();

        StateDigest digest = table.World.Digest();

        Assert.Equal(4, table.Abilities.Calls);
        Assert.Equal(1, Field(digest, table.Target, "attack"));
        _ = table.World.Digest();
        Assert.Equal(8, table.Abilities.Calls);
    }

    [Rule("rr:modifiers")]
    [Fact]
    public void ASettlementOutsideAReadIsNotReused()
    {
        // Between reads the board may change in any way, so a settlement made
        // outside a stable read is not kept for the next one.
        var table = Table.Create();

        Assert.Equal(1, Attack(table.World, table.Target));
        int outside = table.Abilities.Calls;
        _ = table.World.Digest();
        Assert.Equal(outside + 4, table.Abilities.Calls);
    }

    [Rule("rr:modifiers")]
    [Fact]
    public void CapturingCharacterHealthSettlesOnce()
    {
        var table = Table.Create();

        var health = table.World.Effects.CaptureCharacterHealth();

        Assert.Equal(2, health.Count);
        Assert.Equal(4, table.Abilities.Calls);
    }

    [Rule("rr:modifiers")]
    [Fact]
    public void AWindowsWaitingAbilitiesSettleOnce()
    {
        var table = Table.Create();
        table.Abilities.WhileWaiting = world =>
        {
            Assert.Equal(1, Attack(world, table.Target));
            Assert.Equal(1, Attack(world, table.Target));
        };

        _ = table.World.WindowAbilities.Waiting(table.World, Occurrence(), WindowKind.Response);

        Assert.Equal(4, table.Abilities.Calls);
    }

    [Rule("rr:modifiers")]
    [Fact]
    public void ADepartureCheckedInsideAReadSettlesWithoutTheDepartingCard()
    {
        // A preflighted departure marks its cards as leaving for the length of the
        // check, and a departing card's constants are no longer in force.
        var table = Table.Create();
        table.Abilities.WhileWaiting = world =>
        {
            var ending = world.Effects.PreflightConstantsEnding(table.Source);
            Assert.Equal(1, Attack(world, table.Target));
            using (ending.Begin())
            {
                Assert.Equal(0, Attack(world, table.Target));
            }

            Assert.Equal(1, Attack(world, table.Target));
        };

        _ = table.World.WindowAbilities.Waiting(table.World, Occurrence(), WindowKind.Response);
    }

    [Rule("rr:modifiers.2")]
    [Fact]
    public void AnEffectRegisteredInsideAReadReachesTheConstantsThatDependOnIt()
    {
        // "The calculation of a value treats all modifiers as being applied
        // simultaneously." The target's own constant
        // grants it THW while its ATK is at least 1, so a lasting ATK bonus that
        // arrives inside the read switches that constant on.
        var table = Table.Create(dependent: true, sourceGrants: false);
        table.Abilities.WhileWaiting = world =>
        {
            Assert.Equal(0, Thwart(world, table.Target));
            world.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect, "attack", Amount: 1,
                Card: table.Source.ObjectId, Affects: table.Target.ObjectId,
                Lasts: Duration.WhileInPlay));
            Assert.Equal(1, Thwart(world, table.Target));
        };

        _ = table.World.WindowAbilities.Waiting(table.World, Occurrence(), WindowKind.Response);
    }

    [Rule("rr:modifiers.2")]
    [Fact]
    public void AnEffectReplacedInsideAReadReachesTheConstantsThatDependOnIt()
    {
        // One lasting effect ends and another begins, so the board holds as many
        // registered effects as before but no longer the one the target's
        // constant depends on.
        var table = Table.Create(dependent: true, sourceGrants: false);
        var bonus = table.World.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "attack", Amount: 1,
            Card: table.Source.ObjectId, Affects: table.Target.ObjectId,
            Lasts: Duration.WhileInPlay));
        table.Abilities.WhileWaiting = world =>
        {
            Assert.Equal(1, Thwart(world, table.Target));
            bonus.Dispose();
            world.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect, "attack", Amount: 1,
                Card: table.Target.ObjectId, Affects: table.Source.ObjectId,
                Lasts: Duration.WhileInPlay));
            Assert.Equal(0, Thwart(world, table.Target));
        };

        _ = table.World.WindowAbilities.Waiting(table.World, Occurrence(), WindowKind.Response);
    }

    [Rule("rr:hit-points.3.1")]
    [Fact]
    public void AConstantEndingPreflightedInsideAReadFindsTheCharacterItLeavesLethal()
    {
        // "If an ability that says an ally or minion 'gets +X hit points' ceases to
        // be in effect and causes that ally or minion to have damage on it equal to
        // or greater than its hit points, that ally or minion is defeated." Without the source's ATK bonus the target's own +1 hit point
        // constant switches off, so the departure the preflight plans includes it.
        var table = Table.Create(dependent: true, sourceGrants: true);
        table.Target.TakeDamage(1);
        table.Abilities.WhileWaiting = world =>
        {
            Assert.Equal(2, DamagePlacement.Health(world, world.Facts, table.Target));
            using (world.Effects.PreflightConstantsEnding(table.Source).Begin())
            {
                Assert.True(world.Effects.IsDeparting(table.Target));
            }
        };

        _ = table.World.WindowAbilities.Waiting(table.World, Occurrence(), WindowKind.Response);
    }

    [Rule("rr:hit-points.3.1")]
    [Fact]
    public void ALastingEffectEndingInsideAReadFindsTheCharacterItLeavesLethal()
    {
        var table = Table.Create(dependent: true, sourceGrants: false);
        table.Target.TakeDamage(1);
        var bonus = table.World.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "attack", Amount: 1,
            Card: table.Source.ObjectId, Affects: table.Target.ObjectId,
            Lasts: Duration.WhileInPlay));
        table.Abilities.WhileWaiting = world =>
        {
            Assert.Equal(2, DamagePlacement.Health(world, world.Facts, table.Target));
            // This bare board has nothing on the agenda for the defeat to join, so
            // reaching the defeat raises. What is held is that it is reached.
            var raised = Assert.Throws<RulesNotImplementedException>(bonus.Dispose);
            Assert.Contains("'Hit_Points_Reduced'", raised.Message, StringComparison.Ordinal);
        };

        _ = table.World.WindowAbilities.Waiting(table.World, Occurrence(), WindowKind.Response);
    }

    [Rule("rr:ability")]
    [Fact]
    public void ACardLeavingPlayInsideAReadTakesItsConstantsWithIt()
    {
        // "A constant ability ... remains active while the card is in play."
        var table = Table.Create();
        table.Abilities.WhileWaiting = world =>
        {
            Assert.Equal(1, Attack(world, table.Target));
            World.MoveToTop(table.Source, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
            Assert.Equal(0, Attack(world, table.Target));
        };

        _ = table.World.WindowAbilities.Waiting(table.World, Occurrence(), WindowKind.Response);
    }

    [Rule("rr:modifiers")]
    [Fact]
    public void ABlankRegisteredInsideAReadEndsTheBlankedCardsConstants()
    {
        var table = Table.Create();
        table.Abilities.WhileWaiting = world =>
        {
            Assert.Equal(1, Attack(world, table.Target));
            world.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect,
                PrintedText.Blanked,
                Card: table.Target.ObjectId,
                Affects: table.Source.ObjectId,
                Lasts: Duration.WhileInPlay)
            {
                TargetIncarnation = table.Source.Incarnation,
            });
            Assert.Equal(0, Attack(world, table.Target));
        };

        _ = table.World.WindowAbilities.Waiting(table.World, Occurrence(), WindowKind.Response);
    }

    private static long Attack(World world, Card card) =>
        StateFields.Modified(world, card, "attack", world.Facts, world.Players);

    private static long Thwart(World world, Card card) =>
        StateFields.Modified(world, card, "thwart", world.Facts, world.Players);

    private static long Field(StateDigest digest, Card card, string field) =>
        digest.Cards.Single(record => record.Id == card.ObjectId).Fields[field];

    private static Occurrence Occurrence() => new(0, Steps.TurnAction);

    private sealed record Table(World World, CountingConstants Abilities, Card Source, Card Target)
    {
        public static Table Create(bool dependent = false, bool sourceGrants = true)
        {
            var world = new World(new Facts(), players: 1);
            world.CreateSeat("p0");
            var allies = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0));
            var source = world.CreateCard("ally", allies);
            var target = world.CreateCard("ally", allies);
            var abilities = new CountingConstants(source.ObjectId, target.ObjectId, sourceGrants, dependent);
            world.Abilities = abilities;
            return new Table(world, abilities, source, target);
        }
    }

    /// <summary>
    /// The source grants the target +1 ATK while it is in play. A dependent
    /// target grants itself +1 THW and +1 hit point while its ATK is at least 1.
    /// </summary>
    private sealed class CountingConstants(int source, int target, bool sourceGrants, bool dependent)
        : NoCardAbilities
    {
        public int Calls { get; private set; }

        public Action<World>? WhileWaiting { get; set; }

        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
        {
            Calls++;
            if (card.ObjectId == source && sourceGrants)
            {
                return [Grant("attack", source)];
            }

            return card.ObjectId == target && dependent && Attack(world, card) >= 1
                ? [Grant("thwart", target), Grant("health", target)]
                : [];
        }

        public override IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window)
        {
            WhileWaiting?.Invoke(world);
            return [];
        }

        private ContinuousEffect Grant(string field, int from) => new(
            EffectSource.ConstantAbility, field, Amount: 1,
            Card: from, Affects: target, Lasts: Duration.WhileInPlay);
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId == "ally" ? CardKind.Ally : CardKind.Unknown;

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ATK"] = "0", ["THW"] = "0", ["HP"] = "1",
            };

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            attribute == "HP" ? 1 : fallback;
    }
}
