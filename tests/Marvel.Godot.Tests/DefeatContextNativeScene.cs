using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Behavior;
using Marvel.Content.Setup;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Server;
using Xunit;

namespace Marvel.Godot.Tests;

/// <summary>Synthetic global interrupt over two physical Core recipients; no content admission.</summary>
internal static class DefeatContextNativeScene
{
    internal static OpenedGame Create(CardCatalog cards, SetupCatalog setup, AbilityRunner setupAbilities)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "name":"Observe imminent defeat",
              "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
              "cost":{"spend":"Y"},"effect":{"placeAccelerationToken":1}}]}]}
            """));
        var scene = CanonicalCoreScene.Deal(new CoreSceneRequest(
            "synthetic-native:defeat-context", "rhino", ["spider_man"], 855), setup, cards, setupAbilities);
        scene.Apply(new SetSceneForm(0, "01001a"));
        scene.Apply(new MoveSceneCard(new("01006"), new(SceneZone.Support, 0)));
        scene.Apply(new MoveSceneCard(new("01110"), new(SceneZone.EngagedMinion, 0)));
        // Both prepared sources pay Y, so the other recipient remains affordable after Energy is discarded.
        scene.Apply(new SetPlayerHand(0, [new("01088"), new("01087")]));
        Card first = scene.Find(new("01110"));
        Card second = scene.World.CreateCard("01110", first.Area);
        Game game = Game.Begin(scene.World, cards, runner);
        Assert.Equal(PublicDecisionKind.OpeningHand, game.Pending!.PublicKind);
        // Fixture-only preparation queues the shared damage before opening the component window.
        SimultaneousDirectDamage.Resolve(scene.World, cards, new[] { first, second }.Select(card =>
            new DamageRequest(scene.World.Seats[0].IdentityCard, card, 10,
                "synthetic-native-context", "Deal_Damage", 0)).ToArray(), []);
        game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances).Id));
        Assert.Equal(PublicDecisionKind.Interrupt, game.Pending!.PublicKind);
        Assert.Equal(new[] { first.ObjectId, second.ObjectId }, game.Pending.Affordances
            .Select(offer => Assert.Single(offer.ContextCardIds)));
        return new(game, []);
    }
}
