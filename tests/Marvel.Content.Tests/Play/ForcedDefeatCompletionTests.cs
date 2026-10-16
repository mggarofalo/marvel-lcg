using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ForcedDefeatCompletionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    [Theory]
    [InlineData(1, 3)]
    [InlineData(1, 4)]
    [InlineData(2, 7)]
    [Rule("rr:forced.4")]
    [Rule("rr:forced.6")]
    [Rule("rr:damage.step.7")]
    public void SpiderTracerCompletesBeforeInterrogationRoomIsReconsidered(int tracers, int threat)
    {
        // Spider-Tracer prints Forced Interrupt: remove 3 threat when its minion is defeated.
        // rr:forced.6: each forced ability resolves "as completely as possible" before
        // the next. Damage step 7 resolves these interrupts before the minion leaves play.
        var order = Dealer.DealOrder(Setup, "rhino", ["spider_man"], facts: Cards);
        var world = WorldSetup.DealWithoutCardAbilities(Cards, Blueprints.From(order, Cards),
            ["Spider-Man"], 539);
        var hero = world.Seats[0].IdentityCard;
        hero.TurnTo("01001a");
        var minion = world.Cards.First(card => card.FaceId == "01101");
        World.MoveToTop(minion, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        minion.TakeDamage(2);
        var room = world.Cards.First(card => card.FaceId == "01063");
        World.MoveToTop(room, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var attached = world.Cards.Where(card => card.FaceId == "01007").Take(tracers).ToArray();
        Assert.Equal(tracers, attached.Length);
        foreach (var tracer in attached)
            World.MoveToTop(tracer, world.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId, 0));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", threat);
        var game = Game.Begin(world, Cards, AuthoredCards.Runner());
        while (game.Pending!.Affordances.Any(option => option.Verb == Game.ResolveMulligans))
            game.Resolve(Decision.Take(game.Pending.Affordances.Single(option => option.Verb == Game.ResolveMulligans).Id));

        var attack = Assert.Single(game.Pending.Affordances,
            option => option.AnchorId == hero.ObjectId && option.Verb == BasicPowers.AttackVerb);
        var events = new List<GameEvent>(game.Resolve(Decision.Take(attack.Id, [minion.ObjectId], [])).Events);
        for (int index = 0; index < tracers; index++)
        {
            Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == room.ObjectId);
            Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
            if (game.Pending.Asking == Question.Order)
            {
                var first = game.Pending.Affordances.First(option => attached.Any(card => card.ObjectId == option.AnchorId));
                events.AddRange(game.Resolve(Decision.Take(first.Id)).Events);
            }
            var target = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == scheme.ObjectId);
            events.AddRange(game.Resolve(Decision.Take(target.Id)).Events);
            Assert.Equal(threat - 3L * (index + 1), scheme.Tokens["k_threat"]);
        }

        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        var offeredRoom = game.Pending!.Affordances.Where(option => option.AnchorId == room.ObjectId).ToArray();
        Assert.Equal(threat > 3 * tracers ? 1 : 0, offeredRoom.Length);
        int removedThreat = events.FindLastIndex(item => item is FieldSet field
            && field.Card == scheme.ObjectId && field.Field == "k_threat");
        int discardedMinion = events.FindIndex(item => item is CardsMoved moved
            && moved.Cards.Any(card => card.Card == minion.ObjectId));
        Assert.True(removedThreat >= 0 && discardedMinion > removedThreat);
    }
}
