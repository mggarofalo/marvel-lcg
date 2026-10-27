using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Nonpublic boards for actual Kree Fanatic rows; no setup admission.</summary>
internal sealed class RonanRemainingFixture : CoreCardTestBase
{
    internal World World { get; } = new(Cards, 2);
    internal AbilityRunner Runner { get; } = AuthoredCards.Runner();
    internal List<GameEvent> Events { get; } = [];
    internal Card Villain { get; }
    internal Card Main { get; }
    internal RonanRemainingFixture()
    {
        World.Abilities = Runner;
        for (int i = 0; i < 2; i++)
        {
            var seat = World.CreateSeat($"p{i}");
            seat.IdentityCard = World.CreateCard("01001a,01001b", seat.Hero);
            for (int card = 0; card < 8; card++) World.CreateCard("01087", seat.Deck);
        }
        Villain = Card("01094", DeckType.VillainArea);
        Main = Card("01097b", DeckType.MainSchemesArea);
        for (int i = 0; i < 12; i++) Card("01098", DeckType.EncounterDeck);
    }
    internal Card Card(string face, DeckType zone) => World.CreateCard(face, World.AreaOf(zone));
    internal Card Reveal(string face, int player = 1)
    {
        var card = World.CreateCard(face, World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player)));
        World.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: card.ObjectId, Seat: player));
        return card;
    }
    internal void Activate(bool attack, int seat = 1)
    {
        World.Agenda.Add(new PhaseStep(attack ? new EnemyAttackOperation() : new EnemySchemeOperation(),
            1, 2, Subject: Villain.ObjectId, Seat: seat));
    }
    internal void Finish(Func<Prompt, Decision>? answer = null)
    {
        for (int i = 0; i < 50; i++)
        {
            var prompt = Sequence.Work(World, World.Facts, Runner, Events);
            if (prompt is null)
            {
                Assert.Empty(World.Agenda.Outstanding);
                Assert.Null(World.Activation);
                Assert.False(World.IsOver);
                return;
            }
            var decision = answer?.Invoke(prompt) ?? Decision.Decline;
            Assert.True(prompt.Cancellable || decision != Decision.Decline, prompt.Description);
            Sequence.Answer(World, World.Facts, Runner, prompt, decision, Events);
        }
        Assert.Fail("Actual Ronan encounter did not finish");
    }
}
