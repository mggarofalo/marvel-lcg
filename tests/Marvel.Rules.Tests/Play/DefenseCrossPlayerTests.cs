using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class DefenseCrossPlayerTests : AttackTestBase
{
    [Rule("rr:defend-defense.1")]
    [Fact]
    public void EachPlayersCharactersAreOfferedToTheirController()
    {
        // "Only one player at a time can defend". The scheduling policy is
        // ours: a helper passes before the attacked player chooses for themself.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed, players: 2);
        var asked = Sequence.Work(world, printed, new NoCardAbilities(), []);
        Assert.NotNull(asked);
        Assert.Equal(1, asked.Player);
        Assert.Equal(world.Seats[1].IdentityCard.ObjectId, Assert.Single(asked.Affordances).AnchorId);
        Sequence.Answer(world, printed, new NoCardAbilities(), asked, Decision.Decline, []);
        var self = Sequence.Work(world, printed, new NoCardAbilities(), [])!;
        Assert.Equal(0, self.Player);
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, Assert.Single(self.Affordances).AnchorId);
    }

    [Rule("rr:defend-defense.3")]
    [Fact]
    public void ACardCanRequireOnePlayersAllyToDefendIfAble()
    {
        // "Must defend ... with an ally they control, if able" narrows both
        // halves of the ordinary defense question: the other legal characters
        // are absent and declining is no longer an answer.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed, players: 2);
        var required = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var abilities = new RequiresAlly(0);
        var asked = Sequence.Work(world, printed, abilities, []);
        Assert.NotNull(asked);
        Assert.False(asked.Cancellable);
        Assert.Equal(required.ObjectId, Assert.Single(asked.Affordances).AnchorId);
        Assert.Throws<RulesNotImplementedException>(() => Attack.Defend(world, printed, abilities, Decision.Decline, []));
    }

    [Rule("rr:attack-enemy-activation.4")]
    [Fact]
    public void TheOrdinaryOptionalDefenseReturnsWhenTheRequiredAllyIsNotAble()
    {
        // "If able" ends the card's requirement when its matching ally is not
        // ready. The normal attack rule then permits any legal defender or no
        // defender, rather than making the whole step impossible.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed, players: 2);
        var exhausted = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        exhausted.Exhaust();
        var abilities = new RequiresAlly(0);
        var asked = Sequence.Work(world, printed, abilities, []);
        Assert.NotNull(asked);
        Assert.True(asked.Cancellable);
        Assert.Equal(world.Seats[1].IdentityCard.ObjectId, Assert.Single(asked.Affordances).AnchorId);
        Sequence.Answer(world, printed, abilities, asked, Decision.Decline, []);
        var self = Sequence.Work(world, printed, abilities, [])!;
        Assert.True(self.Cancellable);
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, Assert.Single(self.Affordances).AnchorId);
    }

    [Rule("rr:defend-defense.5")]
    [Rule("rr:defend-defense.5.1")]
    [Rule("rr:defend-defense.5.2")]
    [Rule("rr:attack-enemy-activation.1.3")]
    [Rule("rr:attack-enemy-activation.2")]
    [Fact]
    public void DefendingForAnotherPlayerMakesYouTheTarget()
    {
        // "If a player other than the attacked player defends the attack with a
        // character they control, that player becomes the new target of that
        // attack." A hero was declared, so the damage is dealt to that hero.
        // **Both** the target character and the target player move.
        var printed = Printed(atk: 5, boost: 0, def: 2);
        var world = Board(printed, players: 2);
        var rescuer = world.Seats[1].IdentityCard;
        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, printed, new NoCardAbilities(), events);
        Sequence.Answer(world, printed, new NoCardAbilities(), asked!, Decision.Take(rescuer.ObjectId), events);
        // **The target *player* moved, not just the character.** The attack is
        // now against seat 1, which is what `rr:defend-defense.5` says outright
        // and what `.5.2` needs -- "any constant or boost abilities that refer
        // to 'you' refer to the defending player".
        //
        // Asserted on the state because nothing reads it yet: `.5.1` splits
        // "when [enemy] attacks you" from "after [enemy] attacks you", and the
        // first is the window that already opened. No ability triggers on the
        // second.
        Assert.Equal(1, world.Attack!.Player);
        Assert.Equal(rescuer.ObjectId, world.Attack.Target);
        Assert.Equal(1, world.Activation!.Player);
        Sequence.Finish(world, printed, new NoCardAbilities(), events);
        // ATK 5 less the rescuer's own DEF 2. The player it was aimed at takes
        // nothing at all.
        Assert.Equal(3, rescuer.Damage);
        Assert.False(rescuer.Ready);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.True(world.Seats[0].IdentityCard.Ready);
    }
}
