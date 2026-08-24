using Marvel.Tests;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Xunit;

namespace Marvel.Rules.Tests.Events;

/// <summary>
/// The event vocabulary matches the one measured against the corpus.
/// </summary>
/// <remarks>
/// <para>
/// <c>datasets/events/vocabulary.json</c> is written by the Python side from
/// <c>tools/events/model.py</c> — the model whose reducer was checked against
/// 27,895 recorded steps drawn from all 58 corpus shards. These tests hold the
/// C# hierarchy to it.
/// </para>
/// <para>
/// Two implementations agreeing on the set of event kinds but not on the
/// spelling of their payload keys is a contract that passes its own tests and
/// fails in the field, so both are checked.
/// </para>
/// </remarks>
public sealed class EventVocabularyTests
{
    private static readonly JsonElement Fixture = Load();

    private static JsonElement Load()
    {
        using var stream = File.OpenRead(RepositoryPaths.Dataset("events", "vocabulary.json"));
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.Clone();
    }

    private static Dictionary<string, Type> Declared() =>
        typeof(GameEvent)
            .GetCustomAttributes<JsonDerivedTypeAttribute>()
            .ToDictionary(a => (string)a.TypeDiscriminator!, a => a.DerivedType,
                          StringComparer.Ordinal);

    [Fact]
    public void TheKindsAreExactlyTheMeasuredOnes()
    {
        var expected = Fixture.GetProperty("kinds").EnumerateObject()
            .Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal);
        var actual = Declared().Keys.OrderBy(n => n, StringComparer.Ordinal);

        // Not a subset check in either direction. A kind here that the corpus
        // never produced is speculation; one the corpus produced and this
        // lacks is a hole the interpreter will fall into.
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EveryKindSpellsItsPayloadKeysTheSameWay()
    {
        var universal = Fixture.GetProperty("universal").EnumerateArray()
            .Select(e => e.GetString()!).ToHashSet(StringComparer.Ordinal);

        foreach (var (kind, type) in Declared())
        {
            var expected = Fixture.GetProperty("kinds").GetProperty(kind)
                .EnumerateArray().Select(e => e.GetString()!)
                .OrderBy(n => n, StringComparer.Ordinal);

            string json = EventJson.Write(Sample(type));
            using var document = JsonDocument.Parse(json);
            var actual = document.RootElement.EnumerateObject()
                .Select(p => p.Name)
                .Where(name => !universal.Contains(name))
                .OrderBy(n => n, StringComparer.Ordinal);

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void TheDiscriminatorIsCalledKind()
    {
        string json = EventJson.Write(new CardAttached(1, 2));
        Assert.StartsWith("{\"kind\":\"CardAttached\"", json, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(EveryKind))]
    public void EveryKindRoundTrips(GameEvent original)
    {
        // Read back as the base type, so the discriminator has to do its job.
        GameEvent restored = EventJson.Read(EventJson.Write(original));
        Assert.Equal(original.GetType(), restored.GetType());
        Assert.Equal(EventJson.Write(original), EventJson.Write(restored));
    }

    [Fact]
    public void CauseTravelsWithTheEvent()
    {
        var original = new CardsMoved(
            AreaRef.Player("PlayerDeck", 0),
            AreaRef.Player("HandsArea", 0),
            [new Landing(7, 0)])
        {
            Trigger = "WhenPlayerInTurn",
            Verb = "Play",
        };

        var restored = Assert.IsType<CardsMoved>(EventJson.Read(EventJson.Write(original)));
        Assert.Equal("WhenPlayerInTurn", restored.Trigger);
        Assert.Equal("Play", restored.Verb);
    }

    [Fact]
    public void AnAbsentFieldIsNotZero()
    {
        // A field that is gone means the card no longer registers it at all,
        // which is how a granted trait expires. Serialising that as 0 would say
        // the card still has the trait, at zero.
        var expired = new FieldSet(1, "t_AVENGER", From: 1, To: null);
        var zeroed = new FieldSet(1, "attack", From: 2, To: 0);

        Assert.Null(Assert.IsType<FieldSet>(EventJson.Read(EventJson.Write(expired))).To);
        Assert.Equal(0, Assert.IsType<FieldSet>(EventJson.Read(EventJson.Write(zeroed))).To);
    }

    [Fact]
    public void TwoAreasSharingATripleAreNotTheSameArea()
    {
        // MARVEL-163 replayed the corpus against engine state and counted:
        // `AsideDeck` names one area per player — a set-aside nemesis deck
        // each, all owned by the scenario, none hanging off a card — and did
        // so on 5,969 of 6,554 sampled steps. The triple is a description;
        // `Id` is the address.
        var first = AreaRef.Scenario("AsideDeck", "20");
        var second = AreaRef.Scenario("AsideDeck", "35");

        Assert.NotEqual(first, second);
        Assert.Equal(first, AreaRef.Scenario("AsideDeck", "20"));
    }

    [Fact]
    public void AnAreaDescribedFromADigestSaysSo()
    {
        // A consumer rebuilding areas out of digests cannot know an identity,
        // so it leaves one empty rather than inventing one. `IsIdentified` is
        // how a reader tells "this addresses an area" from "this describes the
        // best guess a digest supports".
        Assert.False(AreaRef.Scenario("AsideDeck").IsIdentified);
        Assert.True(AreaRef.Scenario("AsideDeck", "20").IsIdentified);
    }

    [Fact]
    public void TheAreasIdentityIsOnTheWire()
    {
        var original = new AreaReordered(AreaRef.Player("PlayerDeck", 1, "22"), [4, 2, 9]);

        var restored = Assert.IsType<AreaReordered>(EventJson.Read(EventJson.Write(original)));
        Assert.Equal("22", restored.Area.Id);
    }

    public static TheoryData<GameEvent> EveryKind() => [.. Samples];

    private static GameEvent Sample(Type type) =>
        Samples.Single(sample => sample.GetType() == type);

    private static readonly GameEvent[] Samples =
    [
        new CardsCreated(AreaRef.Scenario("EncounterDeck"), [new CreatedCard(3, "01096")]),
        new CardsMoved(AreaRef.Player("PlayerDeck", 0), AreaRef.Player("HandsArea", 0),
                       [new Landing(7, 0), new Landing(8, 1)]),
        new AreaReordered(AreaRef.Player("PlayerDeck", 1), [4, 2, 9]),
        new CardFormChanged(1, "01001a", "01001b"),
        new CardsFlipped([5, 6], FaceUp: false),
        new CardAttached(2, 9),
        new CardDetached(2, 9),
        new ControlChanged(2, 0, 1),
        new FieldSet(2, "health", 10, 8),
    ];
}
