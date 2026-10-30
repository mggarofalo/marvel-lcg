using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic DSL fixtures distinguish search entry instructions using printed research cards.</summary>
public sealed class EncounterDiscardInstructionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:encounter-deck.2")]
    [Theory]
    [InlineData("12026")]
    [InlineData("12027")]
    [InlineData("12028")]
    [InlineData("12029")]
    public void SetMatchRevealsTheExactPhysicalCardRegardlessOfType(string face)
    {
        // Discard until the criterion is met or the encounter deck is empty.
        var world = new World(Cards, players: 1);
        world.CreateSeat("player");
        var source = world.CreateCard("12029", world.AreaOf(DeckType.RevealingArea));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var below = world.CreateCard("01104", deck);
        var match = world.CreateCard(face, deck);
        // Shocker is also a CRIMINAL minion, but is from Rhino's set.
        var skipped = world.CreateCard("01103", deck);
        var duplicate = world.CreateCard(face, world.AreaOf(DeckType.EncounterDiscardPile));
        var result = AbilityDiscardUntilExecution.Run(LowerSet(), Context(world, source));
        Assert.Equal(match.ObjectId, result.Reveal!.Card);
        Assert.Equal(DeckType.RevealingArea, match.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, skipped.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, duplicate.Area.Type);
        Assert.Equal([below], deck.Cards);
    }

    [Rule("rr:encounter-deck.1")]
    [Rule("rr:encounter-deck.2")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetMatchAtDeckBoundaryResetsOnceAndRevealsOnlyIfFound(bool matchLast)
    {
        // The discard stops at empty; the discard pile is immediately shuffled.
        var world = new World(Cards, players: 1);
        world.CreateSeat("player");
        world.CreateCard("01117a", world.AreaOf(DeckType.MainSchemesArea));
        var source = world.CreateCard("12029", world.AreaOf(DeckType.RevealingArea));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var last = world.CreateCard(matchLast ? "12027" : "01104", deck);
        var first = world.CreateCard("01098", deck);
        var context = Context(world, source);
        var result = AbilityDiscardUntilExecution.Run(LowerSet(), context);
        Assert.Equal(matchLast ? last.ObjectId : (int?)null, result.Reveal?.Card);
        Assert.Equal(matchLast ? DeckType.RevealingArea : DeckType.EncounterDeck, last.Area.Type);
        Assert.Equal(DeckType.EncounterDeck, first.Area.Type);
        Assert.Single(context.Events, item => item.Verb == "Reset");
        Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens[EncounterDeck.AccelerationToken]);
    }

    [Rule("rr:encounter-deck.1")]
    [Fact]
    public void EmptyDeckResetsBeforeSearchingForSet()
    {
        // If the deck is empty, its discard pile is immediately shuffled to make a new deck.
        var world = new World(Cards, players: 1);
        world.CreateSeat("player");
        world.CreateCard("01117a", world.AreaOf(DeckType.MainSchemesArea));
        var source = world.CreateCard("12029", world.AreaOf(DeckType.RevealingArea));
        var match = world.CreateCard("12027", world.AreaOf(DeckType.EncounterDiscardPile));
        var context = Context(world, source);
        var result = AbilityDiscardUntilExecution.Run(LowerSet(), context);
        Assert.Equal(match.ObjectId, result.Reveal!.Card);
        Assert.Equal(DeckType.RevealingArea, match.Area.Type);
        Assert.Equal(2, context.Events.Count(item => item.Verb == "Reset"));
        Assert.Equal(2, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens[EncounterDeck.AccelerationToken]);
    }

    [Theory]
    [InlineData("\"set\":\"ant_nemesis\",\"kind\":\"minion\",\"then\":\"reveal\"")]
    [InlineData("\"set\":\"ant_nemesis\",\"trait\":\"CRIMINAL\",\"then\":\"reveal\"")]
    [InlineData("\"set\":\"ant_nemesis\",\"then\":\"putIntoPlay\"")]
    [InlineData("\"set\":\"\",\"then\":\"reveal\"")]
    [InlineData("\"trait\":\"CRIMINAL\",\"then\":\"reveal\"")]
    public void RejectsUnsupportedMatchShapes(string fields)
    {
        Assert.Throws<AbilityException>(() => LowerFields(fields));
    }

    private static AbilityEffect.DiscardUntil LowerSet() => LowerFields("\"set\":\"ant_nemesis\",\"then\":\"reveal\"");

    private static AbilityEffect.DiscardUntil LowerFields(string fields)
    {
        var effect = AbilityCatalog.Parse("{\"cards\":[{\"card\":\"12029\",\"abilities\":[{\"trigger\":{\"event\":\"WhenCardRevealed\",\"timing\":\"WhenRevealed\",\"subject\":\"this\"},\"effect\":{\"discardUntil\":{\"from\":\"encounterDeck\"," + fields + "}}}]}]}").Abilities[0].Effect;
        return Assert.IsType<AbilityEffect.DiscardUntil>(AbilityLowering.Effect(
            new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
            { [effect.Kind] = effect.Argument }), new AbilityLocation("12029", 0, "effect")));
    }

    [Rule("rr:attach-to.3.1")]
    [Fact]
    public void TechSearchAttachesToSourceInsteadOfPrintedHighestHealthHost()
    {
        // "The attach to phrase ... is not resolved if another ability causes
        // that card to attach to a specific game element." Zola's Mutate names itself.
        var world = new World(Cards, players: 1);
        var source = world.CreateCard("04115", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        source.TakeDamage(3);
        world.CreateCard("04114", source.Area);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        world.CreateCard("01104", deck);
        var attachment = world.CreateCard("04117", deck);
        var skipped = world.CreateCard("01098", deck);
        var context = Context(world, source);

        var result = AbilityDiscardUntilExecution.Run(Lower("attachment", "attachToThis", "TECH"), context);

        context.Events.AddRange(CompleteEntry(result, world));
        Assert.Equal(source.ObjectId, attachment.Area.Host);
        Assert.Equal(DeckType.EncounterDiscardPile, skipped.Area.Type);
        Assert.Null(result.Reveal);
        Assert.Single(context.Events.OfType<CardAttached>(), item => item.Card == attachment.ObjectId);
    }

    [Rule("rr:when-revealed-abilities.2")]
    [Theory]
    [InlineData("putIntoPlay", false)]
    [InlineData("reveal", true)]
    public void EnvironmentEntryOnlySchedulesRevealWhenPrinted(string instruction, bool revealed)
    {
        // A When Revealed ability does not resolve when its card is put into play.
        var world = new World(Cards, players: 1);
        var source = world.CreateCard("04079a", world.AreaOf(DeckType.MainSchemesArea));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        world.CreateCard("01104", deck);
        var environment = world.CreateCard("04082", deck);
        var result = AbilityDiscardUntilExecution.Run(Lower("environment", instruction), Context(world, source));

        CompleteEntry(result, world);
        Assert.Equal(revealed ? DeckType.RevealingArea : DeckType.EnvironmentArea, environment.Area.Type);
        Assert.Equal(revealed, result.Reveal is not null);
        if (revealed) Assert.Equal(environment.ObjectId, result.Reveal!.Card);
    }

    [Fact]
    public void MissingMatchTerminatesWithoutInventingAnEntry()
    {
        var world = new World(Cards, players: 1);
        var source = world.CreateCard("04115", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        var result = AbilityDiscardUntilExecution.Run(Lower("attachment", "attachToThis", "TECH"), Context(world, source));
        Assert.Null(result.Reveal);
        Assert.DoesNotContain(world.Cards, card => card.Area.Host == source.ObjectId);
        Assert.Single(world.AreaOf(DeckType.EncounterDeck).Cards);
    }

    private static List<GameEvent> CompleteEntry(AbilityDeckAndRevealResult result, World world)
    {
        if (result.Entry is not { } entry) return [];
        EncounterEntry.Schedule(world, entry, new EffectCompletion(1, 1));
        return Agendas.Finish(world, world.Facts);
    }

    [Fact]
    public void ExplicitFirstPlayerRevealDoesNotEngageTheDefeatingPlayersSeat()
    {
        var world = new World(Cards, players: 2);
        world.CreateSeat("first");
        world.CreateSeat("defeater");
        var source = world.CreateCard("04123", world.AreaOf(DeckType.SideSchemesArea));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        world.CreateCard("01104", deck);
        var minion = world.CreateCard("04115", deck);
        var context = Context(world, source);
        context = context with { Expressions = context.Expressions with
            { Bindings = context.Expressions.Bindings with { Player = 1 } } };
        var effect = Lower("minion", "reveal", player: "firstPlayer");

        var result = AbilityDiscardUntilExecution.Run(effect, context);

        Assert.Equal(minion.ObjectId, result.Reveal!.Card);
        Assert.Equal(0, result.Reveal.Player);
    }

    [Rule("rr:attach-to.3.1")]
    [Fact]
    public void ExplicitAttachmentSelectsTopmostMatchingDiscardAndOverridesPrintedHost()
    {
        // An ability's specified host replaces the card's own attach-to phrase.
        var world = new World(Cards, players: 1);
        var source = world.CreateCard("04124", world.AreaOf(DeckType.SideSchemesArea));
        var minion = world.CreateCard("04116", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        var lower = world.CreateCard("04117", discard);
        var top = world.CreateCard("04118", discard);
        var unrelated = world.CreateCard("01098", discard);
        var context = Context(world, source);
        context = context with { Expressions = context.Expressions with
            { Bindings = context.Expressions.Bindings with { Occurrence = new Occurrence(1, [], Subject: minion.ObjectId) } } };
        var effect = new AbilityEffect.AttachCards(new AbilityCardSelection.Last(
            new AbilityCardSelection.InAreas([AbilitySearchArea.EncounterDiscardPile], CardKind.Attachment, "TECH", null)),
            new AbilityCardSelection.Bound(AbilityCardBinding.TriggerSubject));

        var result = AbilityAttachmentExecution.Run(effect, context);

        Assert.Equal(minion.ObjectId, top.Area.Host);
        Assert.Same(discard, lower.Area);
        Assert.Same(discard, unrelated.Area);
        Assert.Null(result.Reveal);
        Assert.Equal([top.ObjectId], context.Events.OfType<CardAttached>().Select(item => item.Card));
    }

    private static AbilityEffect.DiscardUntil Lower(string kind, string instruction, string? trait = null, string? player = null)
    {
        string qualifier = trait is null ? string.Empty : $",\"trait\":\"{trait}\"";
        if (player is not null) qualifier += $",\"player\":\"{player}\"";
        var effect = AbilityCatalog.Parse($$$"""
            {"cards":[{"card":"04115","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"discardUntil":{"from":"encounterDeck","kind":"{{{kind}}}","then":"{{{instruction}}}"{{{qualifier}}}}}
            }]}]}
            """).Abilities[0].Effect;
        return Assert.IsType<AbilityEffect.DiscardUntil>(AbilityLowering.Effect(
            new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
            { [effect.Kind] = effect.Argument }), new AbilityLocation("04115", 0, "effect")));
    }

    private static AbilityDeckAndRevealContext Context(World world, Card source)
    {
        var binding = new AbilityQueryContext(world, source, new Occurrence(0, []), 0,
            source.Incarnation, null, null, null, []);
        var expressions = new AbilityExpressionContext(binding, ImmutableDictionary<string, long>.Empty,
            [], string.Empty, 0, false, null);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""{"cards":[]}"""));
        return new(expressions, "synthetic", [], runner, runner, []);
    }
}


