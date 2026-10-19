using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic selector fixtures for printed Zola encounter criteria.</summary>
public sealed class CampaignEncounterSelectorTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void RemainingHealthRanksDamageAndModifiersAndKeepsAllTies()
    {
        var world = Board();
        var source = world.CreateCard("04117", world.AreaOf(DeckType.RevealingArea));
        var first = world.CreateCard("04115", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var second = world.CreateCard("04114", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        first.TakeDamage(3);
        var selector = Selection("""{"maxBy":{"of":{"query":"minions"},"by":"remainingHealth"}}""");
        var evaluation = new AbilitySelectorEvaluation(Context(world, source));
        Assert.Equal([second], evaluation.Every(selector));

        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "health", 2, Affects: first.ObjectId));
        Assert.Equal([first, second], evaluation.Every(selector));
        second.TakeDamage(1);
        Assert.Equal([first], evaluation.Every(selector));
    }

    [Fact]
    public void ProjectedHealthSelectionUsesProjectedDamageWithoutMutatingLiveCards()
    {
        var world = Board();
        var source = world.CreateCard("04117", world.AreaOf(DeckType.RevealingArea));
        var first = world.CreateCard("04115", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var second = world.CreateCard("04114", first.Area);
        var expression = new AbilityExpressionContext(Context(world, source), ImmutableDictionary<string, long>.Empty,
            [], "", 0, false, null);
        var admission = new AbilityAdmissionContext(AbilityLowering.Book(AuthoredCards.Book),
            world.ResourceAbilities, expression, new(), null);
        var state = new AreaProjectionState(admission);
        state.Damage[first.ObjectId] = 3;
        var selector = Selection("""{"maxBy":{"of":{"query":"minions"},"by":"remainingHealth"}}""");

        Assert.Equal([second], AbilityProjectedStateQueries.ProjectedEvery(selector, state, admission));
        Assert.Equal(0, first.Damage);
    }

    [Rule("rr:identity-specific-card")]
    [Fact]
    public void HeroClassSearchIncludesHandAndExcludesBasicAlliesAndOtherPlayers()
    {
        // Identity-specific (also called hero-specific) cards belong to an identity's accompanying set.
        var world = Board();
        var source = world.CreateCard("04122", world.AreaOf(DeckType.SideSchemesArea));
        var held = world.CreateCard("01002", world.Seats[0].Hand);
        var buried = world.CreateCard("01041", world.Seats[0].Deck);
        var discarded = world.CreateCard("04032", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01084", world.Seats[0].Hand);
        world.CreateCard("01002", world.Seats[1].Hand);
        var selector = Selection("""{"cardsIn":{"areas":["yourDeck","yourDiscardPile","yourHand"],"kind":"Ally","cardClass":"Hero"}}""");
        string before = world.Digest().Canonical();
        long random = world.Random.Generator.WordsConsumed;

        Assert.Equal([buried, discarded, held], new AbilitySelectorEvaluation(Context(world, source)).Every(selector));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random, world.Random.Generator.WordsConsumed);
    }

    private static AbilityCardSelection Selection(string json)
    {
        var effect = AbilityCatalog.Parse($$$"""
            {"cards":[{"card":"04122","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"returnToHand":{{{json}}}}
            }]}]}
            """).Abilities[0].Effect;
        return Assert.IsType<AbilityEffect.CardAction>(AbilityLowering.Effect(
            new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
            { [effect.Kind] = effect.Argument }), new AbilityLocation("04122", 0, "effect"))).Selection;
    }

    private static AbilityQueryContext Context(World world, Card source) =>
        new(world, source, new Occurrence(1, []), 0, source.Incarnation, null, null, null, []);

    private static World Board()
    {
        var world = new World(Cards, 2);
        for (int i = 0; i < 2; i++)
        {
            var seat = world.CreateSeat($"p{i}");
            seat.IdentityCard = world.CreateCard(i == 0 ? "01001a" : "01010a", seat.Hero);
        }
        return world;
    }
}
