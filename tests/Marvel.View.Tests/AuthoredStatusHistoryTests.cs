using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.View.Tests;

public sealed class AuthoredStatusHistoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WebbedUpExplainsOneAppliedStunForEitherVisibilityPolicy(bool cooperative)
    {
        var world = PersistentFixture.Board("01001a");
        var runner = new AbilityRunner(PersistentFixture.Abilities);
        world.Abilities = runner;
        world.CreateCard("01080", world.Seats[0].Deck);
        var rhino = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var webbed = world.CreateCard("01009",
            world.AreaOf(DeckType.UpgradesArea, rhino.Area.PlayArea, rhino.ObjectId, 0));
        // This is a prepared Core attack window, resolved by the authored Webbed Up ability.
        var occurrence = Occurrence.ForAttack(1, [Steps.AttackInitiated], world,
            PersistentFixture.Cards, rhino.ObjectId, world.Seats[0].IdentityCard.ObjectId, player: 0);
        var ability = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt), item => item.Card == webbed.ObjectId);

        var events = runner.ResolveWindow(world, occurrence, ability, new Decision(ability.Card, [], [])).Events;

        var status = Assert.Single(Statuses.On(world, rhino, Statuses.Stunned));
        Assert.Equal(status.ObjectId, Assert.Single(events.OfType<CardAttached>()).Card);
        IVisibilityPolicy policy = cooperative ? new PermissiveVisibilityPolicy() : new RestrictedVisibilityPolicy(0);
        var visible = WorldProjection.For(world, null, events, policy.Authorize(null, world.Players));
        var history = EventPresenter.Present(visible.Events, visible.World);
        var attachment = Assert.Single(history, entry => entry.Summary == "Attached Stunned to Rhino.");
        Assert.Contains("Rhino", attachment.Summary);
        var notification = Assert.Single(history, entry => entry.ForcedAbilityNotification is not null);
        Assert.Equal(1, notification.Summary.Split("Attached Stunned", StringSplitOptions.None).Length - 1);
        Assert.Equal(DeckType.DiscardPile, webbed.Area.Type);
    }
}
