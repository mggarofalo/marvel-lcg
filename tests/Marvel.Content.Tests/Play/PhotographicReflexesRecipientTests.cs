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
    private static readonly string[] Resources = ["01087", "01088", "01089"];

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:attack-player-ability-type.5")]
    [Rule("rr:triggering-condition.2")]
    public void ShieldTossReflectsTaskmastersShareWhereverHeIsListed(bool taskmasterFirst)
    {
        // Taskmaster's 4 goes to Captain America; Sandman (01102, 4 hit points, placed
        // without his tough status card) takes his own 4.
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

        var events = f.Finish(prompt => Decision.Take(prompt.Affordances.Any(option => option.AnchorId == taskmaster.ObjectId)
            ? taskmaster.ObjectId : prompt.Affordances[0].Id));

        Assert.Contains(events.OfType<Marvel.Rules.Events.AttackBoundary>(),
            boundary => boundary.Started && boundary.Target == taskmaster.ObjectId);
        Assert.Equal(0, taskmaster.Damage);
        Assert.Equal(8, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, reflexes.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:attack-player-ability-type.5")]
    [Rule("rr:interrupt.3")]
    public void MeleesAnotherEnemyMayBeTaskmaster(bool reflexes)
    {
        // Melee (05030): "Deal 3 damage to an enemy. Deal 3 damage to another enemy."
        // Shocker (3 hit points) is the attack's enemy and is defeated; Taskmaster,
        // chosen as "another enemy", goes through the same later-recipient path
        // as Giant Stomp's choice and is attacked when
        // chosen, so the reflexes send his 3 to Captain Marvel.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        World.MoveToTop(f.Villain, world.AreaOf(DeckType.RemovedArea));
        var taskmaster = world.CreateCard(Taskmaster, world.AreaOf(DeckType.VillainArea));
        var attached = reflexes
            ? world.CreateCard(Reflexes, world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, taskmaster.ObjectId)) : null;
        var shocker = KangTemporalFixture.Engaged(world, "01103", 0);
        var hand = world.Seats[0].Hand;
        var melee = world.CreateCard("05030", hand);
        int[] pay = [.. Resources.Select(face => world.CreateCard(face, hand).ObjectId)];
        var action = Assert.Single(f.Runner.Actions(world, 0), ability => ability.Card == melee.ObjectId);
        f.Runner.ExecuteAction(world, new PlayerAction(action, pay, []));
        var choices = new Queue<int>([shocker.ObjectId, taskmaster.ObjectId]);

        f.Finish(prompt => prompt.Affordances.Any(option => choices.Count > 0 && option.AnchorId == choices.Peek())
            ? Decision.Take(choices.Dequeue()) : Decision.Take(prompt.Affordances[0].Id));

        Assert.Equal(DeckType.EncounterDiscardPile, shocker.Area.Type);
        Assert.Equal(reflexes ? 0 : 3, taskmaster.Damage);
        Assert.Equal(reflexes ? 3 : 0, world.Seats[0].IdentityCard.Damage);
        if (attached is not null) Assert.Equal(DeckType.EncounterDiscardPile, attached.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }
}
