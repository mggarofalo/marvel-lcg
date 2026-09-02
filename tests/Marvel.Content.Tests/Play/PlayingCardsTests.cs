using Marvel.Content.Setup;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Playing a card, on the real Rhino board.
/// </summary>
/// <remarks>
/// <c>PlayerPhaseTests</c> holds the <i>offer</i> against the recorded prompt —
/// four cards of a hand of six, in the recorded order, at the recorded prices.
/// This is what happens when one is taken, which the recording never does.
/// </remarks>
public sealed class PlayingCardsTests
{
    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:play-put-into-play")]
    [Fact]
    public void PlayingMockingbirdPutsHerIntoPlayAndPaysForHer()
    {
        // `01083` Mockingbird, an ally costing 3. The opening hand holds five
        // other cards and the recording prices her at 3 against them.
        var (game, world) = Begin();
        var play = Offer(game, "01083");

        Assert.Equal("3", play.CostOptions[0].Cost);
        Assert.False(play.CostOptions[0].DeclarationSensitive);

        int mockingbird = play.AnchorId;
        var spent = Pay(play, cost: 3);
        game.Resolve(Decision.Take(play.Id, [play.Targets!.Legal[0]], spent));

        var seat = world.Seats[0];
        Assert.Equal(DeckType.AlliesArea, world.Cards[mockingbird].Area.Type);
        Assert.All(spent, id => Assert.Equal(DeckType.DiscardPile, world.Cards[id].Area.Type));
        Assert.DoesNotContain(seat.Hand.Cards, card => card.ObjectId == mockingbird);
    }

    [Rule("rr:player-turn")]
    [Fact]
    public void PlayingACardDoesNotEndTheTurn()
    {
        // "Each option, except 'change form', may be performed as many times as
        // the player is able." So the turn carries on, and what is offered next
        // is what is still affordable -- a hand three cards lighter.
        var (game, _) = Begin();
        var play = Offer(game, "01083");
        game.Resolve(Decision.Take(play.Id, [play.Targets!.Legal[0]], Pay(play, cost: 3)));

        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);

        // No longer offered as a *play* -- she is in play. Her object id is
        // still an anchor, now for `rr:player-turn.4`'s "use an ally card they
        // control in play to attack an enemy or thwart a scheme".
        Assert.DoesNotContain(
            game.Pending.Affordances,
            a => a.Verb == CardPlay.Verb && a.AnchorId == play.AnchorId);
        Assert.Contains(
            game.Pending.Affordances,
            a => a.Verb == BasicPowers.AttackVerb && a.AnchorId == play.AnchorId);
    }

    [Rule("rr:initiating-abilities.step.3")]
    [Fact]
    public void ACardTheHandCannotAffordIsNotOffered()
    {
        // Avengers Mansion costs 4 and the other five cards of the opening hand
        // generate six resources, so it is offered. Playing Mockingbird for 3
        // spends three of them, and it stops being offered.
        var (game, _) = Begin();
        Assert.Contains(game.Pending!.Affordances, Anchored("01091", game));

        var play = Offer(game, "01083");
        game.Resolve(Decision.Take(play.Id, [play.Targets!.Legal[0]], Pay(play, cost: 3)));

        Assert.DoesNotContain(game.Pending!.Affordances, Anchored("01091", game));
    }

    [Rule("rr:resource-card")]
    [Fact]
    public void TheResourceCardAndTheInterruptEventAreNotOffered()
    {
        // The hand's six cards yield four offers. `01088` Energy is a resource
        // card, which `rr:player-turn.2` does not list; `01003` Backflip is an
        // event whose ability is an **Interrupt**, and `rr:player-turn.5.d`
        // reaches an event only through an **Action** ability.
        var (game, world) = Begin();
        var offered = game.Pending!.Affordances
            .Where(a => a.Verb == CardPlay.Verb)
            .Select(a => world.Cards[a.AnchorId].FaceId)
            .ToList();

        Assert.Equal(["01083", "01091", "01092", "01093"], offered);
    }

    /// <summary>The play affordance for one printed card.</summary>
    private static Affordance Offer(Game game, string faceId)
    {
        var world = game.State;
        return game.Pending!.Affordances.Single(
            a => a.Verb == CardPlay.Verb && world.Cards[a.AnchorId].FaceId == faceId);
    }

    private static Predicate<Affordance> Anchored(string faceId, Game game) =>
        a => a.Verb == CardPlay.Verb && game.State.Cards[a.AnchorId].FaceId == faceId;

    /// <summary>The first few generators that cover a cost.</summary>
    private static int[] Pay(Affordance play, long cost)
    {
        var spent = new List<int>();
        long got = 0;
        foreach (var source in play.CostOptions[0].Generators)
        {
            if (got >= cost)
            {
                break;
            }

            spent.Add(source.Effect);
            got += source.Generates.Length;
        }

        return [.. spent];
    }

    private static (Game Game, World World) Begin()
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            [Setup.Hero("spider_man").Name],
            12345);
        var game = Game.Begin(world, Cards);
        game.Resolve(Decision.Decline);   // the mulligan
        return (game, world);
    }
}
