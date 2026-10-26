using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual authored rows; Black Widow remains outside public admission.
internal sealed class CounterattackReceiptFixture
{
    internal World World { get; } = BlackWidowEventCompositionFixture.Board();
    internal AbilityRunner Runner { get; }
    internal Card Hero => World.Seats[1].IdentityCard;
    internal Card Counter { get; }
    internal Card Enemy { get; }
    internal List<GameEvent> Events { get; } = [];

    internal CounterattackReceiptFixture(bool villain = false, string? additionalRow = null)
    {
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        if (additionalRow is not null) book["cards"]!.AsArray().Add(JsonNode.Parse(additionalRow));
        Runner = new(AbilityCatalog.Parse(book.ToJsonString()));
        World.Abilities = Runner;
        Counter = World.CreateCard("08030",
            World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        Enemy = villain ? World.TheCardIn(DeckType.VillainArea)!
            : World.CreateCard("01103", World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
    }

    internal void Attack(int seat = 1) => World.Agenda.Add(new PhaseStep(
        new EnemyAttackOperation { DealBoostCards = false }, 1, 2, Subject: Enemy.ObjectId, Seat: seat));

    internal Prompt CounterPrompt()
    {
        for (int index = 0; index < 40; index++)
        {
            var prompt = Sequence.Work(World, World.Facts, Runner, Events);
            Assert.NotNull(prompt);
            if (prompt.Affordances.Any(offer => offer.AnchorId == Counter.ObjectId)) return prompt;
            Assert.True(prompt.Cancellable);
            Answer(prompt, Decision.Decline);
        }
        throw new InvalidOperationException("No Counterattack opportunity");
    }

    internal void Answer(Prompt prompt, Decision answer) =>
        Sequence.Answer(World, World.Facts, Runner, prompt, answer, Events);

    internal int Finish(bool use = true, int defender = -1)
    {
        int offers = 0;
        for (int index = 0; index < 60; index++)
        {
            var prompt = Sequence.Work(World, World.Facts, Runner, Events);
            if (prompt is null) break;
            var offer = prompt.Affordances.SingleOrDefault(a => a.AnchorId == Counter.ObjectId);
            if (offer is not null) offers++;
            Assert.True(prompt.Cancellable);
            Answer(prompt, offer is not null && use ? Decision.Take(offer.Id)
                : prompt.Asking == Question.Defender && prompt.Player == 1 && defender >= 0
                    ? Decision.Take(defender) : Decision.Decline);
        }
        Assert.False(World.Agenda.IsBusy);
        Assert.False(World.IsOver);
        return offers;
    }
}
