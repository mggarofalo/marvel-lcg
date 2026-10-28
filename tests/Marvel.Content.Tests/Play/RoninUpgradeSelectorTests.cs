using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RoninUpgradeSelectorTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly AbilityCardSelection Selector = new AbilityCardSelection.Query(AbilityCardQuery.UpgradesOnThis);

    [Fact]
    [Rule("rr:upgrade.3")]
    [Rule("rr:attachment")]
    [Rule("rr:in-play-and-out-of-play.10")]
    public void SelectsOnlyInPlayEffectiveUpgradesOnTheCurrentHost()
    {
        // rr:upgrade.3: "Some upgrades enter play and ‘attach to’ another card."
        // rr:attachment: "Attachment is an encounter card type."
        // rr:in-play-and-out-of-play.10: facedown cards attached to in-play cards are out of play.
        var world = Board();
        var ronin = Ally(world, "12013");
        var other = Ally(world, "01067");
        var attached = world.CreateCard("01074", Hosted(world, ronin));
        world.CreateCard("04117", Hosted(world, ronin));
        world.CreateCard("01067", Hosted(world, ronin));
        world.CreateCard("01074", Hosted(world, other));
        var stored = world.CreateCard("01074", world.Seats[0].Hand);
        BeneathCards.Place(world, stored, ronin, false, "test", []);
        var detached = world.CreateCard("01074", world.Seats[0].Hand);
        var context = Context(world, ronin);
        var admission = Admission(context);
        var projection = new AreaProjectionState(admission);
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Equal([attached], Live(context));
        Assert.Equal([attached], Projected(projection, admission));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        projection.Hosts[attached.ObjectId] = other.ObjectId;
        projection.Hosts[detached.ObjectId] = ronin.ObjectId;
        projection.Entered.Add(detached.ObjectId);
        Assert.Equal([detached], Projected(projection, admission));
        projection.Departed.Add(detached.ObjectId);
        Assert.Empty(Projected(projection, admission));
        projection.SourceReferenceCurrent = false;
        projection.Hosts[attached.ObjectId] = ronin.ObjectId;
        Assert.Empty(Projected(projection, admission));
        attached.AssignProfile(new EffectiveCardProfile("synthetic", "Synthetic", CardKind.Minion,
            [], ImmutableDictionary<string, long>.Empty));
        Assert.Empty(Live(context));
        Assert.Empty(Projected(new AreaProjectionState(admission), admission));
        World.MoveToTop(ronin, world.Seats[0].Hand);
        World.MoveToTop(ronin, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        Assert.Empty(Live(context));
        Assert.Empty(Projected(new AreaProjectionState(admission), admission));
    }

    [Fact]
    [Rule("rr:attach-to.1")]
    public void RoninBonusTracksAnAttachedUpgradeAndPrintedTextBlanking()
    {
        // rr:attach-to.1: an attached card "remains in play" until its host or it leaves play.
        // Printed 12013: "Ronin gets +1 THW and +1 ATK while an upgrade is attached to them."
        var world = Board();
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"12013","abilities":[{
              "trigger":{"timing":"Constant","subject":"this"},
              "effect":{"if":{"test":{"exists":{"query":"upgradesOnThis"}},"then":{"seq":[
                {"grant":{"card":"this","keyword":"thwart","amount":1}},
                {"grant":{"card":"this","keyword":"attack","amount":1}}
              ]}}}
            }]},{"card":"01074","attachTo":{"withoutAnotherCopyAttached":{"query":"allies"}},"abilities":[{
              "trigger":{"timing":"Constant","subject":"this"},
              "effect":{"seq":[
                {"grant":{"card":"attachedTo","keyword":"thwart","amount":1}},
                {"grant":{"card":"attachedTo","keyword":"attack","amount":1}}
              ]}
            }]}]}
            """));
        var ronin = Ally(world, "12013");
        var other = Ally(world, "01067");
        var upgrade = world.CreateCard("01074", Hosted(world, other));
        long attack = Value(world, ronin, "attack"), thwart = Value(world, ronin, "thwart");
        World.MoveToTop(upgrade, Hosted(world, ronin));
        Assert.Equal(attack + 2, Value(world, ronin, "attack"));
        Assert.Equal(thwart + 2, Value(world, ronin, "thwart"));
        Blank(world, upgrade);
        Assert.Equal(attack + 1, Value(world, ronin, "attack"));
        Assert.Equal(thwart + 1, Value(world, ronin, "thwart"));
        World.MoveToTop(upgrade, Hosted(world, other));
        Assert.Equal(attack, Value(world, ronin, "attack"));
        Assert.Equal(thwart, Value(world, ronin, "thwart"));
        World.MoveToTop(upgrade, Hosted(world, ronin));
        Blank(world, ronin);
        Assert.Equal(attack, Value(world, ronin, "attack"));
        Assert.Equal(thwart, Value(world, ronin, "thwart"));
    }

    private static void Blank(World world, Card card) => world.Effects.Register(
        new ContinuousEffect(EffectSource.LastingEffect, PrintedText.Blanked, Card: card.ObjectId,
            Affects: card.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfPhase))
        { TargetIncarnation = card.Incarnation });
    private static long Value(World world, Card card, string field) =>
        StateFields.Modified(world, card, field, world.Facts, world.Players);
    private static IReadOnlyList<Card> Live(AbilityQueryContext context) => new AbilitySelectorEvaluation(context).Every(Selector);
    private static List<Card> Projected(AreaProjectionState state, AbilityAdmissionContext context) =>
        AbilityProjectedStateQueries.ProjectedEvery(Selector, state, context);
    private static AbilityAdmissionContext Admission(AbilityQueryContext context) => new(
        AbilityLowering.Book(AbilityCatalog.Parse("""{"cards":[]}""")), context.World.ResourceAbilities,
        new AbilityExpressionContext(context, ImmutableDictionary<string, long>.Empty, [], "", 0, false, null), new(), null);
    private static AbilityQueryContext Context(World world, Card source) =>
        new(world, source, new Occurrence(1, []), 0, source.Incarnation, null, null, null, []);
    private static Card Ally(World world, string id) => world.CreateCard(id, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
    private static Area Hosted(World world, Card host) => world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host.ObjectId);
    private static World Board()
    {
        var world = new World(Cards, 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        return world;
    }
}
