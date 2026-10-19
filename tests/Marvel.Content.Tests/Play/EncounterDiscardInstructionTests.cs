using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
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


