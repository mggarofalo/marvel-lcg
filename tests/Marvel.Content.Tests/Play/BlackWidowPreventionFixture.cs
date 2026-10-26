using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

internal sealed class BlackWidowPreventionFixture
{
    internal World World { get; } = ThorActualFixture.Board("06001a", 2);
    internal AbilityRunner Runner { get; }
    internal List<GameEvent> Events { get; } = [];
    internal Card Hero => World.Seats[1].IdentityCard;
    internal Card Villain => World.TheCardIn(DeckType.VillainArea)!;

    internal BlackWidowPreventionFixture(bool hero = true, AbilityRunner? runner = null)
    {
        Runner = runner ?? AuthoredCards.Runner();
        World.MoveToTop(Hero, World.AreaOf(DeckType.RemovedArea));
        World.Seats[1].IdentityCard = World.CreateCard("08001a,08001b", World.Seats[1].Hero);
        if (!hero) Hero.TurnTo("08001b");
        World.Abilities = Runner;
    }

    internal Card Upgrade(string face) => World.CreateCard(face,
        World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));

    internal Prompt Work() => Assert.IsType<Prompt>(Sequence.Work(World, World.Facts, Runner, Events));

    internal Prompt Find(Card source)
    {
        for (int count = 0; count < 20; count++)
        {
            var prompt = Work();
            if (prompt.Affordances.Any(a => a.AnchorId == source.ObjectId)) return prompt;
            Answer(prompt, Decision.Decline);
        }
        throw new InvalidOperationException("Expected actual-card offer was not reached");
    }

    internal void Answer(Prompt prompt, Decision decision) =>
        Sequence.Answer(World, World.Facts, Runner, prompt, decision, Events);

    internal void Use(Prompt prompt, Card source) =>
        Answer(prompt, Decision.Take(Assert.Single(prompt.Affordances, a => a.AnchorId == source.ObjectId).Id));

    internal void Finish(params Card[] forbidden) => FinishWithDefender(null, forbidden);

    internal void FinishDefendingWith(Card defender, params Card[] forbidden) => FinishWithDefender(defender, forbidden);

    private void FinishWithDefender(Card? defender, Card[] forbidden)
    {
        for (int count = 0; count < 30; count++)
        {
            var prompt = Sequence.Work(World, World.Facts, Runner, Events);
            if (prompt is null) return;
            Assert.DoesNotContain(prompt.Affordances, a => forbidden.Any(card => a.AnchorId == card.ObjectId));
            Assert.True(prompt.Cancellable);
            var answer = defender is not null && prompt.Asking == Question.Defender
                && prompt.Affordances.Any(a => a.Id == defender.ObjectId)
                ? Decision.Take(defender.ObjectId) : Decision.Decline;
            Answer(prompt, answer);
        }
        throw new InvalidOperationException("Resolution did not finish");
    }
}
