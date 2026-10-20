using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PhotonicBlastDefeatTests
{
    [Rule("rr:when-defeated-abilities.2.1")]
    [Rule("rr:when-revealed-abilities")]
    [Rule("rr:villain-defeat.2")]
    [Rule("rr:cost.3.1")]
    [Rule("rr:wild-resource.1")]
    [Theory]
    [InlineData(false, "energy")]
    [InlineData(false, "wild")]
    [InlineData(false, "mental")]
    [InlineData(true, "energy")]
    [InlineData(true, "wild")]
    [InlineData(true, "mental")]
    public void AuthoredBlastCompletesDefeatBeforeItsPaymentConditionalDraw(bool promotion, string payment)
    {
        // "A defeated card leaves play after its 'When Defeated' ability is resolved, if any."
        // When revealing "a new villain stage, all 'When Revealed' abilities on the card resolve."
        // "Excess damage that is dealt to defeat a villain stage does not carry over to the new stage."
        // "Resources generated to pay for an ability on a card are considered to have been paid for that card."
        // For wild resources, players "may specify which resource type (energy, mental, physical, or wild) it is being used as."
        // Printed 01013 draws one card only if an energy resource was spent.
        var probed = Run(promotion, payment, probe: true);
        var control = Run(promotion, payment, probe: false);
        Assert.Equal(control, probed);
    }

    private static (string Digest, long Random, string Events) Run(bool promotion, string payment, bool probe)
    {
        var (game, world, blast, target, paying) = Prepare(promotion, payment);
        var action = Assert.Single(game.Pending!.Affordances,
            option => option.AnchorId == blast.ObjectId && option.Verb == Game.ActionVerb);
        if (probe) AssertActionQueryIsPure(game, world, blast);
        int[] ids = [.. paying.Select(card => card.ObjectId)];
        var allocations = Allocate(payment, paying, action);
        var events = game.Resolve(new Decision(action.Id, [], ids, Allocations: allocations)).Events.ToList();
        int handBefore = world.Seats[0].Hand.Cards.Count;
        var drawn = world.Seats[0].Deck.Cards[^1];
        int dealtBefore = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count;
        var targetOffer = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == target!.ObjectId);
        events.AddRange(game.Resolve(Decision.Take(targetOffer.Id)).Events);
        while (game.Pending is { Asking: Question.Opportunity })
            events.AddRange(game.Resolve(Decision.Decline).Events);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(DeckType.DiscardPile, blast!.Area.Type);
        Assert.All(paying, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        bool draws = payment != "mental";
        Assert.Equal(handBefore + (draws ? 1 : 0), world.Seats[0].Hand.Cards.Count);
        Assert.Same(draws ? world.Seats[0].Hand : world.Seats[0].Deck, drawn.Area);
        Assert.Equal(dealtBefore + (promotion ? 0 : 1),
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
        if (promotion)
        {
            var next = world.TheCardIn(DeckType.VillainArea)!;
            Assert.Equal("04059", next.FaceId);
            Assert.Equal(0, next.Damage);
            var gun = Assert.Single(world.Cards, card => card.FaceId == "04064");
            Assert.Equal(next.ObjectId, gun.Area.Host);
            Assert.True(StateFields.Modified(world, next, "piercing", world.Facts, world.Players) > 0);
        }
        else Assert.Equal(DeckType.EncounterDiscardPile, target!.Area.Type);
        if (draws)
            AssertDrawOrder(events, world, drawn, target!, promotion);
        return (world.Digest().Canonical(), world.Random.Generator.WordsConsumed,
            System.Text.Json.JsonSerializer.Serialize(events));
    }

    private static IReadOnlyList<ResourceAllocation>? Allocate(string payment, Card[] paying, Affordance action) =>
        payment == "wild"
            ? [new(paying[0].ObjectId, 0, "Y"), new(paying[1].ObjectId, 0, "BB")]
            : ResourcePayment.Allocate(Assert.Single(action.Costs!), [.. paying.Select(card => card.ObjectId)]);

    private static (Game Game, World World, Card Blast, Card Target, Card[] Paying) Prepare(bool promotion, string payment)
    {
        Card? blast = null;
        Card? target = null;
        Card[] paying = [];
        var runner = AuthoredCards.Runner();
        var (game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo("01010a");
            blast = board.CreateCard("01013", board.Seats[0].Hand);
            string[] faces = payment switch
            {
                "energy" => ["01014"],
                "wild" => ["01011", "01089"],
                _ => ["01015", "01089"],
            };
            paying = [.. faces.Select(face => board.CreateCard(face, board.Seats[0].Hand))];
            target = promotion ? board.TheCardIn(DeckType.VillainArea)!
                : board.CreateCard("01182", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            if (promotion)
            {
                target.TakeDamage(7);
                var gun = Assert.Single(board.Cards, card => card.FaceId == "04064");
                World.MoveToTop(gun, board.AreaOf(DeckType.EncounterDiscardPile));
            }
        }, heroes: ["captain_marvel"], abilities: runner, scenario: "crossbones");
        return (game, world, blast!, target!, paying);
    }

    private static void AssertActionQueryIsPure(Game game, World world, Card blast)
    {
        var runner = AuthoredCards.Runner();
        string digest = world.Digest().Canonical();
        long random = world.Random.Generator.WordsConsumed;
        var pending = game.Pending;
        var agenda = world.Agenda.Current;
        for (int index = 0; index < 3; index++)
            Assert.Contains(runner.Actions(world, 0), ability => ability.Card == blast.ObjectId);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random, world.Random.Generator.WordsConsumed);
        Assert.Same(pending, game.Pending);
        Assert.Equal(agenda, world.Agenda.Current);
    }

    private static void AssertDrawOrder(List<GameEvent> events, World world, Card drawn, Card target, bool promotion)
    {
        int drawIndex = events.FindIndex(item => item is CardsMoved moved
            && moved.To.Zone == nameof(DeckType.HandsArea)
            && moved.Cards.Any(card => card.Card == drawn.ObjectId));
        int departure = events.FindIndex(item => item is CardsMoved moved
            && moved.Cards.Any(card => card.Card == target.ObjectId));
        int followup = promotion
            ? events.FindIndex(item => item is CardsMoved moved
                && moved.To.Host == world.TheCardIn(DeckType.VillainArea)!.ObjectId
                && moved.Cards.Any(card => world.Cards[card.Card].FaceId == "04064"))
            : events.FindIndex(item => item is CardsMoved moved
                && moved.To.Zone == nameof(DeckType.DealtEncounterCardsDeck));
        Assert.True(departure >= 0 && departure < drawIndex);
        Assert.True(followup >= 0 && followup < drawIndex);
    }
}


