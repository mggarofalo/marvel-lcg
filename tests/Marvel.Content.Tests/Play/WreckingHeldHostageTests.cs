using Marvel.Content.Tests.Cards;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingHeldHostageTests
{
    private static readonly string[] Faces = ["07005", "07021", "07036", "07050"];
    private static readonly string[] StatusCases = ["normal", "stunned", "tough"];
    private static readonly int[] Players = [0, 1];

    public static IEnumerable<object[]> Cases() =>
        Faces.SelectMany((face, owner) =>
            Players.SelectMany(player => StatusCases
                .Select(status => new object[] { face, owner, player, status })));

    [Theory]
    [MemberData(nameof(Cases))]
    [Rule("rr:then.2")]
    [Rule("rr:stun-stunned.7")]
    public void ActionUsesAttachedPairAndDiscardsOnlyAfterCompletedAttack(
        string face, int owner, int player, string status)
    {
        // Held Hostage: "The villain corresponding to the attached side scheme attacks you.
        // Then, discard this card." rr:then.2 requires the preceding effect to resolve fully.
        // rr:stun-stunned.7: a canceled attack is "not considered to have attacked".
        var (world, groups, source) = WreckingEncounterCardFixture.Create(face, owner, players: 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.EncounterTopology!.SelectActive(groups[0].Villain, "setup", []);
        Assert.Equal(new[] { groups[0].Scheme.ObjectId }, runner.AttachmentTargets(world, source));
        Reveal.Resolve(world, world.Facts, source, player, []);
        Assert.Equal(groups[0].Scheme.ObjectId, source.Area.Host);
        Assert.False(runner.CanThwartScheme(world, groups[0].Scheme));
        Assert.True(runner.CanRemoveThreat(world, groups[0].Scheme));
        world.EncounterTopology.SelectActive(groups[1].Villain, "test", []);
        if (status == "stunned") Statuses.Give(world, groups[0].Villain, Statuses.Stunned);
        if (status == "tough") Statuses.Give(world, world.Seats[player].IdentityCard, Statuses.Tough);
        var action = Assert.Single(runner.Actions(world, player), item => item.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Finish(world, runner);
        Assert.Equal(status == "stunned" ? DeckType.UpgradesArea : DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Equal(status == "normal" ? 4 : 0, world.Seats[player].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[1 - player].IdentityCard.Damage);
        Assert.Same(groups[1].Villain, EncounterRouting.Villain(world));
    }

    [Theory]
    [InlineData("07005", 0)]
    [InlineData("07021", 1)]
    [InlineData("07036", 2)]
    [InlineData("07050", 3)]
    public void AlterEgoCannotTakeThePrintedHeroAction(string face, int owner)
    {
        var (world, _, source) = WreckingEncounterCardFixture.Create(face, owner, "01001b");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Reveal.Resolve(world, world.Facts, source, 0, []);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source.ObjectId);
    }

    private static void Finish(World world, AbilityRunner runner)
    {
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) return;
            Assert.True(step < 10);
            Assert.True(prompt.Cancellable);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        }
    }
}
