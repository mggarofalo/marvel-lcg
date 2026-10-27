using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Printed 90002 behavior in a test-local book, without public pack admission.</summary>
public sealed class RonanJudgeCompositionProbeTests
{
    [Theory]
    [InlineData("WhenAttackEnds", 0, true)]
    [InlineData("WhenAttackEnds", 1, true)]
    [InlineData("WhenDamageDealt", 0, true)]
    [InlineData("WhenDamageDealt", 1, true)]
    [InlineData("WhenAttackEnds", 0, false)]
    [InlineData("WhenAttackEnds", 1, false)]
    [Rule("rr:attack-enemy-activation.step.6.a")]
    [Rule("rr:triggering-condition.2")]
    public void FriendlyDefeatByEnemyAttackAddsTwoThreatOnce(string triggerEvent, int controller, bool lethal)
    {
        // "after [character] attacks [and damages/defeats] [you/an ally]..."
        // "a single attack causing a character to both take damage and be defeated"
        // Printed 90002: "After a friendly character is defeated by an enemy attack,
        // place 2 threat on the main scheme."
        var f = new RonanJudgeProbeFixture(triggerEvent);
        var ally = f.Ally(controller, lethal);
        var response = f.AttackUntilResponse(ally);
        Assert.Equal(Question.Opportunity, response.Asking);
        Assert.Single(response.Affordances, option => option.AnchorId == f.Observer.ObjectId);
        var occurrence = f.World.Agenda.Occurrence!;
        Assert.Contains("WhenAttackEnds", occurrence.Conditions);
        Assert.Contains("WhenDamageDealt", occurrence.Conditions);
        var damage = Assert.Single(occurrence.ReceivedAttackDamage);
        Assert.Equal(f.Villain.ObjectId, damage.Attacker.Card);
        Assert.Equal(ally.ObjectId, damage.Recipient.Card);
        Assert.Equal(controller, damage.Recipient.Controller);
        Assert.Equal(2, damage.Amount);
        if (lethal)
        {
            var defeat = Assert.Single(occurrence.Defeats);
            Assert.Equal(ally.ObjectId, defeat.Card);
            Assert.Equal(damage.Recipient.Incarnation, defeat.Incarnation);
            Assert.Equal(controller, defeat.Controller);
        }
        else Assert.Empty(occurrence.Defeats);
        Assert.True(occurrence.DamageFromAttack);
        Assert.Equal(1, f.Observer.Tokens.GetValueOrDefault("c_received"));
        Assert.Equal(lethal ? 1 : 0, f.Observer.Tokens.GetValueOrDefault("c_defeatInterrupt"));
        Assert.Equal(lethal ? DeckType.DiscardPile : DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(lethal ? 2 : 0, f.Main.Tokens.GetValueOrDefault("k_threat"));
        f.Finish();
        var completed = Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.Equal(ally.ObjectId, completed.Defender);
        Assert.Equal(DeckType.SideSchemesArea, f.Judge.Area.Type);
        Assert.Equal(lethal ? 2 : 0, f.Main.Tokens.GetValueOrDefault("k_threat"));
    }
}
