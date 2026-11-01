using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.ScarletWitchBoards;

namespace Marvel.Content.Tests.Play;

// Actual production rows on synthetic boards; no public pack admission.
public sealed class ScarletWitchPlayerCardTests
{
    private const string ChaosMagic = "15003";
    private const string Quicksilver = "15002";
    private const string Agatha = "15007";
    private const string TurnTheTide = "15015";
    private const string LastStand = "15029";
    private const string MagicalSuspension = "15026";
    private const string CrisisAverted = "15012";
    private const string SideScheme = "01125";
    private const string Ally = "01058";

    [Fact]
    [Rule("rr:encounter-deck.1")]
    public void AgathaAddsOneLookedAtCardAndOrdersTheRestOntoTheDeckBottom()
    {
        // rr:encounter-deck.1 is the encounter-deck form of the same principle:
        // a deck's order changes only when an ability instructs it, here Agatha's
        // "place the rest on the bottom of your deck in any order".
        var runner = AuthoredCards.Runner();
        var world = Board(AlterEgo);
        var agatha = InPlay(world, Agatha, DeckType.SupportsArea);
        var deck = world.Seats[0].Deck;
        var top = deck.Cards.Reverse().Take(3).ToArray();
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == agatha.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var look = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(top.Select(card => card.ObjectId).Order(), look.Affordances.Select(offer => offer.AnchorId).Order());
        Sequence.Answer(world, world.Facts, runner, look, Decision.Take(top[1].ObjectId), []);
        var order = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(Question.Order, order.Asking);
        var offer = Assert.Single(order.Affordances);
        Assert.Equal(new[] { top[0].ObjectId, top[2].ObjectId }.Order(), offer.Targets!.Legal.Order());
        Sequence.Answer(world, world.Facts, runner, order,
            Decision.Take(offer.Id) with { Targets = [top[2].ObjectId, top[0].ObjectId] }, []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Contains(top[1], world.Seats[0].Hand.Cards);
        // Top to bottom: the first named card sits above the last, which is the bottom card.
        Assert.Equal([top[0], top[2]], deck.Cards.Take(2));
        Assert.False(agatha.Ready);
    }

    [Fact]
    [Rule("rr:encounter-deck.1")]
    public void AgathaWithOneRemainingCardPlacesItWithoutAnOrderChoice()
    {
        // rr:encounter-deck.1: order changes only as the ability instructs; one
        // remaining card has a single order.
        var runner = AuthoredCards.Runner();
        var world = Board(AlterEgo);
        var agatha = InPlay(world, Agatha, DeckType.SupportsArea);
        var deck = world.Seats[0].Deck;
        foreach (var extra in deck.Cards.Take(deck.Cards.Count - 2).ToArray())
            World.MoveToTop(extra, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        var top = deck.Cards.Reverse().ToArray();
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == agatha.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var look = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(2, look.Affordances.Count);
        Sequence.Answer(world, world.Facts, runner, look, Decision.Take(top[0].ObjectId), []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal([top[1]], deck.Cards);
    }

    [Fact]
    [Rule("rr:ignore.1")]
    public void ChaosMagicPlaysAnAllyForFreeAndDiscardsItsPrintedCost()
    {
        // rr:ignore.1: "no resources are paid for that card", so Magical
        // Suspension's increase does not apply to the free play.
        var runner = AuthoredCards.Runner();
        var world = Board();
        world.CreateCard(MagicalSuspension, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0),
            host: world.Seats[0].IdentityCard.ObjectId));
        var chaos = world.CreateCard(ChaosMagic, world.Seats[0].Hand);
        var pietro = world.CreateCard(Quicksilver, world.Seats[0].Hand);
        var energy = world.CreateCard(Energy, world.Seats[0].Hand);
        int encounter = world.AreaOf(DeckType.EncounterDeck).Cards.Count;
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == chaos.ObjectId);
        // Chaos Magic itself costs 0 + 1 under Magical Suspension.
        runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId], []));
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var play = Assert.Single(choice.Affordances, offer => offer.AnchorId == pietro.ObjectId);
        Assert.Equal("0", Assert.Single(play.Costs!).Cost);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(play.Id), []);
        Drive(world, runner, _ => null);
        Assert.Equal(DeckType.AlliesArea, pietro.Area.Type);
        Assert.Equal(encounter - 4, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Equal(DeckType.DiscardPile, chaos.Area.Type);
    }

    [Fact]
    [Rule("rr:ignore.1")]
    public void ChaosMagicPlaysAPaidConditionEventWithNothingPaid()
    {
        // rr:ignore.1: the card "is considered to have been played with zero
        // resources paid for its cost", so Crisis Averted's mental branch is off.
        var runner = AuthoredCards.Runner();
        var world = Board();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 8);
        var chaos = world.CreateCard(ChaosMagic, world.Seats[0].Hand);
        var crisis = world.CreateCard(CrisisAverted, world.Seats[0].Hand);
        int encounter = world.AreaOf(DeckType.EncounterDeck).Cards.Count;
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == chaos.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var play = Assert.Single(choice.Affordances, offer => offer.AnchorId == crisis.ObjectId);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(play.Id), []);
        Drive(world, runner, _ => null);
        Assert.Equal(2, main.Tokens["k_threat"]);
        Assert.Equal(encounter - 3, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Equal(DeckType.DiscardPile, crisis.Area.Type);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(9, false)]
    [Rule("rr:response.1")]
    public void TurnTheTideAnswersAHeroThwartThatClearsAScheme(int threat, bool cleared)
    {
        // rr:response.1: a response "may be triggered immediately after that
        // triggering condition becomes true"; the condition is the hero's thwart
        // removing all threat (ruling 2f5ce668fd00dbb2).
        var runner = AuthoredCards.Runner();
        var world = Board();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var scheme = world.CreateCard(SideScheme, world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", threat);
        var tide = world.CreateCard(TurnTheTide, world.Seats[0].Hand);
        BasicPowers.BasicThwart(world, world.Facts, 0, scheme, []);
        var offered = new List<int>();
        Drive(world, runner, prompt =>
        {
            if (prompt.Affordances.FirstOrDefault(offer => offer.AnchorId == tide.ObjectId) is { } response)
            {
                offered.Add(response.Id);
                return Decision.Take(response.Id);
            }
            return prompt.Affordances.FirstOrDefault(offer => offer.AnchorId == villain.ObjectId) is { } target
                ? Decision.Take(target.Id) : null;
        });
        Assert.Equal(cleared ? 1 : 0, offered.Count);
        Assert.Equal(cleared ? 3 : 0, villain.Damage);
    }

    [Fact]
    [Rule("rr:delayed-effect.1")]
    public void LastStandBoostsTheAllyAttackThenDiscardsTheAlly()
    {
        // rr:delayed-effect.1: a delayed effect resolves "immediately after" its
        // timing point "and before responses"; ruling 01d17bfed5e5dad8 puts the
        // discard before consequential damage.
        var runner = AuthoredCards.Runner();
        var world = Board();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var ally = InPlay(world, Ally, DeckType.AlliesArea);
        var stand = world.CreateCard(LastStand, world.Seats[0].Hand);
        long attack = world.Facts.PrintedValue(Ally, "ATK", world.Players);
        AllyBasicPowers.AllyPower(world, world.Facts, ally, villain, BasicPowers.AttackVerb, []);
        int taken = 0;
        Drive(world, runner, prompt =>
        {
            if (prompt.Affordances.FirstOrDefault(offer => offer.AnchorId == stand.ObjectId) is not { } play) return null;
            taken++;
            return Decision.Take(play.Id);
        });
        Assert.Equal(1, taken);
        Assert.Equal(attack + 3, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(0, ally.Damage);
    }
}
