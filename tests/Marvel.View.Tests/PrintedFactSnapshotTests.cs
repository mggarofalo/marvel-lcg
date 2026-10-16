using Marvel.Rules.Events;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.View.Tests;

public sealed class PrintedFactSnapshotTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LiveAndOccurrenceFacesDoNotRetainProviderCollections(bool editExport)
    {
        // A provider may reuse its own storage. Projection owns the lifetime of
        // its published facts; this synthetic fixture checks that API contract.
        var facts = new MutableFacts();
        var world = new World(facts, 1, seed: 7);
        world.CreateSeat("Player");
        Card card = world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        var happened = new FieldSet(card.ObjectId, "health", 10, 9)
        {
            SubjectFaces = new Dictionary<int, string> { [card.ObjectId] = card.FaceId },
        };
        VisibleResult result = WorldProjection.For(world, null, [happened], ViewScope.None);
        CardFaceDescriptor live = VisibilityFixture.Card(result.World, card.ObjectId).Face!;
        BoardCardPresentation occurrence = result.World.EventFaces[card.FaceId];
        if (editExport)
        {
            ((IList<string>)live.Keywords)[0] = "Changed live keyword";
            ((IList<string>)occurrence.Traits)[0] = "Changed occurrence trait";
            ((IList<string>)occurrence.Keywords)[0] = "Changed occurrence keyword";
            Assert.Equal(["Guard"], facts.KeywordValues);
            Assert.Equal(["BRUTE"], facts.TraitValues);
            Assert.Equal(["BRUTE"], live.Traits);
        }
        else
        {
            facts.KeywordValues[0] = "Changed provider keyword";
            facts.TraitValues[0] = "Changed provider trait";
            Assert.Equal(["Guard"], live.Keywords);
            Assert.Equal(["Guard"], occurrence.Keywords);
            Assert.Equal(["BRUTE"], live.Traits);
            Assert.Equal(["BRUTE"], occurrence.Traits);
        }
    }

    private sealed class MutableFacts : ICardFacts
    {
        internal string[] TraitValues { get; } = ["BRUTE"];
        internal string[] KeywordValues { get; } = ["Guard"];
        public CardKind Kind(string faceId) => CardKind.EncounterVillain;
        public IReadOnlyList<string> Traits(string faceId) => TraitValues;
        public IReadOnlyList<string> Keywords(string faceId) => KeywordValues;
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
