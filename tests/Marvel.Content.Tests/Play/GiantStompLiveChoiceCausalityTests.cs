using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class GiantStompLiveChoiceCausalityTests
{
    private const string Tail = """{"attackAnyEnemy":{"amount":8}}""";

    [Fact]
    public void LiveChoiceRetainsUninterruptedAttackDamageCausality()
    {
        var actual = DamageEvents(Tail, true);
        var expected = DamageEvents("""{"dealAttackDamage":{"cards":{"query":"villain"},"amount":8}}""", false);
        Assert.Equal(2, actual.Count);
        Assert.Equal(expected, actual);
    }

    private static List<(int Card, long? From, long? To, string Trigger, string Verb,
        AttackIdentity? AttackIdentity, EventCause? Cause)> DamageEvents(string tail, bool targetless)
    {
        var runner = GiantStompCompositionTests.Runner(tail, targetless);
        var world = GiantStompCompositionTests.Board(runner, 1);
        var source = world.Seats[0].Hand.Cards.Single(card => card.FaceId == "12003");
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        var payment = world.Seats[0].Hand.Cards.Where(card => card.FaceId is "12021" or "12023")
            .Select(card => card.ObjectId).ToArray();
        var events = runner.ExecuteAction(world, new PlayerAction(action, payment, [])).Events.ToList();
        for (int i = 0; i < 30; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            var villain = world.TheCardIn(DeckType.VillainArea)!;
            var answer = prompt.Affordances.FirstOrDefault(option => option.Id == villain.ObjectId);
            Sequence.Answer(world, world.Facts, runner, prompt,
                answer is null ? Decision.Take(prompt.Affordances[0].Id) : Decision.Take(answer.Id), events);
        }
        Assert.False(world.Agenda.IsBusy);
        if (targetless)
        {
            var boundary = Assert.Single(events.OfType<AttackBoundary>(), item => !item.Started);
            Assert.Equal(-1, boundary.Target);
            Assert.Equal(world.Seats[0].IdentityCard.ObjectId, boundary.Source);
        }
        return events.OfType<FieldSet>().Where(item => item.Field == "health" && item.AttackIdentity is not null)
            .Select(item => (item.Card, item.From, item.To, item.Trigger, item.Verb, item.AttackIdentity, item.Cause)).ToList();
    }

}
