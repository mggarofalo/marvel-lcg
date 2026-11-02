using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.GalaxyModularBoards;

namespace Marvel.Content.Tests.Play;

// The production Crystal Ball (16130) on synthetic boards: "When Defeated: The player who
// defeated this scheme may play a card from their hand, reducing its resources cost by 3."
public sealed class CrystalBallTests : CoreCardTestBase
{
    private const string Backflip = "01003";
    private const string SwingingWebKick = "01005";
    private const string WebbedUp = "01009";
    private const string ForJustice = "01060";
    private const string NickFury = "01084";
    private const string Hawkeye = "04001a,04001b";
    private const string HawkeyesBow = "04002";
    private const string SonicArrow = "04005";

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:play-restrictions-and-permissions.2")]
    public void OnlyTheDefeaterPlaysACardFromTheirHandForThreeLess(int defeater)
    {
        // rr:play-restrictions-and-permissions.2: a permission "allows a player to play a card
        // ... outside the timing ... provided by the game rules". The player who removed the
        // last threat is the one credited (ruling 7130a7550f1a2d8b).
        var world = Board(players: 2);
        var furies = new[] { GrootBoards.Hand(world, NickFury, 0), GrootBoards.Hand(world, NickFury, 1) };
        var payer = GrootBoards.Hand(world, Backflip, defeater);
        var ball = GrootBoards.Side(world, CrystalBall, 1);
        var asked = new List<Prompt>();

        BasicPowers.BasicThwart(world, Cards, defeater, ball, []);
        Drive(world, prompt => IsBallPlay(prompt) ? Record(asked, prompt, Pay(Offer(prompt, furies[defeater]), payer)) : null);

        var prompt = Assert.Single(asked);
        Assert.Equal(defeater, prompt.Player);
        Assert.Equal("1", Assert.Single(Offer(prompt, furies[defeater]).Costs!).Cost);
        Assert.DoesNotContain(prompt.Affordances, offer => offer.AnchorId == furies[1 - defeater].ObjectId);
        Assert.Equal(DeckType.AlliesArea, furies[defeater].Area.Type);
        Assert.Equal(DeckType.HandsArea, furies[1 - defeater].Area.Type);
        Assert.Equal(DeckType.VictoryDisplay, ball.Area.Type);
    }

