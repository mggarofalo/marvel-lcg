using Marvel.Content.Behavior;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;

public sealed class GameBoundaryTests
{
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:initiating-abilities.step.7")]
    [Fact]
    public void PaidEventWaitsForItsTargetThenReportsHealingAndDiscard()
    {
        // First Aid: "Heal 2 damage from any character." Step 5 pays the cost;
        // step 7 says an event's "effects resolve and it is then placed in its
        // owner's discard pile." The target answer resumes that same event.
        var scene = Deal("behavior:card:01086:heal-2-damage-from-any-character");
        var identity = scene.World.Seats[0].IdentityCard;
        var firstAid = scene.Find(new SceneCard("01086"));
        var resource = scene.Find(new SceneCard("01088"));
        scene.Apply(new SetSceneDamage(new SceneCard("01001b"), 2));
        scene.Apply(new SetPlayerHand(0, [new("01086"), new("01088")]));
        var game = Begin(scene);
        var action = Action(game, firstAid);
        var price = Assert.Single(action.CostOptions);
        var allocations = ResourcePayment.Allocate(price, [resource.ObjectId]);
        Assert.NotNull(allocations);

        var paid = game.Resolve(new Decision(
            action.Id, [], [resource.ObjectId], Allocations: allocations));

        Assert.False(game.IsRootPrompt);
        Assert.Equal(2, identity.Damage);
        Assert.Equal(DeckType.DiscardPile, resource.Area.Type);
        Assert.NotEqual(DeckType.DiscardPile, firstAid.Area.Type);
        var paymentMove = Assert.Single(paid.Events.OfType<CardsMoved>(), moved =>
            moved.Cards.Any(card => card.Card == resource.ObjectId));
        Assert.Equal(Places.Reference(scene.World.Seats[0].Hand), paymentMove.From);
        Assert.Equal(Places.Reference(resource.Area), paymentMove.To);
        Assert.Equal(resource.Area.Cards.ToList().IndexOf(resource),
            Assert.Single(paymentMove.Cards).Index);
        var target = Assert.Single(paid.Prompt!.Affordances,
            option => option.AnchorId == identity.ObjectId);
        var resolvingArea = Places.Reference(firstAid.Area);

        var completed = game.Resolve(Decision.Take(target.Id));

        Assert.Equal(0, identity.Damage);
        Assert.True(game.IsRootPrompt);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        var healing = Assert.Single(completed.Events.OfType<FieldSet>(),
            changed => changed.Card == identity.ObjectId && changed.Field == "health");
        Assert.Equal(8L, healing.From);
        Assert.Equal(10L, healing.To);
        var discard = Assert.Single(completed.Events.OfType<CardsMoved>(), moved =>
            moved.Cards.Any(card => card.Card == firstAid.ObjectId));
        Assert.Equal(resolvingArea, discard.From);
        Assert.Equal(Places.Reference(resource.Area), discard.To);
        Assert.Equal(new Landing(firstAid.ObjectId, firstAid.Area.Cards.Count - 1),
            Assert.Single(discard.Cards));
        Assert.Same(firstAid, firstAid.Area.Cards[^1]);
        Assert.True(firstAid.FaceUp);
        Assert.True(completed.Events.ToList().IndexOf(healing)
            < completed.Events.ToList().IndexOf(discard));
    }

