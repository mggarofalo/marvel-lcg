using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.View.Tests;

public sealed class AttackCompletionPresentationTests : EventPresentationTestBase
{
    [Theory]
    [InlineData(7, "Spider-Man defended.")]
    [InlineData(-1, "The attack was undefended.")]
    public void CompletionUsesOccurrenceNamesAndDoesNotInventDamage(int defender, string result)
    {
        var completed = new AttackCompleted(9, 7, defender)
        {
            Subjects = new Dictionary<int, string> { [9] = "Rhino", [7] = "Spider-Man" },
        };
        EventPresentation presented = Assert.Single(EventPresenter.Present([completed], World()));

        Assert.Equal($"Rhino's attack on Spider-Man ended. {result}", presented.Summary);
        Assert.Equal(EventMotionKind.Attack, presented.Motion);
        Assert.Equal([9, 7], presented.Anchors);
        Assert.DoesNotContain("damage", presented.Summary);
    }

    [Fact]
    public void AttackCompletionSurvivesLaterThreatHighlights()
    {
        GameEvent[] events = [new AttackCompleted(9, 7, 7),
            .. Enumerable.Range(0, 5).Select(index => new FieldSet(9, "k_threat", index, index + 1))];
        EventBatchPresentation batch = EventCuePlanner.Plan(events, World(), Outcome.Unfinished);

        Assert.Contains(batch.Highlights, cue => cue.Motion == EventMotionKind.Attack);
        Assert.Equal(EventMotionKind.Attack, batch.Highlights[0].Motion);
        Assert.Contains(batch.Highlights, cue => cue.Summary.Contains("attack", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void CompletionNamesRecordedActualDamageEvenWithoutAHealthEvent(long damage)
    {
        var completed = new AttackCompleted(9, 7, 7)
        {
            DamageDealt = damage,
            Subjects = new Dictionary<int, string> { [9] = "Rhino", [7] = "Spider-Man" },
        };
        EventPresentation presented = Assert.Single(EventPresenter.Present([completed], World()));

        Assert.Contains($"The attack dealt {damage} damage.", presented.Summary);
        Assert.Contains("Spider-Man defended.", presented.Summary);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void UnreadableParticipantSuppressesTheEntireCompletion(int hiddenRole)
    {
        var facts = new VisibilityFacts();
        var world = new World(facts, players: 1);
        var seat = world.CreateSeat("player");
        Card hero = world.CreateCard("hero", seat.Hero);
        seat.IdentityCard = hero;
        Card enemy = world.CreateCard("enemy", world.AreaOf(DeckType.VillainArea));
        Card ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Card secret = world.CreateCard("secret", seat.Deck);
        int[] roles = [enemy.ObjectId, hero.ObjectId, ally.ObjectId];
        roles[hiddenRole] = secret.ObjectId;
        var completed = new AttackCompleted(roles[0], roles[1], roles[2])
        {
            Subjects = new Dictionary<int, string> { [secret.ObjectId] = "secret identity" },
        };

        VisibleResult visible = WorldProjection.For(world, null, [completed],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players));

        Assert.Empty(visible.Events);
    }

    [Fact]
    public void ReadableCompletionRetainsParticipantsAndStripsUnrelatedSubjects()
    {
        var world = new World(new VisibilityFacts(), players: 1);
        var seat = world.CreateSeat("player");
        Card hero = world.CreateCard("hero", seat.Hero);
        seat.IdentityCard = hero;
        Card enemy = world.CreateCard("enemy", world.AreaOf(DeckType.VillainArea));
        Card secret = world.CreateCard("secret", seat.Deck);
        var completed = new AttackCompleted(enemy.ObjectId, hero.ObjectId, -1)
        {
            Subjects = new Dictionary<int, string>
            {
                [enemy.ObjectId] = "Rhino", [hero.ObjectId] = "Spider-Man",
                [secret.ObjectId] = "private card",
            },
        };

        VisibleResult visible = WorldProjection.For(world, null, [completed],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players));

        AttackCompleted safe = Assert.IsType<AttackCompleted>(Assert.Single(visible.Events));
        Assert.Equal(completed.Enemy, safe.Enemy);
        Assert.Equal(completed.Target, safe.Target);
        Assert.Equal(-1, safe.Defender);
        Assert.Equal(2, safe.Subjects!.Count);
        Assert.Equal("Rhino", safe.Subjects[enemy.ObjectId]);
        Assert.Equal("Spider-Man", safe.Subjects[hero.ObjectId]);
        Assert.DoesNotContain(secret.ObjectId, safe.Subjects.Keys);
    }

    private sealed class VisibilityFacts : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "hero" => CardKind.Hero,
            "enemy" => CardKind.EncounterVillain,
            "ally" => CardKind.Ally,
            _ => CardKind.Event,
        };
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public IReadOnlyList<string> Traits(string faceId) => [];
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