    [Fact]
    [Rule("rr:may")]
    public void TheDefeaterMayDeclineAndKeepsTheCard()
    {
        // rr:may: "The word 'may' indicates that a specified player has the option to resolve the text that follows."
        var world = Board();
        var kick = GrootBoards.Hand(world, SwingingWebKick);
        var ball = GrootBoards.Side(world, CrystalBall, 1);
        var asked = new List<Prompt>();

        BasicPowers.BasicThwart(world, Cards, 0, ball, []);
        Drive(world, prompt => IsBallPlay(prompt) ? Record(asked, prompt, Decision.Decline) : null);

        var prompt = Assert.Single(asked);
        Assert.True(prompt.Cancellable);
        Assert.Equal("Do not play a card", prompt.DeclineLabel);
        Assert.Equal(DeckType.HandsArea, kick.Area.Type);
        Assert.Equal(0, GrootBoards.Villain(world).Damage);
        Assert.Equal(DeckType.VictoryDisplay, ball.Area.Type);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.2")]
    public void WithNoCardThatCanBePlayedNothingIsAsked()
    {
        // rr:initiating-abilities step 2: "can the card be played ... at this time?" Backflip
        // is an Interrupt whose triggering condition is not occurring, and Webbed Up is
        // "Hero form only" while Peter Parker is in alter-ego form.
        var world = Board(face: "01001b");
        var backflip = GrootBoards.Hand(world, Backflip);
        GrootBoards.Hand(world, WebbedUp);
        var ball = GrootBoards.Side(world, CrystalBall, 1);
        var asked = new List<Prompt>();

        // Black Cat thwarts, so her controller is the defeater.
        BasicPowers.AllyPower(world, Cards, Ally(world), ball, BasicPowers.ThwartVerb, []);
        Drive(world, prompt => IsBallPlay(prompt) ? Record(asked, prompt, Decision.Decline) : null);

        Assert.Empty(asked);
        Assert.Equal(DeckType.HandsArea, backflip.Area.Type);
        Assert.Equal(DeckType.VictoryDisplay, ball.Area.Type);
    }

    [Fact]
    [Rule("rr:play-put-into-play.1")]
    public void AnAlterEgoMayPlayAnAllyButNotAHeroFormOnlyCard()
    {
        // rr:play-put-into-play.1: "Cards with the text '[type] form only' can only be played
        // ... by a player whose identity is in the specified form." Nothing else asks for a form.
        var world = Board(face: "01001b");
        var fury = GrootBoards.Hand(world, NickFury);
        var webbed = GrootBoards.Hand(world, WebbedUp);
        var payer = GrootBoards.Hand(world, Backflip);
        var ball = GrootBoards.Side(world, CrystalBall, 1);
        var asked = new List<Prompt>();

        // Black Cat thwarts, so her controller is the defeater.
        BasicPowers.AllyPower(world, Cards, Ally(world), ball, BasicPowers.ThwartVerb, []);
        Drive(world, prompt => IsBallPlay(prompt) ? Record(asked, prompt, Pay(Offer(prompt, fury), payer)) : null);

        Assert.DoesNotContain(Assert.Single(asked).Affordances, offer => offer.AnchorId == webbed.ObjectId);
        Assert.Equal(DeckType.AlliesArea, fury.Area.Type);
    }

    [Fact]
    [Rule("rr:player-turn.5")]
    public void AnActionEventResolvesAsItsAction()
    {
        // rr:player-turn.5.d: an event is played by triggering its Action; 3 - 3 costs nothing.
        var world = Board();
        var kick = GrootBoards.Hand(world, SwingingWebKick);
        var villain = GrootBoards.Villain(world);
        var ball = GrootBoards.Side(world, CrystalBall, 1);
        var asked = new List<Prompt>();

        BasicPowers.BasicThwart(world, Cards, 0, ball, []);
        Drive(world, prompt => IsBallPlay(prompt) ? Record(asked, prompt, Decision.Take(Offer(prompt, kick).Id))
            : prompt.Affordances.FirstOrDefault(a => a.AnchorId == villain.ObjectId) is { } target ? Decision.Take(target.Id) : null);

        Assert.Equal("0", Assert.Single(Offer(Assert.Single(asked), kick).Costs!).Cost);
        Assert.Equal(8, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, kick.Area.Type);
    }

    [Fact]
    [Rule("rr:cost.4.1")]
    public void APaidResourceConditionReadsOnlyWhatTheReducedCostTook()
    {
        // rr:cost.4.1: resources beyond the cost "were not paid for that cost". For Justice!
        // costs 2 - 3 = 0, so no mental resource is paid for it and it removes 3 threat.
        var world = Board();
        var justice = GrootBoards.Hand(world, ForJustice);
        GrootBoards.Hand(world, "01089");
        var main = GrootBoards.MainScheme(world);
        main.PlaceTokens("k_threat", 6);
        var ball = GrootBoards.Side(world, CrystalBall, 1);

        BasicPowers.BasicThwart(world, Cards, 0, ball, []);
        Drive(world, prompt => IsBallPlay(prompt) ? Decision.Take(Offer(prompt, justice).Id)
            : prompt.Affordances.FirstOrDefault(a => a.AnchorId == main.ObjectId) is { } target ? Decision.Take(target.Id) : null);

        Assert.Equal(3, main.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, justice.Area.Type);
    }

    [Fact]
    [Rule("rr:cost.5")]
    public void AnEventsArrowCostIsStillPaid()
    {
        // Ruling 4eedaeb17e53584f: a play with a changed resource cost keeps "other costs or
        // restrictions in effect". Sonic Arrow exhausts Hawkeye's Bow as it plays.
        var world = GrootBoards.Board("04001a", identity: Hawkeye);
        var hero = world.Seats[0].IdentityCard;
        var bow = world.CreateCard(HawkeyesBow, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), hero.ObjectId, 0));
        var arrow = GrootBoards.Hand(world, SonicArrow);
        var villain = GrootBoards.Villain(world);
        var ball = GrootBoards.Side(world, CrystalBall, 1);

