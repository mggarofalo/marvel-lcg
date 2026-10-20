using Marvel.Cards.Run;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingAttachmentProjectionTests
{
    [Rule("rr:villain-defeat.3.2")]
    [Theory]
    [InlineData(0, "07006")]
    [InlineData(1, "07020")]
    [InlineData(3, "07049")]
    public void PromotedCharacterCarriesOnlyItsOwnPrintedAttachmentModifier(int groupIndex, string face)
    {
        // "Attachments, upgrades, status cards, counters, and non-damage tokens
        // on a villain carry over to the new stage."
        var (world, groups, scope) = WreckingProjectionFixture.Create();
        var own = groups[groupIndex];
        var attachment = world.CreateCard(face, world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Villains, own.Villain.ObjectId));
        var successor = own.Stages[1];
        long printed = StateFields.Modified(world, successor, "attack", world.Facts, world.Players);
        string digest = world.Digest().Canonical();
        Assert.Equal(printed + 1, AbilityRepeatedCharacteristicTrace.TraceModified(
            successor, "attack", scope, [own.Villain.ObjectId]));
        Assert.Equal(printed, AbilityRepeatedCharacteristicTrace.TraceModified(
            successor, "attack", scope, [own.Villain.ObjectId, attachment.ObjectId]));
        Assert.Equal(new[] { attachment }, AbilityRepeatedCharacteristicTrace.TraceCarriedAttachments(
            successor, scope, [own.Villain.ObjectId]));
        foreach (var other in groups.Where(group => group != own))
            Assert.DoesNotContain(attachment, AbilityRepeatedCharacteristicTrace.TraceCarriedAttachments(
                other.Stages[1], scope, [other.Villain.ObjectId]));
        Assert.Equal(digest, world.Digest().Canonical());
        var copy = world.CreateCard(face, own.Deck);
        var copyScope = scope.ForConstant(copy);
        Assert.True(AbilityRepeatedStatusTrace.AnotherCopyAttachedInTrace(successor, copyScope, []));
        Assert.False(AbilityRepeatedStatusTrace.AnotherCopyAttachedInTrace(
            successor, copyScope, [attachment.ObjectId]));
        Assert.All(groups.Where(group => group != own), other =>
            Assert.False(AbilityRepeatedStatusTrace.AnotherCopyAttachedInTrace(other.Stages[1], copyScope, [])));
    }

    [Fact]
    public void InactivePromotedArmorReplacesOnlyDamageToItsOwnGroup()
    {
        // Synthetic group composition exercises the authored Rhino attachment
        // independently of which group holds the active-villain counter.
        var (world, groups, scope) = WreckingProjectionFixture.Create("01095", "01094");
        var armor = world.CreateCard("01098", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Villains, groups[0].Villain.ObjectId));
        var damage = new Dictionary<int, long>();
        Assert.Equal(0, AbilityDamageProtectionTrace.AfterForcedDamageReplacements(
            scope, groups[0].Stages[1].ObjectId, 3, damage, [], groups[1].Villain.ObjectId));
        Assert.Equal(3, damage[armor.ObjectId]);
        Assert.Equal(3, AbilityDamageProtectionTrace.AfterForcedDamageReplacements(
            scope, groups[3].Stages[1].ObjectId, 3, damage, [], groups[1].Villain.ObjectId));
        Assert.Equal(3, AbilityDamageProtectionTrace.AfterForcedDamageReplacements(
            scope, world.Seats[0].IdentityCard.ObjectId, 3, damage, [], groups[1].Villain.ObjectId));
        Assert.Equal(3, damage[armor.ObjectId]);
    }
}