    [Rule("rr:choose-option.2")]
    [Rule("rr:leaves-play.1")]
    [Fact]
    public void PaidChoiceConsumesItsLimitAndReentryStartsANewInstance()
    {
        // Vision: "Spend a [energy] resource → choose THW or ATK. Until the
        // end of the phase, Vision gets +2 to the chosen power. (Limit once
        // per round.)" Both choices can resolve; only the chosen stat changes.
        var scene = Deal("behavior:card:01068:choose-thw-plus-two-until-end-phase",
            "captain_marvel");
        var vision = scene.Find(new SceneCard("01068"));
        var resource = scene.Find(new SceneCard("01012"));
        scene.Apply(new MoveSceneCard(new("01068"), new(SceneZone.Ally, 0)));
        scene.Apply(new SetSceneDamage(new("01068"), 2));
        scene.Apply(new SetPlayerHand(0,
            [new("01012"), new("01012", 1), new("01071"), new("01014"), new("01088")]));
        var game = Begin(scene);
        var action = Action(game, vision);
        var allocations = ResourcePayment.Allocate(
            Assert.Single(action.CostOptions), [resource.ObjectId]);
        Assert.NotNull(allocations);

        var choosing = game.Resolve(new Decision(
            action.Id, [], [resource.ObjectId], Allocations: allocations));

        Assert.False(game.IsRootPrompt);
        Assert.Equal(2, choosing.Prompt!.Affordances.Count);
        Assert.Equal(DeckType.DiscardPile, resource.Area.Type);

        game.Resolve(Decision.Take(choosing.Prompt.Affordances[0].Id));

        Assert.True(game.IsRootPrompt);
        Assert.Equal(3, StateFields.Modified(scene.World, vision, "thwart", Cards, 1));
        Assert.Equal(2, StateFields.Modified(scene.World, vision, "attack", Cards, 1));
        Assert.DoesNotContain(game.Pending!.Affordances, option =>
            option.AnchorId == vision.ObjectId && option.Verb == Game.ActionVerb);

        // The vendored FAQ for 01068 says the Vision that re-enters "has not
        // triggered its ability yet." Consequential damage defeats this
        // instance; Make the Call puts the same physical card into play again.
        int incarnation = vision.Incarnation;
        var attack = Assert.Single(game.Pending.Affordances, option =>
            option.Verb == BasicPowers.AttackVerb && option.AnchorId == vision.ObjectId);
        game.Resolve(new Decision(attack.Id,
            [scene.World.TheCardIn(DeckType.VillainArea)!.ObjectId]));
        Assert.True(game.IsRootPrompt);
        Assert.Equal(DeckType.DiscardPile, vision.Area.Type);

        var makeTheCall = scene.Find(new SceneCard("01071"));
        game.Resolve(Decision.Take(Action(game, makeTheCall).Id));
        Assert.False(game.IsRootPrompt);
        var returnAlly = Assert.Single(game.Pending!.Affordances,
            option => option.AnchorId == vision.ObjectId);
        int[] payment = [scene.Find(new("01014")).ObjectId, scene.Find(new("01088")).ObjectId];
        var returnAllocation = ResourcePayment.Allocate(
            Assert.Single(returnAlly.CostOptions), payment);
        Assert.NotNull(returnAllocation);
        game.Resolve(new Decision(returnAlly.Id, [], payment, Allocations: returnAllocation));

        Assert.True(game.IsRootPrompt);
        Assert.Equal(DeckType.AlliesArea, vision.Area.Type);
        Assert.True(vision.Incarnation > incarnation);
        Assert.Equal(1, StateFields.Modified(scene.World, vision, "thwart", Cards, 1));
        Assert.Contains(game.Pending!.Affordances, option =>
            option.AnchorId == vision.ObjectId && option.Verb == Game.ActionVerb);
    }

    [Rule("rr:player-phase")]
    [Fact]
    public void BothPlayerTurnsFinishBeforeEndPhaseDiscardBegins()
    {
        // "During the player phase, each player (in player order) takes one
        // turn." The phase cannot end when only the first player has passed.
        var scene = Deal("behavior:rr:player-phase:published-result",
            "spider_man", "captain_marvel");
        var game = Begin(scene);

        var secondTurn = game.Resolve(Decision.Decline);

        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.Equal(1, game.Active);
        Assert.Equal(1, secondTurn.Prompt!.Player);

        var discard = game.Resolve(Decision.Decline);

        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.Equal(0, discard.Prompt!.Player);
        Assert.Contains(discard.Prompt.Affordances,
            option => option.Verb == Game.EndPhaseVerb);
    }

    [Rule("rr:villain-defeat")]
    [Fact]
    public void FinalVillainDefeatEndsThePublicDecisionLoop()
    {
        // "If the final stage of the villain deck is defeated, the players
        // win the game." Rhino II has 15 HP per player; Spider-Man attacks for 2.
        var scene = Deal("behavior:rr:villain-defeat:published-result");
        scene.Apply(new SetSceneVillain(new("01095")));
        scene.Apply(new SetSceneForm(0, "01001a"));
        scene.Apply(new SetSceneDamage(new("01095"), 14));
        scene.Apply(new SetPlayerHand(0, []));
        var game = Begin(scene);
        var attack = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == BasicPowers.AttackVerb
            && option.AnchorId == scene.World.Seats[0].IdentityCard.ObjectId);

        var ended = game.Resolve(new Decision(
            attack.Id, [scene.Find(new("01095")).ObjectId]));

        Assert.Equal(Outcome.PlayersWin, scene.World.Result);
        Assert.Equal(GamePhase.Over, game.Phase);
        Assert.Null(ended.Prompt);
        Assert.Null(game.Pending);
        Assert.False(game.IsRootPrompt);
        Assert.Throws<InvalidOperationException>(() => game.Resolve(Decision.Decline));
    }

    private static CanonicalCoreScene Deal(string authority, params string[] heroes) =>
        CanonicalCoreScene.Deal(
            new CoreSceneRequest(authority, "rhino",
                heroes.Length == 0 ? ["spider_man"] : heroes, 855),
            Setup, Cards, AuthoredCards.Runner());

    private static Game Begin(CanonicalCoreScene scene)
    {
        var game = Game.Begin(scene.World, Cards, scene.World.Abilities);
        for (int player = 0; player < scene.World.Players; player++)
        {
            Assert.Equal(GamePhase.Mulligan, game.Phase);
            Assert.Equal(player, game.Pending!.Player);
            game.Resolve(Decision.Decline);
        }
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.True(game.IsRootPrompt);
        return game;
    }

    private static Affordance Action(Game game, Card source) =>
        Assert.Single(game.Pending!.Affordances, option =>
            option.AnchorId == source.ObjectId && option.Verb == Game.ActionVerb);
}
