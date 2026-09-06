using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActivationKeepsCompiledPriority(bool first)
    {
        var (runner, fields) = MutableEffectRunner("enemyAttacks",
            $$"""{"enemies":{"query":"minionsEngagedWithYou"},"first":"{{(first ? "true" : "false")}}"}""", false);
        Card? source = null;
        Card? minion = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard(AuthoredCards.Shocker,
                board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), ability => ability.Card == source!.ObjectId);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Agenda.NowActivation(new PhaseStep(Steps.Attack, 1, 2,
            Subject: villain.ObjectId, Seat: 0));
        var occurrence = new Occurrence(17, [Steps.TurnAction], Player: 0);
        fields["first"] = new AbilityValue.Word(first ? "false" : "true");

        runner.Resolve(world, occurrence, action, [], []);

        Assert.Equal(first ? [minion!.ObjectId, villain.ObjectId] : [villain.ObjectId, minion!.ObjectId],
            world.Agenda.Outstanding.Where(step => step.What == Steps.Attack).Select(step => step.Subject));
        var scheduled = Assert.Single(world.Agenda.Outstanding,
            step => step.What == Steps.Attack && step.Subject == minion.ObjectId);
        var continuation = world.Agenda.ActivationWait(scheduled.ActivationId);
        Assert.NotNull(continuation);
        Assert.Same(occurrence, continuation.Value.AbilityOccurrence);
        Assert.Equal(source!.ObjectId, continuation.Value.Subject);
        Assert.Equal(0, continuation.Value.AbilityOrdinal);
        Assert.Empty(continuation.Value.AbilityPath!);
    }

    [Theory]
    [InlineData("trigger.actor")]
    [InlineData("trigger.target")]
    public void ActivationKeepsCompiledTargetAndSnapshotController(string role)
    {
        // The engine's occurrence bindings retain the attacked player's
        // snapshot even when the named character changes control meanwhile.
        var (runner, fields) = MutableEffectRunner("enemyAttacks",
            $$"""{"enemies":{"query":"villain"},"against":"{{role}}"}""", false);
        Card? source = null;
        Card? ally = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            ally = board.CreateCard("01083", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), ability => ability.Card == source!.ObjectId);
        var occurrence = Occurrence.ForAttack(18, [Steps.TurnAction], world, Cards,
            ally!.ObjectId, world.Seats[1].IdentityCard.ObjectId, player: 0);
        Assert.Equal(1, occurrence.ActorFacts!.Controller);
        Assert.Equal(1, occurrence.TargetFacts!.Controller);
        World.MoveToTop(ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        fields["against"] = new AbilityValue.Word("you");

        runner.Resolve(world, occurrence, action, [], []);

        var attack = Assert.Single(world.Agenda.Outstanding, step => step.What == Steps.Attack);
        Assert.Equal(role == "trigger.actor" ? ally.ObjectId : world.Seats[1].IdentityCard.ObjectId,
            attack.Character);
        Assert.Equal(1, attack.Seat);
        Assert.Equal(1, attack.Index);
        Assert.Equal(world.TheCardIn(DeckType.VillainArea)!.ObjectId, attack.Subject);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActivationKeepsCompiledEngagedHeroBinding(bool hero)
    {
        var (runner, fields) = MutableEffectRunner("enemyAttacks",
            """{"enemies":{"query":"minions"},"against":"engagedHero"}""", false);
        Card? source = null;
        Card? minion = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard(AuthoredCards.Shocker,
                board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            if (hero) board.Seats[1].IdentityCard.TurnTo("01010a");
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), ability => ability.Card == source!.ObjectId);
        fields["against"] = new AbilityValue.Word("you");

        runner.Resolve(world, new Occurrence(19, [Steps.TurnAction], Player: 0), action, [], []);

        var attacks = world.Agenda.Outstanding.Where(step => step.What == Steps.Attack).ToList();
        if (hero)
        {
            var attack = Assert.Single(attacks);
            Assert.Equal(minion!.ObjectId, attack.Subject);
            Assert.Equal(1, attack.Seat);
            Assert.Equal(-1, attack.Character);
        }
        else Assert.Empty(attacks);
    }

    [Rule("rr:activation.5")]
    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void ActivationOrderKeepsCompiledEnemiesAcrossTheChoice(bool attack, bool changeWhilePending)
    {
        // "followed by minion activations in the order of your choice."
        // Compilation is an engine-owned snapshot boundary: changing the
        // caller's selector cannot rewrite either that question or its answer.
        var (runner, fields) = MutableEffectRunner(attack ? "enemyAttacks" : "enemySchemes",
            """{"enemies":{"query":"minionsEngagedWithYou"}}""", false);
        Card? source = null;
        Card? first = null;
        Card? second = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var engaged = board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
            first = board.CreateCard(AuthoredCards.Shocker, engaged);
            second = board.CreateCard(AuthoredCards.Shocker, engaged);
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        void ChangeSelector() => fields["enemies"] = new AbilityValue.Map(
            new Dictionary<string, AbilityValue> { ["query"] = new AbilityValue.Word("villain") });
        if (!changeWhilePending) ChangeSelector();

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(Question.Order, game.Pending!.Asking);
        Assert.Equal(world.FirstPlayer, game.Pending.Player);
        var order = Assert.Single(game.Pending.Affordances);
        Assert.Equal([first!.ObjectId, second!.ObjectId], order.Targets!.Legal);
        Assert.Equal(2, order.Targets.Min);
        Assert.Equal(2, order.Targets.Max);
        if (changeWhilePending) ChangeSelector();
        var result = game.Resolve(Decision.Take(order.Id, [second.ObjectId, first.ObjectId], []));

        if (attack)
        {
            Assert.Equal(Question.Defender, game.Pending!.Asking);
            Assert.Equal(second.ObjectId, world.Attack!.Enemy);
            game.Resolve(Decision.Decline);
            Assert.Equal(Question.Defender, game.Pending!.Asking);
            Assert.Equal(first.ObjectId, world.Attack!.Enemy);
            game.Resolve(Decision.Decline);
            Assert.Equal(4, world.Seats[0].IdentityCard.Damage);
        }
        else
        {
            var placements = result.Events.OfType<FieldSet>()
                .Where(change => change.Field == "k_threat").ToList();
            Assert.Equal(2, placements.Count);
            Assert.Equal(2, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
            Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        }
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void ActivationResumeKeepsTheCompiledDynamicFlag(bool dynamic, bool changeWhilePending)
    {
        // The DSL's dynamic flag chooses whether the instruction re-reads its
        // enemy collection after a completed batch. Its persisted results must
        // exclude enemies already processed and retain that compiled choice.
        var (runner, fields) = MutableEffectRunner("enemyAttacks",
            $$"""{"enemies":{"query":"minionsEngagedWithYou"},"dynamic":"{{(dynamic ? "true" : "false")}}"}""", false);
        Card? source = null;
        Card? first = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            first = board.CreateCard(AuthoredCards.Shocker,
                board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        void ChangeFlag() => fields["dynamic"] = new AbilityValue.Word(dynamic ? "false" : "true");
        if (!changeWhilePending) ChangeFlag();
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(Question.Defender, game.Pending!.Asking);
        Assert.Equal(first!.ObjectId, world.Attack!.Enemy);

        if (changeWhilePending) ChangeFlag();
        var newcomer = world.CreateCard(AuthoredCards.Shocker,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        game.Resolve(Decision.Decline);

        if (dynamic)
        {
            Assert.Equal(Question.Defender, game.Pending!.Asking);
            Assert.Equal(newcomer.ObjectId, world.Attack!.Enemy);
            game.Resolve(Decision.Decline);
        }
        Assert.Equal(dynamic ? 4 : 2, world.Seats[0].IdentityCard.Damage);
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }
}
