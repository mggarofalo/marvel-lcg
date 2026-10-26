using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Godot.Tests;

internal static class WidowNativeAssertions
{
    internal static void Verify(string contract, NativeContractServer server, EngineRequest[] mutations)
    {
        var game = server.PreparedGame;
        var world = game.State;
        if (contract == "widow-safe-house")
        {
            var copies = world.Cards.Where(card => card.FaceId == "08010").OrderBy(card => card.ObjectId).ToArray();
            Assert.Equal(new[] { copies[1].ObjectId }, Assert.Single(mutations).Decision!.Targets);
            Assert.Same(world.Seats[0].Hand, copies[1].Area);
            Assert.Equal(DeckType.DiscardPile, copies[0].Area.Type);
            Assert.False(world.Cards.Single(card => card.FaceId == "08005").Ready);
            Assert.Equal(DeckType.DiscardPile, world.Cards.Single(card => card.FaceId == "08020").Area.Type);
        }
        else
        {
            Assert.Equal(3, mutations.Length);
            var initial = server.Responses.First(response => response.World is not null).World!;
            int bite = Assert.IsType<int>(Assert.Single(initial.Areas.Where(area => area.Zone == "UpgradesArea")
                .SelectMany(area => area.Cards), card => card.Face?.Title == "Widow's Bite").Id);
            Assert.Equal(DeckType.DiscardPile, world.Cards[bite].Area.Type);
            Assert.Equal(2, world.Cards.Single(card => card.FaceId == "01103").Damage);
            Assert.True(Statuses.Has(world, world.Cards.Single(card => card.FaceId == "01103"), Statuses.Stunned));
            Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
            Assert.True(world.Seats[0].IdentityCard.Ready);
            Assert.False(world.Cards.Single(card => card.FaceId == "08009").Ready);
            Assert.Contains(server.Responses, response => response.Prompt?.Trigger == "WhenPreparationAbilityResolved"
                && response.Prompt.Affordances.Count == 2);
        }
        Assert.Equal(PublicDecisionKind.PlayerAction, game.Pending!.PublicKind);
    }
}
