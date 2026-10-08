using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class BoardLiveFieldTests
{
    [Rule("rr:loses")]
    [Rule("rr:loses.1")]
    [Theory]
    [InlineData("01101", DeckType.EngagedEnemiesArea, "guard", "GUARD", "Guard")]
    [InlineData("01107", DeckType.SideSchemesArea, "hazard", "HAZARD", "Hazard")]
    public void SuppressedLiveCharacteristicRemainsAnExplicitZeroUntilTheEngineRestoresIt(
        string face, DeckType zone, string field, string label, string attribute)
    {
        // "the card functions as if it does not possess" the characteristic;
        // "Lost characteristics are still considered to be printed on the card."
        // A synthetic lasting loss exercises the owning rule on real Core faces;
        // it does not claim a new Core card ability that creates the loss.
        World world = PersistentFixture.Board();
        Card target = world.CreateCard(face, world.AreaOf(zone,
            zone == DeckType.EngagedEnemiesArea ? PlayArea.Of(0) : PlayArea.Villains));
        Assert.Equal("1", LiveField(world, target, label));
        world.Effects.Register(new(EffectSource.LastingEffect, Characteristics.LossOf(field),
            Card: world.Seats[0].IdentityCard.ObjectId, Affects: target.ObjectId,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound)));
        string digest = world.Digest().Canonical();
        long randomWords = world.Random.Generator.WordsConsumed;

        Assert.Equal(0, StateFields.Modified(world, target, field, world.Facts, world.Players));
        Assert.Equal(0, PersistentFixture.Card(world, target).Face!.Fields[field]);
        Assert.Equal("0", LiveField(world, target, label));
        Assert.Equal(1, world.Facts.PrintedValue(face, attribute, world.Players));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(randomWords, world.Random.Generator.WordsConsumed);

        world.Effects.Expire(TimingPoints.EndOfRound);
        Assert.Equal("1", LiveField(world, target, label));
    }

    [Theory]
    [InlineData("acceleration_icon", "ACCELERATION_ICON")]
    [InlineData("amplify", "AMPLIFY")]
    [InlineData("crisis", "CRISIS")]
    [InlineData("hazard", "HAZARD")]
    [InlineData("guard", "GUARD")]
    [InlineData("patrol", "PATROL")]
    [InlineData("steady", "STEADY")]
    [InlineData("stalwart", "STALWART")]
    [InlineData("engaged_with", "ENGAGED_WITH")]
    public void EverySuppliedLiveZeroSurvivesWithoutAKeywordWhitelist(string field, string label)
    {
        // Synthetic descriptor tests the generic supplied-value boundary;
        // zero is meaningful for both suppression and seat-zero quantities.
        BoardCardPresentation card = Present(new Dictionary<string, long> { [field] = 0 }, "EngagedEnemiesArea");
        Assert.Equal(new BoardFieldPresentation(label, "0"), Assert.Single(card.Fields));
    }

    [Fact]
    public void AbsentLiveFieldIsNotInventedFromPrintedKeywordsOrIcons()
    {
        BoardCardPresentation absent = Present(new Dictionary<string, long>(), "EngagedEnemiesArea");
        BoardCardPresentation suppressed = Present(new Dictionary<string, long> { ["guard"] = 0, ["hazard"] = 0 }, "EngagedEnemiesArea");
        Assert.Contains("Guard", absent.Keywords);
        Assert.Contains(new BoardFieldPresentation("Hazard", "1"), absent.PrintedStats);
        Assert.Empty(absent.Fields);
        Assert.Equal([new("GUARD", "0"), new BoardFieldPresentation("HAZARD", "0")], suppressed.Fields);
    }

    [Fact]
    public void OutOfPlayFieldsKeepTheirExistingZeroOmissionAndPrintedFacts()
    {
        BoardCardPresentation card = Present(new Dictionary<string, long> { ["guard"] = 0, ["hazard"] = 1 }, "EncounterDiscardPile");
        Assert.Equal(new BoardFieldPresentation("HAZARD", "1"), Assert.Single(card.Fields));
        Assert.Contains("Guard", card.Keywords);
    }

    [Fact]
    public void TraitExhaustionAndCharacterThreatBookkeepingAreNotNumericBoardFields()
    {
        BoardCardPresentation card = Present(new Dictionary<string, long>
        {
            ["t_BRUTE"] = 1, ["is_exhaust"] = 1, ["k_threat"] = 2, ["guard"] = 0,
        }, "EngagedEnemiesArea");
        Assert.Equal(new BoardFieldPresentation("GUARD", "0"), Assert.Single(card.Fields));
    }

    private static string LiveField(World world, Card card, string field) => Assert.Single(
        Assert.Single(BoardPresentation.From(PersistentFixture.Visible(world)).Areas.SelectMany(area => area.Cards),
            candidate => candidate.TargetId == card.ObjectId).Fields, value => value.Name == field).Value;

    private static BoardCardPresentation Present(IReadOnlyDictionary<string, long> fields, string zone)
    {
        var face = new CardFaceDescriptor("synthetic", "Synthetic", "", CardKind.Minion, fields)
        {
            Keywords = ["Guard"], PrintedStats = new Dictionary<string, string> { ["Hazard"] = "1" },
        };
        return Assert.Single(Assert.Single(BoardPresentation.From(new WorldDescriptor([], [new AreaDescriptor(
            1, zone, 0, -1, [new CardDescriptor(1, CardBack.Encounter, true, true, -1, face)], [])], [], Outcome.Unfinished)).Areas).Cards);
    }
}
