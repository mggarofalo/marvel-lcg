using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.View.Tests;

// The attack facts a prompt carries reach a viewer only when both cards are readable to it, and are named there.
public sealed class AttackFactsProjectionTests
{
    [Fact]
    public void AReadableAttackIsNamedInTheDecisionFrame()
    {
        World world = VisibilityFixture.Board();
        Card villain = world.TheCardIn(DeckType.VillainArea)!;
        Card hero = world.Areas.First(area => area.Type == DeckType.AlliesArea).Cards[0];
        Prompt prompt = Defense(villain.ObjectId, hero.ObjectId);
        VisibleResult visible = WorldProjection.For(world, prompt, [], new RestrictedVisibilityPolicy(0).Authorize(null, 2));

        Assert.Equal(prompt.Attack, visible.Prompt!.Attack);
        DecisionAttack attack = Assert.IsType<DecisionAttack>(PromptPresentation.From(visible.Prompt, visible.World).Frame.Attack);
        Assert.Equal((Title(visible.World, villain.ObjectId), Title(visible.World, hero.ObjectId), 3L, false, true),
            (attack.Attacker, attack.Target, attack.Value, attack.Calculated, attack.BoostUnresolved));
    }

    [Fact]
    public void AnAttackOnAConcealedCardIsRemoved()
    {
        World world = VisibilityFixture.Board();
        Card villain = world.TheCardIn(DeckType.VillainArea)!;
        int secret = world.Seats[1].Hand.Cards[0].ObjectId;
        VisibleResult visible = WorldProjection.For(world, Defense(villain.ObjectId, secret), [],
            new RestrictedVisibilityPolicy(0).Authorize(null, 2));

        Assert.Null(visible.Prompt!.Attack);
        Assert.Null(PromptPresentation.From(visible.Prompt, visible.World).Frame.Attack);
    }

    private static string Title(WorldDescriptor world, int id) => world.Areas.SelectMany(area => area.Cards)
        .Single(card => card.Id == id).Face!.Title;

    private static Prompt Defense(int attacker, int target) =>
        new(0, Question.Defender, TimingPriority.Untimed, "attack_initiated", "Defend", true,
            [new Affordance(1, "Defend", target, 0, "Defend")])
        {
            Attack = new PromptAttack(attacker, target, 3, Calculated: false, BoostUnresolved: true),
        };
}
