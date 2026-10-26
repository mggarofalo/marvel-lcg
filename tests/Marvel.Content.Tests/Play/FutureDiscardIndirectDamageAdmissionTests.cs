using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic response shapes isolate admission without admitting Power Drain content.</summary>
public sealed class FutureDiscardIndirectDamageAdmissionTests
{
    private const string Discard = """{"discardTop":{"from":"encounterDeck","count":1}}""";
    private const string Damage = """{"indirectDamage":{"among":"you","amount":{"printedBoostIconsDiscarded":"game"}}}""";

    [Theory]
    [InlineData("01121", 0)]
    [InlineData("02042", 2)]
    [InlineData("02041", 3)]
    [Rule("rr:target.3")]
    public void EarlierDiscardAdmitsUnknownIndirectDamageAndResolvesTheActualReceipt(string face, int expected)
    {
        // "A target is valid ... if any part of that ability can affect that target."
        // Admission uses the preceding discard instruction, not the hidden top card's icons.
        var runner = Response($$"""{"seq":[{{Discard}},{{Damage}}]}""");
        var (world, source, occurrence) = Board(runner, face);
        string digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Current;
        int count = world.Agenda.Count;
        var offered = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Response));
        Assert.Equal(source.ObjectId, offered.Card);
        Assert.Single(runner.Waiting(world, occurrence, WindowKind.Response));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Current);
        Assert.Equal(count, world.Agenda.Count);

        runner.ResolveWindow(world, occurrence, offered, Decision.Take(offered.Card));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(expected, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Single(world.AreaOf(DeckType.EncounterDeck).Cards);
        Assert.Equal(face, Assert.Single(world.AreaOf(DeckType.EncounterDiscardPile).Cards).FaceId);
        var (control, controlSource, controlOccurrence) = Board(runner, face);
        runner.ResolveWindow(control, controlOccurrence,
            new PendingAbility(controlSource.ObjectId, AbilityType.ForcedResponse, 1),
            Decision.Take(controlSource.ObjectId));
        Sequence.Finish(control, control.Facts, runner, []);
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(control.Agenda.Count, world.Agenda.Count);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("zero")]
    [InlineData("later")]
    [InlineData("noRecipients")]
    [Rule("rr:target.2")]
    public void ReceiptWithoutAnEarlierPositiveProducerDoesNotCreateATarget(string shape)
    {
        // "... can only be initiated if it has at least one valid target."
        string effect = shape switch
        {
            "missing" => Damage,
            "zero" => $$"""{"seq":[{{Discard.Replace("1", "0", StringComparison.Ordinal)}},{{Damage}}]}""",
            "later" => $$"""{"seq":[{{Damage}},{{Discard}}]}""",
            _ => $$"""{"seq":[{{Discard}},{{Damage.Replace("\"you\"", "{\"query\":\"alliesYouControl\"}", StringComparison.Ordinal)}}]}""",
        };
        var runner = Response(effect);
        var (world, source, occurrence) = Board(runner, "02041");
        string digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Current;
        int count = world.Agenda.Count;
        Assert.Empty(runner.Waiting(world, occurrence, WindowKind.Response));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Current);
        Assert.Equal(count, world.Agenda.Count);
        Assert.True(source.Ready);
        Assert.Equal(2, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
    }

    [Fact]
    public void UnsafeSuffixIsRejectedBeforeDiscardOrDamageCommits()
    {
        // Engine boundary: a mutable repeated count after a prior effect is unsupported.
        const string suffix = """{"forEach":{"count":{"damageOn":"you"},"effect":{"exhaust":"you"}}}""";
        var runner = Response($$"""{"seq":[{{Discard}},{{Damage}},{{suffix}}]}""");
        var (world, source, occurrence) = Board(runner, "02041");
        string digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Current;
        int count = world.Agenda.Count;
        var error = Assert.Throws<RulesNotImplementedException>(() => runner.ResolveWindow(
            world, occurrence, new PendingAbility(source.ObjectId, AbilityType.ForcedResponse, 1),
            Decision.Take(source.ObjectId)));
        Assert.Contains("for-each count after state may change", error.Message, StringComparison.Ordinal);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Current);
        Assert.Equal(count, world.Agenda.Count);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.Empty(world.AreaOf(DeckType.EncounterDiscardPile).Cards);
    }
    private static AbilityRunner Response(string effect) => new(AbilityCatalog.Parse($$"""
        {"cards":[{"card":"02042","abilities":[{
          "trigger":{"event":"WhenAttackEnds","timing":"ForcedResponse","actor":"this","player":"trigger.player"},
          "effect":{{effect}}}]}]}
        """));

    private static (World World, Card Source, Occurrence Occurrence) Board(AbilityRunner runner, string face)
    {
        var world = new World(CardCatalogData, players: 2) { Abilities = runner };
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
            world.CreateCard("01003", seat.Deck);
        }
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01107", world.AreaOf(DeckType.MainSchemesArea));
        var source = world.CreateCard("02042", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        world.CreateCard("01121", deck);
        world.CreateCard(face, deck);
        return (world, source, Occurrence.ForAttack(99, [Steps.AttackEnds], world, world.Facts,
            source.ObjectId, world.Seats[1].IdentityCard.ObjectId, 1));
    }
}
