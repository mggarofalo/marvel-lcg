using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual modular rows, outside public Green Goblin admission.</summary>
public sealed class RunningInterferenceCardTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryPlayerChoosesPaymentOrThreatInEitherForm(bool hero)
    {
        var f = RedSkullFixture.Create(hero: hero, players: 2);
        if (hero) f.World.Seats[1].IdentityCard.TurnTo("01010a");
        foreach (var seat in f.World.Seats)
            foreach (var card in seat.Hand.Cards.ToArray()) World.MoveToTop(card, seat.Deck);
        var mental = f.World.CreateCard("01089", f.World.Seats[0].Hand);
        var physical = f.World.CreateCard("01090", f.World.Seats[0].Hand);
        var source = f.Reveal("02046", 1);
        var choices = new List<int>();
        f.Finish(prompt =>
        {
            choices.Add(prompt.Player);
            var option = prompt.Player == 0
                ? prompt.Affordances.Single(item => item.CostOptions.Count > 0)
                : prompt.Affordances[^1];
            if (prompt.Player == 1) return Decision.Take(option.Id);
            int[] spent = [mental.ObjectId, physical.ObjectId];
            return new Decision(option.Id, [], spent,
                Allocations: ResourcePayment.Allocate(option.CostOptions[0], spent));
        });
        Assert.Equal([0, 1], choices);
        Assert.Equal(DeckType.SideSchemesArea, source.Area.Type);
        Assert.Equal(4, source.Tokens["k_threat"]); // Printed one per player plus one player's two.
        Assert.Equal(DeckType.DiscardPile, mental.Area.Type);
        Assert.Equal(DeckType.DiscardPile, physical.Area.Type);
    }

    [Theory]
    [InlineData(0, "01001a", false, false, true)]
    [InlineData(1, "01010a", false, false, true)]
    [InlineData(1, "01010b", false, false, true)]
    [InlineData(1, "01010a", true, false, false)]
    [InlineData(1, "01010a", false, true, false)]
    [Rule("rr:wild-resource.3")]
    public void TombstoneRequiresActualIdentityDamageAndDiscardsOnlyPrintedMentalOrPhysical(
        int player, string form, bool tough, bool allyDefense, bool discards)
    {
        // "Outside of generating resources to pay costs, wild resources cannot be interpreted"
        // as other types; Tombstone's "attacks and damages you" requires identity damage.
        var f = RedSkullFixture.Create(players: 2);
        var world = f.World;
        world.Seats[player].IdentityCard.TurnTo(form);
        foreach (var card in world.Seats[player].Hand.Cards.ToArray()) World.MoveToTop(card, world.Seats[player].Deck);
        var mental = world.CreateCard("01089", world.Seats[player].Hand);
        var wild = world.CreateCard("01011", world.Seats[player].Hand);
        var energy = world.CreateCard("01088", world.Seats[player].Hand);
        var minion = world.CreateCard("02047", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)));
        var ally = allyDefense ? world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea,
            PlayArea.Of(player), cardOwner: player)) : null;
        if (tough) Statuses.Give(world, world.Seats[player].IdentityCard, Statuses.Tough);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: minion.ObjectId, Seat: player));
        int choices = 0;
        for (int step = 0; step < 30; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, f.Runner, []);
            if (prompt is null) break;
            Decision answer;
            if (prompt.Asking == Question.Element)
            {
                choices++;
                Assert.Equal(player, prompt.Player);
                var offer = Assert.Single(prompt.Affordances);
                Assert.Contains(mental.ObjectId, offer.Targets!.Legal);
                Assert.DoesNotContain(wild.ObjectId, offer.Targets.Legal);
                Assert.DoesNotContain(energy.ObjectId, offer.Targets.Legal);
                answer = Decision.Take(offer.Id, [mental.ObjectId], []);
            }
            else
            {
                answer = AnswerAttack(prompt, ally, player);
            }
            Sequence.Answer(world, world.Facts, f.Runner, prompt, answer, []);
        }
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(discards ? 1 : 0, choices);
        Assert.Equal(discards ? DeckType.DiscardPile : DeckType.HandsArea, mental.Area.Type);
        Assert.Same(world.Seats[player].Hand, wild.Area);
        Assert.Same(world.Seats[player].Hand, energy.Area);
        Assert.Equal(discards ? 3 : 0, world.Seats[player].IdentityCard.Damage);
    }

    private static Decision AnswerAttack(Prompt prompt, Card? ally, int player)
    {
        Assert.True(prompt.Cancellable, prompt.Description);
        return ally is not null && prompt.Asking == Question.Defender && prompt.Player == player
            ? Decision.Take(ally.ObjectId) : Decision.Decline;
    }

}
