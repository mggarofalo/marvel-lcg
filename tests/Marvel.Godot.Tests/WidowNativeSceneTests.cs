using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class WidowNativeSceneTests
{
    [Fact]
    public void PreparedSafeHouseSelectsExactDiscardAsCostBeforeExhaustionAndReturn()
    {
        Game game = Create("widow-safe-house");
        Card support = game.State.Cards.Single(card => card.FaceId == "08005");
        var offer = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == support.ObjectId);
        int[] copies = game.State.Cards.Where(card => card.FaceId == "08010").Select(card => card.ObjectId).ToArray();
        Assert.Equal(copies, offer.Targets!.Legal);
        Assert.True(support.Ready);
        game.Resolve(new Decision(offer.Id, [copies[1]]));
        Assert.False(support.Ready);
        Assert.Equal(DeckType.DiscardPile, game.State.Cards[copies[0]].Area.Type);
        Assert.Same(game.State.Seats[0].Hand, game.State.Cards[copies[1]].Area);
    }

    [Fact]
    public void DiscardedActualPreparationOffersBothCompletionResponsesAndTheirEffects()
    {
        Game game = Create("widow-preparation");
        var bite = game.State.Cards.Single(card => card.FaceId == "08010" && card.Area.Type == DeckType.UpgradesArea);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, item => item.AnchorId == bite.ObjectId).Id));
        Assert.Equal(DeckType.DiscardPile, bite.Area.Type);
        Assert.Equal("WhenPreparationAbilityResolved", game.Pending!.Trigger);
        Assert.Equal(2, game.Pending.Affordances.Count);
        var hero = game.State.Seats[0].IdentityCard;
        var response = Assert.Single(game.Pending.Affordances, item => item.AnchorId == hero.ObjectId);
        int villain = game.State.TheCardIn(DeckType.VillainArea)!.ObjectId;
        game.Resolve(new Decision(response.Id, [villain]));
        var suit = game.State.Cards.Single(card => card.FaceId == "08009");
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, item => item.AnchorId == suit.ObjectId).Id));
        Assert.True(hero.Ready);
        Assert.False(suit.Ready);
        Assert.Equal(1, game.State.Cards[villain].Damage);
        Assert.Equal(PublicDecisionKind.PlayerAction, game.Pending!.PublicKind);
    }

    internal static Game Create(string contract) => WidowNativeScene.Create(contract,
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"))),
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"))),
        new AbilityRunner(AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))))).Game;
}
