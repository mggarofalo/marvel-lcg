using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Rule("rr:damage.step.6")]
    [Fact]
    public void DirectAttackDamageResumesItsFollowingEffectAfterTheDefeatInterrupt()
    {
        // rr:damage.step.6 resolves the interrupt window before damage defeats
        // the minion. The draw following the attack therefore waits for that
        // window, then runs once when the action resumes.
        var runner = DamageContinuationRunner(
            """
            { "chooseCard": {
              "from": { "query": "attackableMinions" },
              "effect": { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            } }
            """);
        Card? source = null;
        Card? minion = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                InPlay(board, "01092");
                minion = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(Damage.Health(board, Cards, minion) - 1);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(Question.Element, game.Pending!.Asking);
        game.Resolve(Decision.Take(minion!.ObjectId));

        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);

        game.Resolve(Decision.Decline);

        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(DeckType.EncounterDiscardPile, minion!.Area.Type);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:indirect-damage.3")]
    [Rule("rr:damage.step.6")]
    [Fact]
    public void AssignedIndirectDamageResumesItsFollowingEffectAfterTheDefeatInterrupt()
    {
        // All indirect damage is assigned before it resolves. When that
        // resolution opens damage step 6, the following draw waits until the
        // defeat interrupt finishes and then runs once.
        var runner = DamageContinuationRunner(
            """
            { "indirectDamage": {
              "among": { "query": "heroesAndAllies" }, "amount": 1
            } }
            """);
        Card? source = null;
        Card? ally = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                InPlay(board, "01092");
                ally = board.CreateCard(
                    AuthoredCards.BlackCat,
                    board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                ally.TakeDamage(Damage.Health(board, Cards, ally) - 1);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var assignment = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(Question.Element, game.Pending.Asking);
        game.Resolve(Decision.Take(assignment.Id, [ally!.ObjectId], []));

        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);

        game.Resolve(Decision.Decline);

        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.False(world.Agenda.IsBusy);
    }

    private static Marvel.Cards.Run.AbilityRunner DamageContinuationRunner(string damage) =>
        new(AbilityCatalog.Parse(
            $$"""
            { "cards": [
              { "card": "01006", "abilities": [{
                "trigger": {
                  "event": "WhenActionTriggered", "timing": "Action", "subject": "game"
                },
                "effect": { "seq": [
                  {{damage}},
                  { "draw": { "player": "you", "count": 1 } }
                ] }
              }] },
              { "card": "01092", "abilities": [{
                "trigger": {
                  "event": "WhenCardWouldBeDefeated", "timing": "Interrupt", "subject": "game"
                },
                "effect": { "draw": { "player": "you", "count": 1 } }
              }] }
            ] }
            """));
}
