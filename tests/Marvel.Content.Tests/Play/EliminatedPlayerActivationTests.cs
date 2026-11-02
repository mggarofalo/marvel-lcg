using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>An ability's enemy activation after the ability eliminated its own player.</summary>
public sealed class EliminatedPlayerActivationTests : CoreCardTestBase
{
    [Theory]
    [InlineData("enemyAttacks", "engagedHero", 1, 0)]
    [InlineData("enemyAttacks", "you", 0, 0)]
    [InlineData("enemySchemes", null, 0, 1)]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:player-elimination.step.5")]
    [Rule("rr:scheme-enemy-activation.step.3")]
    public void TheRestOfTheAbilityResolvesWithoutAttackingTheEliminatedPlayer(
        string activation, string? against, int attacks, int threat)
    {
        // Synthetic action: "Deal 30 damage to your hero. Each minion attacks the hero it is
        // engaged with", "... attacks you" or "... schemes". rr:player-elimination.5: "If a
        // player is eliminated partway through the resolution of an ability, resolve the
        // entire ability." rr:player-elimination.step.5: "Eliminated players no longer
        // participate in the game", so a minion engaged with the other hero still attacks
        // that hero, and nobody attacks the eliminated player. rr:scheme-enemy-activation.step.3:
        // "Place threat on the main scheme equal to the scheming enemy's modified SCH value";
        // ruling 79c57a49f6e38fe0 has an eliminated player still place Metal Shards' counter
        // on the main scheme, so the scheme still places its threat.
        var (world, runner, source, main) = Board(activation, against);
        var events = new List<GameEvent>();

        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        events.AddRange(runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events);
        Sequence.Finish(world, Cards, runner, events);

        Assert.True(world.Seats[0].Eliminated);
        Assert.False(world.Seats[1].Eliminated);
        Assert.Equal(attacks, events.OfType<AttackBoundary>().Count(boundary => boundary.Started));
        Assert.Equal(attacks, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(threat, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:player-elimination.5")]
    public void ASchemeAgainstTheEliminatedPlayersIdentityRaises()
    {
        // Synthetic action: "Deal 30 damage to your hero. Each minion schemes against you."
        // The named identity has left play with its player, and no rule names another
        // character for the scheme to be against, so the engine refuses rather than guess.
        var (world, runner, source, main) = Board("enemySchemes", "you");
        var events = new List<GameEvent>();
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);

        var error = Assert.Throws<RulesNotImplementedException>(() =>
        {
            events.AddRange(runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events);
            Sequence.Finish(world, Cards, runner, events);
        });

        Assert.Contains("cannot resolve its explicit attacked character", error.Message);
        Assert.True(world.Seats[0].Eliminated);
        Assert.Equal(0, main.Tokens.GetValueOrDefault("k_threat"));
    }

    private static (World World, AbilityRunner Runner, Card Source, Card Main) Board(
        string activation, string? against)
    {
        string target = against is null ? "" : $$""","against":"{{against}}" """;
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$$"""
            {"seq":[
              {"dealDamage":{"cards":"you","amount":30}},
              {"{{{activation}}}":{"enemies":{"query":"minions"}{{{target}}}}}
            ]}
            """, cost: """{"exhaust":"this"}""", includeAuthored: true);
        var world = Hero("01001a", players: 2);
        world.Abilities = runner;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var main = world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        var source = InPlay(world, AuthoredCards.AuntMay);
        // Hydra Bomber: ATK 1, SCH 1.
        world.CreateCard("01110", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        // An exhausted hero has no basic defense to declare.
        world.Seats[1].IdentityCard.Exhaust();
        return (world, runner, source, main);
    }
}
