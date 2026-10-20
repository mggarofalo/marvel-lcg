using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CountedAttackVillainStageTests
{
    [Rule("rr:villain-defeat.3.1")]
    [Rule("rr:attack-player-ability-type.5.1")]
    [Fact]
    public void PromotedVillainRemainsTheAttackedCharacterForRetaliation()
    {
        // Same-title stages represent "the same character"; a surviving attacked
        // enemy retaliates after the attack resolves. A stage change is not a
        // returning minion copy. The attack body is synthetic; Zola's stage
        // transition and Test Subjects reveal use the authored scenario rules.
        var runner = Runner("01003", "Action", """
            {"attack":{"target":{"query":"attackableEnemies"},"count":2,"effect":{
              "dealAttackDamage":{"cards":{"query":"powerTargets"},"amount":1,"simultaneous":1}
            }}}
            """, includeAuthored: true);
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("01003", board.Seats[0].Hand);
            foreach (var minion in board.Cards.Where(card =>
                board.Facts.Kind(card.FaceId) == CardKind.Minion && DeckTypes.IsInPlay(card.Area.Type)).ToArray())
                World.MoveToTop(minion, board.AreaOf(DeckType.EncounterDiscardPile));
            board.TheCardIn(DeckType.VillainArea)!.TakeDamage(11);
        }, hero: true, scenario: "zola", abilities: runner);
        var first = world.TheCardIn(DeckType.VillainArea)!;
        Assert.Equal("04109", first.FaceId);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        game.Resolve(new Decision(Assert.Single(game.Pending!.Affordances).Id, [first.ObjectId], []));
        Assert.StartsWith("04110:", game.Pending!.Label, StringComparison.Ordinal);
        Assert.All(game.Pending.Affordances, option => Assert.Equal("04123", option.Label));
        game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));
        while (game.Pending is { Asking: Question.Opportunity }) game.Resolve(Decision.Decline);
        Assert.Equal("04110", world.TheCardIn(DeckType.VillainArea)!.FaceId);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
    }
}
