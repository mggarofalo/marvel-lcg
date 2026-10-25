using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaHelmetAttackTests
{
    [Fact]
    [Rule("rr:damage.step.6")]
    [Rule("rr:retaliate-x.2")]
    public void SavedCaptainCompletesEnemyAttackAndRetaliatesAfterHelmetDiscard()
    {
        // rr:damage.step.6: "Abilities that trigger “when [character] would be defeated...”"
        // rr:retaliate-x.2: "The character with retaliate X must be in play after the attack resolves to deal this damage."
        Card? helmet = null;
        var (_, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            board.Seats[0].IdentityCard.TakeDamage(10);
            helmet = board.CreateCard("03008", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            board.CreateCard("03009", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        });
        var captain = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), Round: 1, Number: 2,
            Subject: villain.ObjectId, Seat: 0));
        bool used = false;
        for (int answered = 0; Sequence.Work(world, world.Facts, world.Abilities, events) is { } prompt; answered++)
        {
            Assert.True(answered < 12);
            var save = prompt.Affordances.SingleOrDefault(item => item.AnchorId == helmet!.ObjectId);
            if (save is not null)
            {
                Assert.False(used);
                Assert.Equal(0, villain.Damage);
                Assert.True(captain.Damage >= 11);
                used = true;
            }
            else Assert.True(prompt.Asking is Question.Defender or Question.Opportunity);
            Sequence.Answer(world, world.Facts, world.Abilities, prompt,
                save is null ? Decision.Decline : Decision.Take(save.Id), events);
        }
        Assert.True(used);
        Assert.False(world.Seats[0].Eliminated);
        Assert.Equal(10, captain.Damage);
        Assert.Equal(1, villain.Damage);
        Assert.Single(events.OfType<AttackCompleted>());
        int discarded = events.FindIndex(item => item is CardsMoved moved && moved.Cards.Any(landing => landing.Card == helmet!.ObjectId));
        int retaliation = events.FindIndex(item => item is FieldSet set && set.Card == villain.ObjectId && set.Field == "health");
        Assert.True(discarded >= 0 && retaliation > discarded);
    }
}
