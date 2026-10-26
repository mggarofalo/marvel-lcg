using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class StunnedFriendlyCountTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    private static AbilityRunner Runner() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"02037","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{"placeThreat":{"scheme":"this","amount":{"mul":[2,
            {"count":{"withStatus":{"cards":{"query":"heroesAndAllies"},"status":"stunned"}}}
          ]}}}
        }]}]}
        """));

    [Rule("rr:friendly")]
    [Rule("rr:character")]
    [Rule("rr:stun-stunned.3")]
    [Fact]
    public void MessOfThingsCountsOnlyStunnedFriendlyCharactersAcrossSeats()
    {
        // "Friendly is a blanket term that refers to cards the players control."
        // "Identities (heroes and alter-egos), allies, villains, and minions are all characters."
        // "A character is stunned if it has a stunned status card."
        var world = Board();
        var scheme = world.CreateCard("02037", world.AreaOf(DeckType.SideSchemesArea));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var unstunnedAlly = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var enemy = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var outOfPlay = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea,
            PlayArea.Of(1), cardOwner: 1));
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Stunned);
        Statuses.Give(world, ally, Statuses.Stunned);
        Statuses.Give(world, enemy, Statuses.Stunned);
        Statuses.Give(world, outOfPlay, Statuses.Stunned);
        World.MoveToTop(outOfPlay, world.Seats[1].Hand);
        Statuses.Give(world, unstunnedAlly, Statuses.Confused);
        var runner = Runner();
        world.Abilities = runner;
        string digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        var selection = new AbilityCardSelection.WithStatus(
            new AbilityCardSelection.Query(AbilityCardQuery.HeroesAndAllies), Statuses.Stunned);
        var query = new AbilitySelectorEvaluation(new AbilityQueryContext(world, scheme,
            new Occurrence(1, []), 0, scheme.Incarnation, null, null, null, []));
        Assert.Equal([world.Seats[0].IdentityCard, ally], query.Every(selection));
        Assert.Equal([world.Seats[0].IdentityCard, ally], query.Every(selection));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);

        var resolution = runner.ExecuteReveal(world, scheme, 0);
        Sequence.Finish(world, Cards, runner, resolution.Events.ToList());
        Assert.True(resolution.IsComplete);
        Assert.Equal(4, scheme.Tokens.GetValueOrDefault("k_threat"));
        World.MoveToTop(Statuses.On(world, ally, Statuses.Stunned).Single(),
            world.AreaOf(DeckType.RemovedArea));
        Assert.Equal([world.Seats[0].IdentityCard], query.Every(selection));
    }

    [Rule("rr:stun-stunned.3.1")]
    [Fact]
    public void OneStunnedCardOnSteadyCharacterDoesNotCountUntilSecond()
    {
        // "A character with the steady keyword is stunned only if it has two stunned status cards."
        var world = Board();
        var scheme = world.CreateCard("02037", world.AreaOf(DeckType.SideSchemesArea));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "steady", 1,
            Affects: ally.ObjectId));
        Statuses.Give(world, ally, Statuses.Stunned);
        var selected = new AbilityCardSelection.WithStatus(
            new AbilityCardSelection.Query(AbilityCardQuery.HeroesAndAllies), Statuses.Stunned);
        var query = new AbilityQueryContext(world, scheme, new Occurrence(1, []), 0,
            scheme.Incarnation, null, null, null, []);
        var expressions = new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty,
            [], "", 0, false, null);
        var admission = new AbilityAdmissionContext(AbilityLowering.Book(AuthoredCards.Book),
            world.ResourceAbilities, expressions, new(), null);
        var projected = new AreaProjectionState(admission);
        string digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        Assert.Empty(AbilityProjectedStateQueries.ProjectedEvery(selected, projected, admission));
        projected.Status[(ally.ObjectId, Statuses.Stunned)] = 2;
        Assert.Equal([ally], AbilityProjectedStateQueries.ProjectedEvery(selected, projected, admission));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        var runner = Runner();
        world.Abilities = runner;
        var first = runner.ExecuteReveal(world, scheme, 0);
        Sequence.Finish(world, Cards, runner, first.Events.ToList());
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("k_threat"));
        Statuses.Give(world, ally, Statuses.Stunned);
        projected.Status[(ally.ObjectId, Statuses.Stunned)] = 0;
        digest = world.Digest().Canonical();
        words = world.Random.Generator.WordsConsumed;
        Assert.Empty(AbilityProjectedStateQueries.ProjectedEvery(selected, projected, admission));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        var second = runner.ExecuteReveal(world, scheme, 0);
        Sequence.Finish(world, Cards, runner, second.Events.ToList());
        Assert.Equal(2, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

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