        BasicPowers.BasicThwart(world, Cards, 0, ball, []);
        Drive(world, prompt => IsBallPlay(prompt) ? Decision.Take(Offer(prompt, arrow).Id)
            : prompt.Affordances.FirstOrDefault(a => a.AnchorId == villain.ObjectId) is { } target ? Decision.Take(target.Id) : null);

        Assert.False(bow.Ready);
        Assert.Equal(3, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, arrow.Area.Type);
    }

    [Fact]
    [Rule("rr:cost.5")]
    public void AnEventWhoseArrowCostCannotBePaidIsNotOffered()
    {
        var world = GrootBoards.Board("04001a", identity: Hawkeye);
        var hero = world.Seats[0].IdentityCard;
        var bow = world.CreateCard(HawkeyesBow, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), hero.ObjectId, 0));
        bow.Exhaust();
        var arrow = GrootBoards.Hand(world, SonicArrow);
        var kick = GrootBoards.Hand(world, SwingingWebKick);
        var ball = GrootBoards.Side(world, CrystalBall, 1);
        var asked = new List<Prompt>();

        BasicPowers.BasicThwart(world, Cards, 0, ball, []);
        Drive(world, prompt => IsBallPlay(prompt) ? Record(asked, prompt, Decision.Decline) : null);

        var offered = Assert.Single(asked).Affordances.Select(offer => offer.AnchorId);
        Assert.Contains(kick.ObjectId, offered);
        Assert.DoesNotContain(arrow.ObjectId, offered);
    }

    [Fact]
    [Rule("rr:unique-icon.4.1")]
    public void TheCardsOwnRestrictionsStillApply()
    {
        // Ruling 4eedaeb17e53584f: only the resource cost changes; a unique ally whose
        // counterpart is in play still cannot be played.
        var world = Board();
        GrootBoards.InPlay(world, NickFury, DeckType.AlliesArea);
        var fury = GrootBoards.Hand(world, NickFury);
        var kick = GrootBoards.Hand(world, SwingingWebKick);
        var ball = GrootBoards.Side(world, CrystalBall, 1);
        var asked = new List<Prompt>();

        BasicPowers.BasicThwart(world, Cards, 0, ball, []);
        Drive(world, prompt => IsBallPlay(prompt) ? Record(asked, prompt, Decision.Decline) : null);

        var offered = Assert.Single(asked).Affordances.Select(offer => offer.AnchorId);
        Assert.Contains(kick.ObjectId, offered);
        Assert.DoesNotContain(fury.ObjectId, offered);
    }

    [Fact]
    [Rule("rr:resource-ability.1")]
    public void TheMilanoMayPayForAnotherPlayersCrystalBallPlay()
    {
        // Research only: Ship Command is withheld. "Piloting - Resource: Exhaust the Milano ->
        // generate a [wild] resource for any player" pays this play as it pays any other.
        var world = Board(players: 2);
        var milano = ShipCommandBoards.PutMilano(world);
        var fury = GrootBoards.Hand(world, NickFury, 1);
        var ball = GrootBoards.Side(world, CrystalBall, 1);

        BasicPowers.BasicThwart(world, Cards, 1, ball, []);
        Drive(world, prompt => IsBallPlay(prompt) ? PayWith(Offer(prompt, fury), milano.ObjectId) : null);

        Assert.False(milano.Ready);
        Assert.Equal(DeckType.AlliesArea, fury.Area.Type);
    }

    private static bool IsBallPlay(Prompt prompt) => prompt.Label == $"{CrystalBall}: choose a card to play";

    private static Affordance Offer(Prompt prompt, Card card) => Assert.Single(prompt.Affordances, a => a.AnchorId == card.ObjectId);

    private static Decision Record(List<Prompt> asked, Prompt prompt, Decision decision)
    {
        asked.Add(prompt);
        return decision;
    }

    private static Decision Pay(Affordance offer, params Card[] paying) =>
        PayWith(offer, [.. paying.Select(card => card.ObjectId)]);

    private static Decision PayWith(Affordance offer, params int[] spent) =>
        new(offer.Id, [], spent, Allocations: ResourcePayment.Allocate(Assert.Single(offer.Costs!), spent));
}
