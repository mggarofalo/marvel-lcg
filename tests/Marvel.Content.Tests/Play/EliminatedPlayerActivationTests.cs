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
    [InlineData("enemyAttacks", "engagedHero", 1)]
    [InlineData("enemyAttacks", "you", 0)]
    [InlineData("enemySchemes", "you", 0)]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:player-elimination.step.5")]
    public void TheRestOfTheAbilityActivatesOnlyAgainstPlayersStillInTheGame(string activation, string against, int attacks)
    {
        // Synthetic action: "Deal 30 damage to your hero. Each minion attacks the hero it is
        // engaged with", "... attacks you" or "... schemes against you". rr:player-elimination.5: "If a player is
        // eliminated partway through the resolution of an ability, resolve the entire
        // ability." rr:player-elimination.step.5: "Eliminated players no longer participate
        // in the game", so a minion engaged with the other hero still attacks that hero,
        // and no enemy activates against the eliminated player.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"seq":[
              {"dealDamage":{"cards":"you","amount":30}},
              {"ACTIVATION":{"enemies":{"query":"minions"},"against":"AGAINST"}}
            ]}
            """.Replace("ACTIVATION", activation, StringComparison.Ordinal)
                .Replace("AGAINST", against, StringComparison.Ordinal),
            cost: """{"exhaust":"this"}""", includeAuthored: true);
        var world = Hero("01001a", players: 2);
        world.Abilities = runner;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var main = world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        var source = InPlay(world, AuthoredCards.AuntMay);
        // Hydra Bomber: ATK 1, SCH 1.
        world.CreateCard("01110", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        // An exhausted hero has no basic defense to declare.
        world.Seats[1].IdentityCard.Exhaust();
        var events = new List<GameEvent>();

        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        events.AddRange(runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events);
        Sequence.Finish(world, Cards, runner, events);

        Assert.True(world.Seats[0].Eliminated);
        Assert.False(world.Seats[1].Eliminated);
        Assert.Equal(attacks, events.OfType<AttackBoundary>().Count(boundary => boundary.Started));
        Assert.Equal(attacks, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(world.Agenda.IsBusy);
    }
}
