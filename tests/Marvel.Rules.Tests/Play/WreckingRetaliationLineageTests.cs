using Marvel.Rules.State;
using Marvel.Rules.Play;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class WreckingRetaliationLineageTests
{
    [Rule("rr:villain-defeat.3.1")]
    [Rule("rr:retaliate-x.1")]
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void CapturedRecipientCannotFollowAnUnrelatedSameTitleVillain(bool promoted, bool separateArea)
    {
        // Villain defeat: "The new stage of the villain is considered the same
        // character as the defeated stage for purposes of card abilities."
        // Retaliate: "After this character is attacked, deal X damage to the attacker."
        // A different physical group is not that character.
        // Synthetic duplicate titles distinguish ownership from title-only lookup.
        var fixture = new WreckingRuntimeFixture(extreme: true);
        var world = fixture.World;
        var group = fixture.Groups[2];
        var original = group.Villain;
        var area = original.Area;
        var recipient = new RetaliationRecipient(Attacked.Capture(original, world.Facts), area.Id);
        var unrelated = world.CreateCard(original.FaceId, area);
        foreach (var card in new[] { unrelated, group.Stages[1] })
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                "retaliate", 1, Affects: card.ObjectId));
        World.MoveToTop(original, world.AreaOf(DeckType.RemovedArea));
        if (promoted)
            World.MoveToTop(group.Stages[1], separateArea ? world.CreateArea(DeckType.VillainArea) : area);
        var attacker = world.Seats[0].IdentityCard;
        var occurrence = new Occurrence(1, [Steps.AttackEnds])
        {
            Retaliation = new RetaliationContext(attacker.ObjectId, attacker.Incarnation, false, [recipient]),
        };
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);

        var offered = world.WindowAbilities.Waiting(world, occurrence, WindowKind.Response);
        if (promoted && !separateArea)
        {
            var ability = Assert.Single(offered);
            Assert.Equal(NativeAbilityKind.Retaliate, ability.Native);
            Assert.Equal(original.ObjectId, ability.Card);
        }
        else Assert.Empty(offered);

        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
    }
}
