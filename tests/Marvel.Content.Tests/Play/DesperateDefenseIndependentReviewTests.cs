using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DesperateDefenseIndependentReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:tough.3")]
    public void PreventedAttackDamageReadiesAnAlreadyDamagedHeroUnlessReadyingIsProhibited(bool prohibited)
    {
        // Tough prevents damage fully: the character "is not considered to have taken damage".
        // Printed 09015 tests damage from that attack, not the hero's existing damage.
        var (world, runner, source, payment, _) = DesperateDefensePrerequisiteFixture.Board(1, 7);
        var hero = world.Seats[1].IdentityCard;
        hero.TakeDamage(3);
        Statuses.Give(world, hero, Statuses.Tough);
        if (prohibited)
            world.Effects.Register(new(EffectSource.LastingEffect, ReadinessPermissions.ProhibitedField,
                Amount: 1, Affects: hero.ObjectId));
        var events = new List<GameEvent>();
        Assert.Equal(1, DesperateDefensePrerequisiteFixture.Drain(world, runner, source, payment,
            hero.ObjectId, 1, events));
        Assert.Equal(3, hero.Damage);
        Assert.False(Statuses.Has(world, hero, Statuses.Tough));
        Assert.Equal(!prohibited, hero.Ready);
        Assert.Equal(2, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
        Assert.Null(world.Activation);
        Assert.Null(world.Attack);
        Assert.False(world.Agenda.IsBusy);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind == UndamagedDefenseReady.Kind);
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    public void ReenteredEnemyCannotAuthorizeTheOriginalDefenseAndCompletionRetiresUnusedReady()
    {
        // A returned card "is considered to be a new copy of the card".
        var (world, runner, source, _, enemy) = DesperateDefensePrerequisiteFixture.Board(1, 4);
        var occurrence = Prepare(world, runner);
        Assert.True(UndamagedDefenseReady.CanInstall(world, occurrence, 1));
        UndamagedDefenseReady.Install(world, occurrence, source, 1);
        var registration = Assert.Single(world.Effects.Registered,
            effect => effect.Kind == UndamagedDefenseReady.Kind);
        Assert.Equal(world.Activation!.Id, registration.ActivationId);
        Assert.Equal(world.Seats[1].IdentityCard.Incarnation, registration.TargetIncarnation);
        var home = enemy.Area;
        World.MoveToTop(enemy, world.AreaOf(DeckType.AsideDeck));
        World.MoveToTop(enemy, home);
        Assert.False(UndamagedDefenseReady.CanInstall(world, occurrence, 1));
        world.Effects.CompleteActivation(world.Activation.Id, []);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind == UndamagedDefenseReady.Kind);
        Assert.False(world.Seats[1].IdentityCard.Ready);
    }

    [Fact]
    [Rule("rr:activation.8")]
    public void AttackRequestedInsideTheDefenseWindowQueuesAndCannotInheritTheReadyOrDefenseGrant()
    {
        // An activation initiated during an activation "resolves after the current ... activation".
        // Synthetic Armored Vest requests a real villain attack; only 09015 has its printed behavior.
        var (world, _, source, payment, _) = DesperateDefensePrerequisiteFixture.Board(1, 4);
        var runner = QueuedAttackRunner();
        world.Abilities = runner;
        var support = world.CreateCard("01079", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var hero = world.Seats[1].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        PrepareQueuedVillain(world, villain);
        var events = new List<GameEvent>();
        int played = 0, requested = 0;
        var trace = new List<string>();
        bool readyBeforeQueuedDefense = false;
        for (int decision = 0; decision < 50; decision++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            trace.Add($"{prompt.Asking} {prompt.Label}: source={source.Area.Type} offers={string.Join('/', prompt.Affordances.Select(item => $"{item.Id}:{item.AnchorId}"))}");
            var offered = prompt.Affordances.SingleOrDefault(item => item.AnchorId == source.ObjectId);
            var queued = prompt.Affordances.SingleOrDefault(item => item.AnchorId == support.ObjectId);
            Decision answer;
            if (offered is not null)
            {
                if (prompt.Asking == Question.Opportunity) played++;
                answer = Decision.Take(offered.Id, [], [payment.ObjectId]);
            }
            else if (queued is not null)
            {
                if (prompt.Asking == Question.Opportunity) requested++;
                Assert.Empty(events.OfType<AttackCompleted>());
                answer = Decision.Take(queued.Id);
            }
            else if (prompt.Asking == Question.Defender && prompt.Player == 1)
            {
                if (events.OfType<AttackCompleted>().Any()) readyBeforeQueuedDefense = hero.Ready;
                answer = Decision.Take(hero.ObjectId);
            }
            else answer = Decision.Decline;
            Sequence.Answer(world, world.Facts, runner, prompt, answer, events);
        }
        Assert.True(played == 1, string.Join(Environment.NewLine, trace));
        Assert.True(requested == 1, string.Join(Environment.NewLine, trace));
        Assert.True(readyBeforeQueuedDefense);
        var completed = events.OfType<AttackCompleted>().ToArray();
        Assert.Equal(2, completed.Length);
        Assert.NotEqual(villain.ObjectId, completed[0].Enemy);
        Assert.Equal(villain.ObjectId, completed[1].Enemy);
        Assert.Equal(0, completed[0].DamageDealt);
        Assert.Equal(2, completed[1].DamageDealt);
        Assert.Equal(2, hero.Damage);
        Assert.False(hero.Ready);
        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Activation);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind == UndamagedDefenseReady.Kind);
    }

    private static void PrepareQueuedVillain(World world, Card villain)
    {
        long printed = StateFields.Modified(world, villain, "attack", world.Facts, world.Players);
        world.Effects.Register(new(EffectSource.LastingEffect, "attack", 4 - printed, Affects: villain.ObjectId));
        for (int card = 0; card < 5; card++) world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
    }

    private static Occurrence Prepare(World world, AbilityRunner runner)
    {
        var helper = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, helper, Decision.Decline, []);
        var defense = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, defense,
            Decision.Take(world.Seats[1].IdentityCard.ObjectId), []);
        Assert.NotNull(Sequence.Work(world, world.Facts, runner, []));
        return world.Agenda.Occurrence!;
    }

    private static AbilityRunner QueuedAttackRunner()
    {
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        var cards = book["cards"]!.AsArray();
        var vest = cards.Single(card => card!["card"]!.GetValue<string>() == "01079");
        cards.Remove(vest);
        cards.Add(JsonNode.Parse("""
            {"card":"01079","abilities":[{
              "trigger":{"event":"WhenHeroDefends","timing":"Interrupt","subject":"you","form":"hero"},
              "cost":{"exhaust":"this"},
              "effect":{"enemyAttacks":{"enemies":{"query":"villain"}}}}]}
            """));
        return new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
    }
}
