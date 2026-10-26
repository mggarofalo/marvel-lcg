using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Server;
using Xunit;

namespace Marvel.Godot.Tests;

/// <summary>Public Thor setup followed by labelled fixture-only ally entry and encounter stacking.</summary>
internal static class HeimdallNativeScene
{
    internal static OpenedGame Create(DatasetGameFactory factory, GameSpecification specification)
    {
        Assert.Equal("thor", specification.Heroes[0]);
        OpenedGame opened = factory.Create(specification);
        Game game = opened.Game;
        World world = game.State;
        var deck = world.AreaOf(DeckType.EncounterDeck);
        // Fixture preparation fixes physical identities, without answering the interaction under test.
        foreach (var card in deck.Cards.Take(3).ToArray()) World.MoveToTop(card, deck);
        var ally = world.Cards.Single(card => card.FaceId == "06020");
        CardPlay.PutAllyIntoPlay(world, world.Facts, world.Abilities, ally, 0,
            "prepared-native-heimdall-entry", []);
        for (int step = 0; step < 12 && game.Pending!.PublicKind != PublicDecisionKind.Response; step++)
        {
            Assert.Equal(PublicDecisionKind.OpeningHand, game.Pending!.PublicKind);
            game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances).Id));
        }
        Assert.Equal(PublicDecisionKind.Response, game.Pending!.PublicKind);
        Assert.Contains(game.Pending.Affordances, offer => offer.AnchorId == ally.ObjectId);
        return opened;
    }
}
