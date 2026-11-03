using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Session.Tests;

public sealed class ReplayHistoryActionTests
{
    [Theory]
    [InlineData("phase_step", "Offered label")]
    [InlineData("turn_action", "synthetic-identity")]
    [InlineData("terminal", "synthetic-identity")]
    public void AnAnchoredRootIsNamedByItsOfferOnlyInAPhaseStep(string role, string expected)
    {
        // History presentation choice: a phase step is named by the offer taken, any
        // other unit by the card that anchors it. The role is read when the unit
        // completes, because a game-ending decision renames its unit "terminal".
        // Synthetic fixture; no card behavior or tabletop timing is asserted.
        var facts = new TitleIsFaceFacts();
        var world = new World(facts, 1);
        Seat seat = world.CreateSeat("Player");
        seat.IdentityCard = world.CreateCard("synthetic-identity", seat.Hero);
        Game game = Game.BeginWithoutCardAbilities(world, facts);
        var offer = new Affordance(41, "Synthetic", seat.IdentityCard.ObjectId, 0, "Offered label");
        Prompt prompt = game.Pending! with { Affordances = [offer] };
        Decision decision = Decision.Take(offer.Id);
        JournalStep step = JournalStep.From(0, prompt, decision, [], 0, "synthetic state");
        var unit = new JournalUnit(role, "complete", 0, 0, 0, "Mulligan", [step], []);
        var history = new ReplayHistory();
        history.ObserveRoot(0, game, prompt, decision);
        var units = new List<HistoryUnitInspection>();

        history.Append(units, unit, 0);

        HistoryUnitInspection inspected = Assert.Single(units);
        Assert.Equal(expected, inspected.Action);
        Assert.Equal(role, inspected.Role);
        Assert.Equal(seat.IdentityCard.ObjectId, inspected.Subject);
    }

    private sealed class TitleIsFaceFacts : ICardFacts
    {
        public CardKind Kind(string faceId) => CardKind.Hero;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
