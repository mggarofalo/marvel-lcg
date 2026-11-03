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
            Assert.Equal(1, Attack(world, table.Target));
            using (world.Effects.PreflightConstantsEnding(table.Source).Begin())
            {
                Assert.Equal(0, Attack(world, table.Target));
            }

            Assert.Equal(1, Attack(world, table.Target));
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

    private static long Field(StateDigest digest, Card card, string field) =>
        digest.Cards.Single(record => record.Id == card.ObjectId).Fields[field];

    private static Occurrence Occurrence() => new(0, Steps.TurnAction);

    private sealed record Table(World World, CountingConstants Abilities, Card Source, Card Target)
    {
        public static Table Create()
        {
            var world = new World(new Facts(), players: 1);
            world.CreateSeat("p0");
            var allies = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0));
            var source = world.CreateCard("ally", allies);
            var target = world.CreateCard("ally", allies);
            var abilities = new CountingConstants(source.ObjectId, target.ObjectId);
            world.Abilities = abilities;
            return new Table(world, abilities, source, target);
        }
    }

    /// <summary>The source grants the target +1 ATK while it is in play.</summary>
    private sealed class CountingConstants(int source, int target) : NoCardAbilities
    {
        public int Calls { get; private set; }

        public Action<World>? WhileWaiting { get; set; }

        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
        {
            Calls++;
            return card.ObjectId == source
                ? [new ContinuousEffect(
                    EffectSource.ConstantAbility,
                    "attack",
                    Amount: 1,
                    Card: source,
                    Affects: target,
                    Lasts: Duration.WhileInPlay)]
                : [];
        }

        public override IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window)
        {
            WhileWaiting?.Invoke(world);
            return [];
        }
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId == "ally" ? CardKind.Ally : CardKind.Unknown;

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>(StringComparer.Ordinal) { ["ATK"] = "0" };

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            fallback;
    }
}
