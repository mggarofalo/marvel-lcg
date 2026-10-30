using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Photographic Reflexes (04104) on Taskmaster (04093) when Taskmaster is not the
// attack's first target. rr:attack-player-ability-type.5 considers the attacker "to
// have attacked each of those enemies", and ruling a5ab2d45d8eb09ac: it "is intended
// to prevent all damage Taskmaster would take from the attack", dealing it to the
// attacking identity instead.
public sealed class PhotographicReflexesRecipientTests : CoreCardTestBase
{
    private const string Reflexes = "04104";
    private const string Taskmaster = "04093";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:attack-player-ability-type.5")]
    [Rule("rr:triggering-condition.2")]
    public void ShieldTossReflectsTaskmastersShareWhereverHeIsListed(bool taskmasterFirst)
    {
        // Taskmaster's 4 goes to Captain America; the Hydra Soldier takes its own 4.
        var (game, world, taskmaster, minion, attached) = AttackRecipientFixture.ShieldToss(Reflexes, 1, Taskmaster);
        var attack = Assert.Single(game.Pending!.Affordances);

        game.Resolve(new Decision(attack.Id,
            taskmasterFirst ? [taskmaster.ObjectId, minion.ObjectId] : [minion.ObjectId, taskmaster.ObjectId]));

        Assert.Equal(0, taskmaster.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(4, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, attached[0].Area.Type);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.5")]
    [Rule("rr:interrupt.3")]
    public void GiantStompChoosingTaskmasterReflectsTheEight()
    {
        // The 8 is chosen after the attack begins; Taskmaster is attacked then, and the
        // reflexes resolve before the damage. Giant Ant-Man takes the 8.
        var (world, f, stomp, pay) = AttackRecipientFixture.AntMan();
        World.MoveToTop(f.Villain, world.AreaOf(DeckType.RemovedArea));
        var taskmaster = world.CreateCard(Taskmaster, world.AreaOf(DeckType.VillainArea));
        var reflexes = world.CreateCard(Reflexes, world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, taskmaster.ObjectId));
        AttackRecipientFixture.Execute(f, stomp, pay);

        f.Finish(prompt => Decision.Take(prompt.Affordances.Any(option => option.AnchorId == taskmaster.ObjectId)
            ? taskmaster.ObjectId : prompt.Affordances[0].Id));

        Assert.Equal(0, taskmaster.Damage);
        Assert.Equal(8, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, reflexes.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }
}
