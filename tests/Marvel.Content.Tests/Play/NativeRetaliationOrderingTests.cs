using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class NativeRetaliationOrderingTests
{
    [Rule("rr:attack-player-ability-type.step.7")]
    [Rule("rr:retaliate-x.1")]
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void PrintedForcedResponseAndRetaliateOnSameEnemyCanResolveInEitherOrder(bool nativeFirst, bool labelled)
    {
        // rr:attack-player-ability-type.step.7: "Forced abilities (such as the retaliate keyword)"
        // resolve "in any order". Synthetic printed damage distinguishes their actual execution order.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01094","abilities":[{
              "trigger":{"event":"WhenAttackEnds","timing":"ForcedResponse","target":"this"},
              "effect":{"dealDamage":{"cards":"you","amount":1}}}]},
              {"card":"01007","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "effect":{"seq":[
                  {"attack":{"target":{"query":"villain"},"effect":{"dealAttackDamage":{"cards":{"query":"villain"},"amount":2}}}},
                  {"placeCounters":{"card":"this","counter":"observed","count":1}}]}}]}]}
            """));
        var (game, world) = Playing(board =>
        {
            if (labelled) InPlay(board, "01007");
            var target = board.TheCardIn(DeckType.VillainArea)!;
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                "retaliate", 2, Affects: target.ObjectId));
        }, hero: true, abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var attack = Assert.Single(game.Pending!.Affordances, option => labelled ? option.Verb == Game.ActionVerb : option.Verb == BasicPowers.AttackVerb);
        game.Resolve(new Decision(attack.Id, labelled ? [] : [villain.ObjectId], []));
        Assert.Equal(Question.Order, game.Pending!.Asking);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        AssertFollowingEffect(world, labelled, 0);
        Assert.Equal(2, game.Pending.Affordances.Count);
        Assert.Equal(2, game.Pending.Affordances.Select(option => option.Id).Distinct().Count());
        Assert.All(game.Pending.Affordances, option => Assert.Equal(villain.ObjectId, option.AnchorId));
        var chosen = Assert.Single(game.Pending.Affordances, option => (option.Verb == "Retaliate") == nativeFirst);
        var result = game.Resolve(Decision.Take(chosen.Id));
        var damage = result.Events.OfType<FieldSet>().Where(item => item.Card == world.Seats[0].IdentityCard.ObjectId
            && item.Field == "health").Select(item => item.From - item.To).ToArray();
        Assert.Equal(nativeFirst ? new long?[] { 2, 1 } : [1, 2], damage);
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        AssertFollowingEffect(world, labelled, 1);
    }
    private static void AssertFollowingEffect(World world, bool labelled, int expected)
    {
        if (!labelled) return;
        var support = world.Cards.Single(card => card.FaceId == "01007" && DeckTypes.IsInPlay(card.Area.Type));
        Assert.Equal(expected, support.Tokens.GetValueOrDefault("c_observed"));
    }
}
