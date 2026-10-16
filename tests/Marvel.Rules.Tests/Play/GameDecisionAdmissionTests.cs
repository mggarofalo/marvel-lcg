using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class GameDecisionAdmissionTests
{
    [Fact]
    public void InvalidAnswersLeaveTheCurrentDecisionAndWorldUntouched()
    {
        // Synthetic API contract: malformed answers have no game occurrence.
        var facts = new Printed();
        var world = new World(facts, 2);
        for (int player = 0; player < 2; player++)
        {
            Seat seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("identity", seat.Hero);
        }
        Game game = Game.BeginWithoutCardAbilities(world, facts);
        var prompt = game.Pending!;
        int offered = Assert.Single(prompt.Affordances).Id;
        string digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        Decision[] invalid =
        [
            Decision.Decline,
            Decision.Take(offered + 1),
            new Decision(offered, [world.Seats[1].IdentityCard.ObjectId]),
            new Decision(offered, [], [world.Seats[0].IdentityCard.ObjectId]),
            new Decision(offered, [], Values: new Dictionary<string, long> { ["invented"] = 1 }),
        ];
        foreach (Decision answer in invalid)
        {
            Assert.Throws<ArgumentException>(() => game.Resolve(answer));
            Assert.Same(prompt, game.Pending);
            Assert.Equal(digest, world.Digest().Canonical());
            Assert.Equal(words, world.Random.Generator.WordsConsumed);
            Assert.Equal(GamePhase.Mulligan, game.Phase);
            Assert.Equal(0, game.Round);
        }
        var accepted = game.Resolve(Decision.Take(offered));
        Assert.Equal(1, accepted.Prompt!.Player);
        Assert.Equal(GamePhase.Mulligan, game.Phase);
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => CardKind.AlterEgo;

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>();

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
