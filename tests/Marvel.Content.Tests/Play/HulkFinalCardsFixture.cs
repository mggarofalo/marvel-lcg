using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nonpublic prepared board using the actual production ability book.
internal sealed class HulkFinalCardsFixture
{
    internal readonly World World = ThorActualFixture.Board("06001a", 2);
    internal readonly AbilityRunner Runner;
    internal readonly List<GameEvent> Events = [];
    internal Prompt? Pending;
    internal Card Hero => World.Seats[1].IdentityCard;
    internal Card Villain => World.TheCardIn(DeckType.VillainArea)!;
    internal Card Scheme => World.TheCardIn(DeckType.MainSchemesArea)!;

    internal HulkFinalCardsFixture()
    {
        Runner = new(AuthoredCards.Book);
        World.Abilities = Runner;
        World.MoveToTop(Hero, World.AreaOf(DeckType.RemovedArea));
        World.Seats[1].IdentityCard = World.CreateCard("10001a,10001b", World.Seats[1].Hero);
        Scheme.PlaceTokens("k_threat", 4);
    }

    internal Card Minion(string face = "01103") => World.CreateCard(face,
        World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));

    internal Card Support() => World.CreateCard("10029",
        World.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));

    internal void Start(Card source, int ordinal = 0, params Card[] payment)
    {
        var action = Assert.Single(Runner.Actions(World, 1),
            a => a.Card == source.ObjectId && a.Ordinal == ordinal);
        Events.AddRange(Runner.ExecuteAction(World,
            new PlayerAction(action, payment.Select(c => c.ObjectId).ToArray(), [])).Events);
        Pending = Sequence.Work(World, World.Facts, Runner, Events);
    }

    internal void Answer(Decision answer)
    {
        Sequence.Answer(World, World.Facts, Runner, Pending!, answer, Events);
        Pending = Sequence.Work(World, World.Facts, Runner, Events);
    }

    internal void Finish()
    {
        for (int i = 0; Pending is not null && i < 30; i++)
        {
            Assert.True(Pending.Cancellable, Pending.Label);
            Answer(Decision.Decline);
        }
        Assert.Null(Pending);
        Assert.False(World.Agenda.IsBusy);
        Assert.False(World.IsOver);
    }
}
