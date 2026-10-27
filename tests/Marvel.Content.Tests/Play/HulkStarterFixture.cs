using Marvel.Rules.Timing;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Prepared nonpublic board using the complete actual authored ability book.
internal sealed class HulkStarterFixture
{
    internal World World { get; } = ThorActualFixture.Board("06001a", 2);
    internal AbilityRunner Runner { get; } = AuthoredCards.Runner();
    internal Card Hero => World.Seats[1].IdentityCard;
    internal Card Villain => World.TheCardIn(DeckType.VillainArea)!;
    internal Card Scheme => World.TheCardIn(DeckType.MainSchemesArea)!;
    internal List<GameEvent> Events { get; } = [];

    internal HulkStarterFixture()
    {
        World.Abilities = Runner;
        World.Effects.Register(new(EffectSource.LastingEffect, "health", 30, Affects: Hero.ObjectId));
        Scheme.PlaceTokens("k_threat", 20);
    }

    internal Card Held(string face) => World.CreateCard(face, World.Seats[1].Hand);

    internal Card PlayEvent(string face, Card target, params Card[] payment)
    {
        var source = Held(face);
        var action = Assert.Single(Runner.Actions(World, 1), offer => offer.Card == source.ObjectId);
        var cost = Assert.Single(Runner.Describe(World, action).CostOptions);
        int[] paid = payment.Select(card => card.ObjectId).ToArray();
        var allocation = ResourcePayment.Allocate(cost, paid);
        Assert.NotNull(allocation);
        Events.AddRange(Runner.ExecuteAction(World, new PlayerAction(action, paid, [target.ObjectId], Allocations: allocation)).Events);
        return source;
    }

    internal void Finish(Func<Prompt, Decision?>? decide = null)
    {
        for (int i = 0; i < 80; i++)
        {
            var prompt = Sequence.Work(World, World.Facts, Runner, Events);
            if (prompt is null) break;
            var answer = decide?.Invoke(prompt);
            Assert.True(answer is not null || prompt.Cancellable, prompt.Label);
            Sequence.Answer(World, World.Facts, Runner, prompt, answer ?? Decision.Decline, Events);
        }
        Assert.False(World.Agenda.IsBusy);
        Assert.False(World.IsOver);
    }
}
