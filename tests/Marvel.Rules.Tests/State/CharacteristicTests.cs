using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.State;

public sealed class CharacteristicTests
{
    [Rule("rr:gains")]
    [Fact]
    public void GainedTraitsAndKeywordsFunctionAsPossessedButAreNotPrinted()
    {
        // A card that gains a characteristic "functions as if it possesses"
        // it, but "gained characteristics are not considered to be printed."
        // The facts remain the printed authority while runtime queries see the
        // continuous effects.
        var (world, facts, card) = Board();
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, Traits.Granted + "AERIAL", Affects: card.ObjectId));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, Keywords.Overkill, Amount: 1, Affects: card.ObjectId));

        Assert.True(Traits.Has(world, card, "AERIAL", facts));
        Assert.True(Keywords.Has(world, card, Keywords.Overkill, facts));
        Assert.DoesNotContain("AERIAL", facts.Traits(card.FaceId));
        Assert.DoesNotContain("Overkill", facts.Attributes(card.FaceId).Keys);
    }

    [Rule("rr:loses")]
    [Rule("rr:loses.1")]
    [Fact]
    public void LostPrintedCharacteristicsRemainPrintedButDoNotFunction()
    {
        // A card "functions as if it does not possess" a lost characteristic,
        // while that characteristic "is still considered to be printed."
        var (world, facts, card) = Board();
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf(Traits.Granted + "AVENGER"),
            Affects: card.ObjectId));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf(Keywords.Ranged),
            Affects: card.ObjectId));

        Assert.False(Traits.Has(world, card, "AVENGER", facts));
        Assert.False(Keywords.Has(world, card, Keywords.Ranged, facts));
        Assert.Contains("AVENGER", facts.Traits(card.FaceId));
        Assert.Contains("Ranged", facts.Attributes(card.FaceId).Keys);
    }

    [Rule("rr:loses")]
    [Rule("rr:loses.1")]
    [Fact]
    public void AStateSnapshotReportsALostPrintedFieldAsInactive()
    {
        // The characteristic remains in printed facts, while the live state
        // sent to a client reports how the card currently functions.
        var (world, facts, card) = Board();
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("toughness"),
            Affects: card.ObjectId));

        var fields = StateFields.For(
            card, facts, players: 1, inPlay: true, hasHeldPools: true,
            hasFirstPlayerToken: false, world);

        Assert.Equal(0, fields["toughness"]);
        Assert.Equal(1, facts.PrintedValue(card.FaceId, "Toughness", 1));
    }

    [Rule("rr:loses.2")]
    [Fact]
    public void ACharacteristicCannotBeRegainedWhileItsLossIsActive()
    {
        // "A lost characteristic cannot be regained while the ability causing
        // it to be lost is in effect, even if a new effect would cause the
        // characteristic to be gained." Registration order therefore cannot
        // let a later grant win.
        var (world, facts, card) = Board();
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf(Traits.Granted + "AERIAL"),
            Affects: card.ObjectId));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Traits.Granted + "AERIAL",
            Affects: card.ObjectId));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf(Keywords.Ranged),
            Affects: card.ObjectId));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Keywords.Ranged,
            Amount: 1,
            Affects: card.ObjectId));

        Assert.False(Traits.Has(world, card, "AERIAL", facts));
        Assert.False(Keywords.Has(world, card, Keywords.Ranged, facts));
    }

    private static (World World, Facts Facts, Card Card) Board()
    {
        var facts = new Facts();
        var world = new World(facts, 1);
        world.CreateSeat("p0");
        var card = world.CreateCard("hero", world.Seats[0].Hero);
        world.Seats[0].IdentityCard = card;
        return (world, facts, card);
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => CardKind.Hero;

        public IReadOnlyList<string> Traits(string faceId) => ["AVENGER"];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ranged"] = "1",
                ["Toughness"] = "1",
            };

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? value)
            && long.TryParse(value, out long number)
                ? number
                : fallback;

        public string Title(string faceId) => faceId;
    }
}
