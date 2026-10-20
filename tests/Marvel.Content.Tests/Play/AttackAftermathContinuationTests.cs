using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AttackAftermathContinuationTests
{
    [Rule("rr:retaliate-x.1")]
    [Rule("rr:interrupt.3")]
    [Rule("rr:ability.4")]
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(false, false, false, true)]
    [InlineData(true, false, false, true)]
    public void FollowingEffectWaitsForTheAttacksSuspendedRetaliation(bool prevent, bool ranged, bool repeatedDamage, bool secondEnemy)
    {
        var control = Run(prevent, ranged, repeatedDamage, secondEnemy, false);
        var probed = Run(prevent, ranged, repeatedDamage, secondEnemy, true);
        Assert.Equal(control.World.Digest().Canonical(), probed.World.Digest().Canonical());
        Assert.Equal(control.Events, probed.Events);
        Assert.Equal(control.World.Random.Generator.GetState().Words, probed.World.Random.Generator.GetState().Words);
        Assert.Equal(control.World.Random.Generator.GetState().Index, probed.World.Random.Generator.GetState().Index);
        Assert.Equal(control.World.Random.Generator.WordsConsumed, probed.World.Random.Generator.WordsConsumed);
    }

    private static (World World, string[] Events) Run(bool prevent, bool ranged,
        bool repeatedDamage, bool secondEnemy, bool probe)
    {
        var runner = Runner(repeatedDamage, secondEnemy);
        var (game, world, source, payment, modifier) = Board(runner, ranged, secondEnemy);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        int[] paying = [payment!.ObjectId];
        var allocation = ResourcePayment.Allocate(Assert.Single(action.Costs!), paying);
        var events = game.Resolve(new Decision(action.Id, [], paying, Allocations: allocation)).Events.ToList();
        Assert.Equal(repeatedDamage ? 4 : 2, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.DoesNotContain(modifier!, world.Effects.Active());
        if (secondEnemy && !ranged)
        {
            // rr:attack-player-ability-type.step.7: forced abilities resolve "in any order".
            Assert.Equal(Question.Order, game.Pending!.Asking);
            events.AddRange(game.Resolve(Decision.Take(game.Pending.Affordances[0].Id)).Events);
        }
        if (!ranged)
        {
            Assert.NotNull(game.Pending);
            Assert.Equal(0, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_observed"));
            Answer();
            if (secondEnemy)
            {
                Assert.Equal(0, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_observed"));
                Assert.NotNull(game.Pending);
                Answer();
            }
        }
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens["c_observed"]);
        Assert.Equal(ExpectedDamage(prevent, ranged, secondEnemy), world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        return (world, events.Select(item => System.Text.Json.JsonSerializer.Serialize(item, item.GetType())).ToArray());

        void Answer()
        {
            if (probe)
            {
                AssertQueryPurity(world, game, runner);
            }
            events.AddRange(game.Resolve(Decision.Take(game.Pending!.Affordances[prevent ? 0 : 1].Id)).Events);
        }
    }
    private static AbilityRunner Runner(bool repeatedDamage, bool secondEnemy)
    {
        // rr:interrupt.3: "before that triggering condition resolves";
        // rr:ability.4: "resolve the ability one sentence at a time".
        // A synthetic distinct following effect observes completion of retaliation.
        const string damage = """{"dealAttackDamage":{"cards":{"query":"villain"},"amount":1}}""";
        string body = repeatedDamage ? "{\"seq\":[" + damage + "," + damage + "]}" : damage;
        if (secondEnemy) body = "{\"seq\":[" + damage
            + ", {\"dealAttackDamage\":{\"cards\":{\"titled\":\"Sandman\"},\"amount\":1}}]}";
        return new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01004","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "effect":{"seq":[
                  {"attack":{"target":{"query":"villain"},"effect":BODY}},
                  {"placeCounters":{"card":"you","counter":"observed","count":1}}
                ]}
              }]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","target":"you"},
                "effect":{"choose":{"options":[
                  {"preventDamage":"trigger.target"},
                  {"draw":{"player":"you","count":1}}
                ]}}
              }]}
            ]}
            """.Replace("BODY", body, StringComparison.Ordinal)));
    }

    private static void AssertQueryPurity(World world, Game game, AbilityRunner runner)
    {
        string digest = world.Digest().Canonical();
        var pending = game.Pending;
        var outstanding = world.Agenda.Outstanding.ToArray();
        long consumed = (long)world.Random.Generator.WordsConsumed;
        for (int index = 0; index < 3; index++) _ = runner.Actions(world, 0);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Same(pending, game.Pending);
        Assert.Equal(outstanding, world.Agenda.Outstanding);
        Assert.Equal(consumed, (long)world.Random.Generator.WordsConsumed);
    }

    private static (Game Game, World World, Card Source, Card Payment, ContinuousEffect Modifier)
        Board(AbilityRunner runner, bool ranged, bool secondEnemy)
    {
        Card? source = null;
        Card? payment = null;
        ContinuousEffect? modifier = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("01004", board.Seats[0].Hand);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
            InPlay(board, "01092");
            modifier = new ContinuousEffect(EffectSource.LastingEffect, Kind: "attackDamage",
                Amount: 1, Affects: source.ObjectId, Lasts: new Duration(Uses: 1));
            board.Effects.Register(modifier);
            if (ranged) board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Kind: "ranged", Amount: 1, Affects: source.ObjectId));
            if (secondEnemy)
            {
                var minion = board.CreateCard("01102", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                    Kind: "retaliate", Amount: 1, Affects: minion.ObjectId));
            }
            var villain = board.TheCardIn(DeckType.VillainArea)!;
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Kind: "retaliate", Amount: 1, Affects: villain.ObjectId));
        }, hero: true, abilities: runner);
        return (game, world, source!, payment!, modifier!);
    }

    private static int ExpectedDamage(bool prevent, bool ranged, bool secondEnemy) =>
        prevent || ranged ? 0 : secondEnemy ? 2 : 1;

}
