using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class DefenseOwnershipTests : AttackTestBase
{
    [Rule("rr:attack-enemy-activation.step.2")]
    [Fact]
    public void EachControllerPassesOnlyTheirOwnOpportunityInTheSameOccurrence()
    {
        // "If a player wishes to defend, that player exhausts a hero or ally".
        // Helpers-first scheduling is ours, not an ordering rule from the book.
        var facts = Printed(3, 0, 1);
        var world = Board(facts, players: 3);
        var abilities = new NoCardAbilities();
        var first = Sequence.Work(world, facts, abilities, [])!;
        var occurrence = world.Agenda.Occurrence;
        Assert.Equal(1, first.Player);
        Assert.All(first.Affordances, option => Assert.Equal(1, option.AnchorPlayer));
        Assert.Equal("Pass defense opportunity", first.DeclineLabel);
        Assert.Contains("Passing offers the next eligible player a defense opportunity.", first.Description);
        Assert.Contains("against p0", first.Description);
        Sequence.Answer(world, facts, abilities, first, Decision.Decline, []);
        var second = Sequence.Work(world, facts, abilities, [])!;
        Assert.Equal(2, second.Player);
        Assert.Same(occurrence, world.Agenda.Occurrence);
        Assert.True(world.Seats[1].IdentityCard.Ready);
        Assert.Equal(0, world.Attack!.Player);
        Sequence.Answer(world, facts, abilities, second, Decision.Decline, []);
        var self = Sequence.Work(world, facts, abilities, [])!;
        Assert.Equal(0, self.Player);
        Assert.Equal("Leave attack undefended", self.DeclineLabel);
        Assert.Same(occurrence, world.Agenda.Occurrence);
        Sequence.Answer(world, facts, abilities, self,
            Decision.Take(world.Seats[0].IdentityCard.ObjectId), []);
        Sequence.Finish(world, facts, abilities, []);
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.True(world.Seats[1].IdentityCard.Ready);
        Assert.True(world.Seats[2].IdentityCard.Ready);
    }

    [Rule("rr:defend-defense.1")]
    [Rule("rr:defend-defense.3.1")]
    [Fact]
    public void HelperCommitsTheirControlledAllyAndOnlyOneDefense()
    {
        // "that ally becomes the target character ... its controller becomes
        // the target player". Ownership of the physical card is independent.
        var facts = Printed(2, 0);
        var world = Board(facts, players: 2);
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 0));
        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, facts, new NoCardAbilities(), events)!;
        Assert.Equal(1, asked.Player);
        Assert.Contains(asked.Affordances, option => option.AnchorId == ally.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => Attack.Defend(world, facts,
            new NoCardAbilities(), Decision.Take(world.Seats[0].IdentityCard.ObjectId), events));
        Assert.True(ally.Ready);
        Attack.Defend(world, facts, new NoCardAbilities(), Decision.Take(ally.ObjectId), events);
        Assert.Equal(1, world.Attack!.Player);
        Assert.Equal(ally.ObjectId, world.Attack.Target);
        Assert.False(ally.Ready);
        Assert.Throws<RulesNotImplementedException>(() => Attack.Defend(world, facts,
            new NoCardAbilities(), Decision.Take(ally.ObjectId), events));
        Assert.Single(events.OfType<FieldSet>(), change => change.Field == "is_exhaust");
    }

    [Theory]
    [InlineData("exhausted")]
    [InlineData("alter-ego")]
    [InlineData("eliminated")]
    [Rule("rr:defend-defense.2")]
    [Rule("rr:player-elimination.3")]
    public void IneligibleHelpersAreSkipped(string reason)
    {
        // "A hero must exhaust to use this power". An eliminated player takes
        // no further part in the game; an alter-ego has no basic defense.
        var facts = Printed(2, 0);
        var world = Board(facts, players: 2);
        if (reason == "exhausted") world.Seats[1].IdentityCard.Exhaust();
        if (reason == "alter-ego") world.Seats[1].IdentityCard = world.CreateCard("alter", world.Seats[1].Hero);
        if (reason == "eliminated") world.Seats[1].Eliminated = true;
        var asked = Sequence.Work(world, facts, new NoCardAbilities(), [])!;
        Assert.Equal(0, asked.Player);
        Assert.All(asked.Affordances, option => Assert.Equal(0, option.AnchorPlayer));
    }

    [Rule("rr:defend-defense.4.3")]
    [Rule("rr:defend-defense.4.6")]
    [Fact]
    public void ExistingAbilityDefenderAloneMayAddBasicDefenseOrKeepTheirDefense()
    {
        // Defense abilities do not apply DEF; "That hero can still be declared
        // the defender ... during the Declare Defender step". Other players
        // cannot defend after a defense ability establishes the defender.
        var facts = Printed(3, 0, 1);
        var world = Board(facts, players: 2);
        var asked = Sequence.Work(world, facts, new NoCardAbilities(), [])!;
        Attack.BeginDefenseAbility(world, 1);
        asked = Attack.DeclareDefender(world, facts, new NoCardAbilities())!;
        Assert.Equal(1, asked.Player);
        Assert.Equal("Keep current defense", asked.DeclineLabel);
        Assert.Contains("is already defending", asked.Description);
        Assert.Contains("keep the current defense", asked.Description);
        Assert.All(asked.Affordances, option => Assert.Equal(1, option.AnchorPlayer));
        Sequence.Answer(world, facts, new NoCardAbilities(), asked, Decision.Decline, []);
        Sequence.Finish(world, facts, new NoCardAbilities(), []);
        Assert.Equal(3, world.Seats[1].IdentityCard.Damage);
        Assert.True(world.Seats[1].IdentityCard.Ready);
    }
    [Fact]
    public void RequiredDefenseAllowsAnEarlierPassButNotTheLastEligibleController()
    {
        // Synthetic card restriction: at least one legal hero must defend.
        // The restriction contract is existing engine policy, not Core card text.
        var facts = Printed(2, 0);
        var world = Board(facts, players: 2);
        var abilities = new RequiresDefense();
        var helper = Sequence.Work(world, facts, abilities, [])!;
        Assert.Equal(1, helper.Player);
        Assert.True(helper.Cancellable);
        Sequence.Answer(world, facts, abilities, helper, Decision.Decline, []);
        var self = Sequence.Work(world, facts, abilities, [])!;
        Assert.Equal(0, self.Player);
        Assert.False(self.Cancellable);
        Assert.Throws<RulesNotImplementedException>(() =>
            Sequence.Answer(world, facts, abilities, self, Decision.Decline, []));
        Sequence.Answer(world, facts, abilities, self,
            Decision.Take(world.Seats[0].IdentityCard.ObjectId), []);
        Sequence.Finish(world, facts, abilities, []);
        Assert.False(world.Seats[0].IdentityCard.Ready);
    }

    [Fact]
    public void PassingLastHelperDoesNotCreateAnEmptyAttackedSeatConfirmation()
    {
        // Empty opportunities are skipped; the sole helper's pass commits only
        // that player's choice, and the attack continues because nobody can defend.
        var facts = Printed(2, 0);
        var world = Board(facts, players: 2);
        world.Seats[0].IdentityCard.Exhaust();
        var helper = Sequence.Work(world, facts, new NoCardAbilities(), [])!;
        Assert.Equal(1, helper.Player);
        Assert.Equal("Pass; leave attack undefended", helper.DeclineLabel);
        Assert.Contains("No other player can use basic defense.", helper.Description);
        Assert.Contains("Passing leaves this attack undefended.", helper.Description);
        Sequence.Answer(world, facts, new NoCardAbilities(), helper, Decision.Decline, []);
        Sequence.Finish(world, facts, new NoCardAbilities(), []);
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.True(world.Seats[1].IdentityCard.Ready);
    }

    private sealed class RequiresDefense : NoCardAbilities
    {
        public override DefenderChoice Defenders(World world, EnemyAttack attack, IReadOnlyList<Card> candidates)
            => new(candidates, Required: true);
    }

}
