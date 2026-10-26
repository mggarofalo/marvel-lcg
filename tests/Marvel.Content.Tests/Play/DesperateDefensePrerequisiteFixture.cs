using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic actual-text 09015 candidate; this does not admit the card to the production book.</summary>
internal static class DesperateDefensePrerequisiteFixture
{
    internal static AbilityRunner Runner()
    {
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        book["cards"]!.AsArray().Add(JsonNode.Parse("""
            {"card":"09015","abilities":[{
              "trigger":{"event":"WhenHeroDefends","timing":"Interrupt","subject":"you","form":"hero"},
              "labels":["defense"],
              "effect":{"seq":[
                {"grantUntil":{"card":"you","keyword":"defense","amount":2,"until":"EndOfAttack"}},
                {"readyUndamagedDefender":"you"}
              ]}}]}
            """));
        return new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
    }

    internal static (World World, AbilityRunner Runner, Card Source, Card Payment, Card Enemy) Board(
        int owner = 0, int attack = 4, bool boost = false)
    {
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = Runner();
        world.Abilities = runner;
        var source = world.CreateCard("09015", world.Seats[owner].Hand);
        var payment = world.CreateCard("01088", world.Seats[owner].Hand);
        var enemy = boost ? world.TheCardIn(DeckType.VillainArea)!
            : world.CreateCard("01103", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(owner)));
        long printed = StateFields.Modified(world, enemy, "attack", world.Facts, world.Players);
        world.Effects.Register(new(EffectSource.LastingEffect, "attack", attack - printed, Affects: enemy.ObjectId));
        if (boost) world.CreateCard("01154", world.AreaOf(DeckType.EncounterDeck));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation { DealBoostCards = boost },
            1, 1, Subject: enemy.ObjectId, Seat: owner));
        return (world, runner, source, payment, enemy);
    }

    internal static int Drain(World world, AbilityRunner runner, Card source, Card payment,
        int defender, int defenderSeat, List<GameEvent> events)
    {
        int played = 0;
        for (int index = 0; index < 40; index++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null)
            {
                Assert.False(world.Agenda.IsBusy);
                return played;
            }
            var offer = prompt.Affordances.SingleOrDefault(item => item.AnchorId == source.ObjectId);
            var answer = offer is not null ? Decision.Take(offer.Id, [], [payment.ObjectId])
                : prompt.Asking == Question.Defender && prompt.Player == defenderSeat ? Decision.Take(defender)
                : Decision.Decline;
            if (offer is not null) played++;
            Sequence.Answer(world, world.Facts, runner, prompt, answer, events);
        }
        throw new InvalidOperationException("Desperate Defense fixture did not drain");
    }
}
