using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic ranked pair instructions, without pack admission or ability-book rows.
public sealed class RankedEnemyActivationTests : ChoosingCardsTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:first-player.1")]
    public void BothTiesRetainTheCharacterAndOfferExactlyOneEnemy(bool defend)
    {
        // "The first player selects among the eligible options."
        var (world, runner, allies, enemies) = Fixture();
        var character = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(1, character.Player);
        Assert.Equal(allies.Select(card => card.ObjectId), character.Affordances.Select(offer => offer.Id));
        Sequence.Answer(world, Cards, runner, character, Decision.Take(allies[1].ObjectId), []);
        var enemy = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(1, enemy.Player);
        var offer = Assert.Single(enemy.Affordances);
        Assert.Equal(enemies.Select(card => card.ObjectId), offer.Targets!.Legal);
        Assert.Equal(1, offer.Targets.Min);
        Assert.Equal(1, offer.Targets.Max);
        Assert.Equal("rr:first-player.1", offer.Targets.Rule);
        Sequence.Answer(world, Cards, runner, enemy,
            Decision.Take(offer.Id, [enemies[1].ObjectId], []), []);
        var attack = Assert.Single(world.Agenda.Outstanding, step => step.What == Steps.Attack);
        Assert.Equal(enemies[1].ObjectId, attack.Subject);
        Assert.Equal(1, attack.Seat);
        Assert.Equal(allies[1].ObjectId, attack.Require<EnemyAttackOperation>().Character);
        if (defend)
        {
            for (int step = 0; ; step++)
            {
                Assert.True(step < 10);
                var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
                if (prompt.Asking == Question.Defender && prompt.Player == 0)
                {
                    Sequence.Answer(world, Cards, runner, prompt, Decision.Take(allies[0].ObjectId), []);
                    Assert.Equal(0, world.Attack!.Player);
                    Assert.Equal(allies[0].ObjectId, world.Attack.Target);
                    break;
                }
                Assert.True(prompt.Cancellable);
                Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, []);
            }
        }
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(3, allies[defend ? 0 : 1].Damage);
        Assert.Equal(0, allies[defend ? 1 : 0].Damage);
        Assert.All(world.Seats, seat => Assert.Equal(0, seat.IdentityCard.Damage));
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Null(world.Activation);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [Rule("rr:stun-stunned.7")]
    [Rule("rr:tough.2")]
    public void UniqueModifiedMaximaNeedNoChoiceAndSurgeOnlyWithoutAnAttack(bool stunned, bool hero, bool tough)
    {
        // A replaced attack means the character "is not considered to have attacked."
        // Tough instead says "prevent all of that damage"; the attack still occurred.
        var (world, runner, allies, enemies) = Fixture(start: false);
        if (hero) Forms.Change(world.Seats[1], Cards);
        var recipient = hero ? world.Seats[1].IdentityCard : allies[1];
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "attack", Amount: 3, Card: recipient.ObjectId, Affects: recipient.ObjectId));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "attack", Amount: 1, Card: enemies[1].ObjectId, Affects: enemies[1].ObjectId));
        if (stunned) Statuses.Give(world, enemies[1], Statuses.Stunned);
        if (tough) Statuses.Give(world, recipient, Statuses.Tough);
        var source = Assert.Single(world.Cards, card => card.FaceId == "10028");
        runner.ExecuteReveal(world, source, 0);
        var attack = Assert.Single(world.Agenda.Outstanding, step => step.What == Steps.Attack);
        Assert.Equal(enemies[1].ObjectId, attack.Subject);
        Assert.Equal(recipient.ObjectId, attack.Require<EnemyAttackOperation>().Character);
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(stunned || tough ? 0 : 4, recipient.Damage);
        Assert.Equal(stunned ? 1 : 0,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
        Assert.False(Statuses.Has(world, enemies[1], Statuses.Stunned));
        Assert.False(Statuses.Has(world, recipient, Statuses.Tough));
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void EnemyChoiceQueriesAndRejectedAnswersPreserveTheAttackPair()
    {
        var (world, runner, allies, enemies) = Fixture();
        var (control, controlRunner, controlAllies, controlEnemies) = Fixture();
        var character = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var controlCharacter = Assert.IsType<Prompt>(Sequence.Work(control, Cards, controlRunner, []));
        Sequence.Answer(world, Cards, runner, character, Decision.Take(allies[1].ObjectId), []);
        Sequence.Answer(control, Cards, controlRunner, controlCharacter,
            Decision.Take(controlAllies[1].ObjectId), []);
        var continuation = world.Agenda.Outstanding[0];
        string before = Snapshot(world);
        var enemy = Assert.IsType<Prompt>(runner.Choosing(world, continuation));
        var repeated = Assert.IsType<Prompt>(runner.Choosing(world, continuation));
        Assert.Equal(before, Snapshot(world));
        Assert.Equal(enemy.Player, repeated.Player);
        var offer = Assert.Single(enemy.Affordances);
        Assert.Equal(offer.Targets!.Legal, Assert.Single(repeated.Affordances).Targets!.Legal);
        Assert.Equal(DecisionRejection.UnauthorizedActor,
            DecisionAssessment.Assess(enemy, Decision.Take(offer.Id, [enemies[1].ObjectId], []), actor: 0).Rejection);
        // Engine choice: malformed selection rejects before committing pending work.
        foreach (var answer in new[] { Decision.Decline, Decision.Take(offer.Id),
            Decision.Take(offer.Id, enemies.Select(card => card.ObjectId).ToArray(), []) })
        {
            Assert.Throws<ArgumentException>(() => runner.Chose(world, continuation, answer));
            Assert.Equal(before, Snapshot(world));
        }
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var controlPrompt = Assert.IsType<Prompt>(Sequence.Work(control, Cards, controlRunner, []));
        Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(offer.Id, [enemies[1].ObjectId], []), []);
        Sequence.Answer(control, Cards, controlRunner, controlPrompt,
            Decision.Take(Assert.Single(controlPrompt.Affordances).Id, [controlEnemies[1].ObjectId], []), []);
        WreckingRemainingSchemeFixture.Finish(world, runner);
        WreckingRemainingSchemeFixture.Finish(control, controlRunner);
        Assert.Equal(3, allies[1].Damage);
        Assert.Equal(Snapshot(control), Snapshot(world));
    }

    [Fact]
    public void StaleEnemyChoiceRejectsWithoutChangingPendingWork()
    {
        var (world, runner, allies, enemies) = Fixture();
        var character = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Sequence.Answer(world, Cards, runner, character, Decision.Take(allies[1].ObjectId), []);
        var enemy = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        World.MoveToTop(enemies[1], world.AreaOf(DeckType.EncounterDiscardPile));
        string before = Snapshot(world);
        // Engine choice: a disappeared typed choice is an unsupported stale
        // continuation; it cannot silently pick its remaining candidate.
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, Cards, runner, enemy,
            Decision.Take(Assert.Single(enemy.Affordances).Id, [enemies[1].ObjectId], []), []));
        Assert.Equal(before, Snapshot(world));
    }

    [Fact]
    public void EmptyEnemyPoolDoesNotAskForAnUnnecessaryCharacterChoice()
    {
        var (world, runner, _, enemies) = Fixture(start: false);
        foreach (var enemy in enemies.Append(world.TheCardIn(DeckType.VillainArea)!))
            World.MoveToTop(enemy, world.AreaOf(DeckType.EncounterDiscardPile));
        runner.ExecuteReveal(world, Assert.Single(world.Cards, card => card.FaceId == "10028"), 0);
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.All(world.Seats, seat => Assert.Equal(0, seat.IdentityCard.Damage));
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static string Snapshot(World world) => System.Text.Json.JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Stage, world.Agenda.Outstanding,
        world.Agenda.Current, world.Agenda.Occurrence, world.Effects.Registered, world.Attack, world.Activation,
    });

    private static (World, AbilityRunner, Card[], Card[]) Fixture(bool start = true)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"10028","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"otherwise":{"effect":{"chooseCard":{"chooser":"firstPlayer",
                "from":{"maxBy":{"of":{"query":"heroesAndAllies"},"by":"attack"}},
                "effect":{"enemyAttacks":{"enemies":{"maxBy":{"of":{"query":"enemies"},"by":"attack"}},
                  "against":"chosen","chooseEnemy":"firstPlayer"}}}},"otherwise":{"gainSurge":1}}}
            }]}]}
            """));
        var world = Deal("spider_man", "captain_marvel");
        world.Agenda.Abandon();
        world.Abilities = runner;
        world.FirstPlayer = 1;
        var allies = Enumerable.Range(0, 2).Select(player => world.CreateCard(player == 0 ? "06011" : "01050",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: 0))).ToArray();
        var enemies = Enumerable.Range(0, 2).Select(_ => world.CreateCard("01167",
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)))).ToArray();
        var source = world.CreateCard("10028", world.AreaOf(DeckType.RevealingArea));
        if (start) runner.ExecuteReveal(world, source, 0);
        return (world, runner, allies, enemies);
    }
}
