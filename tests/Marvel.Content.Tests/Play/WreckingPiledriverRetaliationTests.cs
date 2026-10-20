using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual authored cards in arranged multi-villain games, not scenario admission.</summary>
public sealed class WreckingPiledriverRetaliationTests : CoreCardTestBase
{
    [Rule("rr:villain-defeat.3.1")]
    [Rule("rr:villain-defeat.2")]
    [Rule("rr:retaliate-x.1")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BasicAttackPromotesPiledriverAndHisNewStageRetaliatesOnce(bool active)
    {
        // 07032/07033: "Retaliate 1." Villain defeat: "The new stage of the villain
        // is considered the same character as the defeated stage for purposes of card abilities."
        // "Excess damage ... does not carry over to the new stage."
        // Retaliate: "After this character is attacked, deal X damage to the attacker."
        var groups = new List<EncounterDeckGroup>();
        var (game, world) = Playing(board =>
        {
            foreach (var card in board.Cards.Where(card =>
                card.Area.Type is DeckType.VillainArea or DeckType.VillainDeck).ToArray())
                World.MoveToTop(card, board.AreaOf(DeckType.RemovedArea));
            groups.Add(Group(board, "07032", "07033", "07034"));
            groups.Add(Group(board, "07017", "07018", "07019"));
            board.BindEncounterTopology(new EncounterTopology(board, groups,
                groups[active ? 0 : 1].Villain));
            var target = groups[0].Villain;
            target.TakeDamage(Damage.Health(board, Cards, target) - 1);
        }, hero: true, abilities: AuthoredCards.Runner());
        var original = groups[0].Villain;
        var promoted = groups[0].Stages[1];
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);
        var attack = Assert.Single(game.Pending!.Affordances, option => option.Verb == BasicPowers.AttackVerb);
        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));

        var result = game.Resolve(new Decision(attack.Id, [original.ObjectId], []));

        Assert.Equal(DeckType.RemovedArea, original.Area.Type);
        Assert.Same(promoted, groups[0].CurrentVillain);
        Assert.Equal(0, promoted.Damage);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Single(result.Events.OfType<FieldSet>(), item => item.Verb == "Retaliate"
            && item.Card == world.Seats[0].IdentityCard.ObjectId);
        Assert.Same(groups[active ? 0 : 1], world.EncounterTopology!.Active);
        Assert.NotNull(game.Pending);
    }

    private static EncounterDeckGroup Group(World world, string first, string second, string scheme)
    {
        var villain = world.CreateCard(first, world.AreaOf(DeckType.VillainArea));
        var next = world.CreateCard(second, world.AreaOf(DeckType.VillainDeck));
        return new(villain, world.CreateCard(scheme, world.AreaOf(DeckType.SideSchemesArea)),
            world.CreateArea(DeckType.EncounterDeck), world.CreateArea(DeckType.EncounterDiscardPile),
            [], [villain, next]);
    }
}
