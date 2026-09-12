using Marvel.Cards.Dsl;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public sealed class ActionAbilityCoreAWindowEventTests
{
    [Fact]
    public void AWindowEventWithPrintedAndArrowCostsUsesOneAllocatedPayment()
    {
        Card? card = null;
        Card? energy = null;
        var runner = Runner(AuthoredCards.Backflip, "Interrupt", """{ "draw": { "player": "you", "count": 1 } }""", cost: """{ "spend": "Y" }""", eventName: "WhenDamageWouldBeDealt");
        var(_, world) = Playing(board =>
        {
            Hand(board, AuthoredCards.Backflip, 0);
            card = board.CreateCard(AuthoredCards.Backflip, board.Seats[0].Hand);
            energy = board.CreateCard("01088", board.Seats[0].Hand);
        }, hero: true, abilities: runner);
        var eventCard = Assert.IsType<Card>(card);
        var payment = Assert.IsType<Card>(energy);
        var occurrence = new Occurrence(1, ["WhenDamageWouldBeDealt"], Player: 0, Target: world.Seats[0].IdentityCard.ObjectId);
        Assert.Contains(runner.Waiting(world, occurrence, WindowKind.Interrupt), pending => pending.Card == eventCard.ObjectId);
        runner.Resolve(world, occurrence, new PendingAbility(eventCard.ObjectId, AbilityType.Interrupt, 0), [payment.ObjectId], [], allocations: [new ResourceAllocation(payment.ObjectId, Cost: 1, PaidAs: "Y"), ]);
        Assert.Equal(DeckType.DiscardPile, eventCard.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
    }

    [Fact]
    public void UnlikeOverpaymentForAResourceSensitiveEventIsRejectedBeforePayment()
    {
        const string relentlessAssault = "01053"; // cost 2; physical payment grants overkill.
        Card? card = null;
        Card? doubleMental = null;
        Card? physical = null;
        var runner = AuthoredCards.Runner();
        var(_, world) = Playing(board =>
        {
            Hand(board, Physicals, 0);
            board.Seats[0].IdentityCard.TurnTo("01010a");
            card = board.CreateCard(relentlessAssault, board.Seats[0].Hand);
            doubleMental = board.CreateCard("01089", board.Seats[0].Hand);
            physical = board.CreateCard(Physicals, board.Seats[0].Hand);
            board.CreateCard(AuthoredCards.Shocker, board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, heroes: ["captain_marvel"], abilities: runner);
        foreach (var extra in world.Seats[0].Hand.Cards.Where(candidate => candidate != card && candidate != doubleMental && candidate != physical).ToList())
        {
            World.MoveToTop(extra, world.Seats[0].Deck);
        }

        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Contains(price.Generators, source => source.Effect == doubleMental!.ObjectId);
        Assert.Contains(price.Generators, source => source.Effect == physical!.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => runner.Act(world, action, [doubleMental!.ObjectId, physical!.ObjectId], [world.Cards.First(candidate => candidate.FaceId == AuthoredCards.Shocker).ObjectId]));
        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.Same(world.Seats[0].Hand, doubleMental!.Area);
        Assert.Same(world.Seats[0].Hand, physical!.Area);
    }

    [Rule("rr:overkill")]
    [Fact]
    public void RelentlessAssaultPreviewsItsPaymentGrantedOverkill()
    {
        const string relentlessAssault = "01053";
        Card? card = null;
        Card? strength = null;
        Card? shocker = null;
        var runner = AuthoredCards.Runner();
        var(_, world) = Playing(board =>
        {
            card = board.CreateCard(relentlessAssault, board.Seats[0].Hand);
            strength = board.CreateCard("01090", board.Seats[0].Hand);
            shocker = board.CreateCard(AuthoredCards.Shocker, board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        runner.Act(world, action, [strength!.ObjectId], []);
        Prompt prompt = Assert.IsType<Prompt>(Sequence.Work(world, CardCatalogData, runner, []));
        string? preview = Assert.Single(prompt.Affordances, option => option.Id == shocker!.ObjectId).Description;
        Assert.Contains("Overkill carries 2 excess damage", preview);
    }

    [Fact]
    public void WildOverpaymentForAResourceSensitiveEventIsRejectedBeforePayment()
    {
        const string relentlessAssault = "01053";
        Card? card = null;
        Card? doubleMental = null;
        Card? wild = null;
        var runner = AuthoredCards.Runner();
        var(_, world) = Playing(board =>
        {
            Hand(board, Physicals, 0);
            card = board.CreateCard(relentlessAssault, board.Seats[0].Hand);
            doubleMental = board.CreateCard("01089", board.Seats[0].Hand);
            wild = board.CreateCard("01011", board.Seats[0].Hand);
            board.CreateCard(AuthoredCards.Shocker, board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        int target = world.Cards.First(candidate => candidate.FaceId == AuthoredCards.Shocker).ObjectId;
        Assert.Throws<RulesNotImplementedException>(() => runner.Act(world, action, [doubleMental!.ObjectId, wild!.ObjectId], [target]));
        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.Same(world.Seats[0].Hand, doubleMental!.Area);
        Assert.Same(world.Seats[0].Hand, wild!.Area);
    }

    [Fact]
    public void AllWildOverpaymentStillNeedsEachWildsDeclaredType()
    {
        // The player may declare every paid wild as a non-physical type. The
        // source-only wire cannot infer Relentless Assault's physical bonus.
        const string relentlessAssault = "01053";
        Card? card = null;
        var wilds = new List<Card>();
        var runner = AuthoredCards.Runner();
        var(_, world) = Playing(board =>
        {
            Hand(board, Physicals, 0);
            board.Seats[0].IdentityCard.TurnTo("01010a");
            card = board.CreateCard(relentlessAssault, board.Seats[0].Hand);
            for (int index = 0; index < 3; index++)
            {
                wilds.Add(board.CreateCard("01011", board.Seats[0].Hand));
            }

            board.CreateCard(AuthoredCards.Shocker, board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, heroes: ["captain_marvel"], abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        int target = world.Cards.First(candidate => candidate.FaceId == AuthoredCards.Shocker).ObjectId;
        Assert.Throws<RulesNotImplementedException>(() => runner.Act(world, action, [..wilds.Select(source => source.ObjectId)], [target]));
        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.All(wilds, source => Assert.Same(world.Seats[0].Hand, source.Area));
    }

    [Fact]
    public void ExactWildPaymentStillNeedsTheWildsDeclaredType()
    {
        const string relentlessAssault = "01053";
        Card? card = null;
        Card? wild = null;
        Card? mental = null;
        var runner = AuthoredCards.Runner();
        var(_, world) = Playing(board =>
        {
            Hand(board, Physicals, 0);
            card = board.CreateCard(relentlessAssault, board.Seats[0].Hand);
            wild = board.CreateCard("01011", board.Seats[0].Hand);
            mental = board.CreateCard(Mentals, board.Seats[0].Hand);
            board.CreateCard(AuthoredCards.Shocker, board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        int target = world.Cards.First(candidate => candidate.FaceId == AuthoredCards.Shocker).ObjectId;
        Assert.Throws<RulesNotImplementedException>(() => runner.Act(world, action, [wild!.ObjectId, mental!.ObjectId], [target]));
        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.Same(world.Seats[0].Hand, wild!.Area);
        Assert.Same(world.Seats[0].Hand, mental!.Area);
    }

    [Rule("rr:requirement-resources")]
    [Fact]
    public void ARequirementCanForceAnUnambiguousWildDeclaration()
    {
        const string requiredEvent = "27016"; // cost 2, requirement physical.
        Card? card = null;
        Card? doubleWild = null;
        var runner = Runner(requiredEvent, "Action", """{ "if": { "test": { "paidWithResource": "R" }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        var(_, world) = Playing(board =>
        {
            Hand(board, Physicals, 0);
            card = board.CreateCard(requiredEvent, board.Seats[0].Hand);
            doubleWild = board.CreateCard("01044", board.Seats[0].Hand);
        }, heroes: ["captain_marvel"], abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        runner.Act(world, action, [doubleWild!.ObjectId], []);
        Assert.Equal(DeckType.DiscardPile, card!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, doubleWild.Area.Type);
    }

    [Rule("rr:requirement-resources")]
    [Fact]
    public void AForcedWildDeclarationIsCarriedIntoPaidResourceTests()
    {
        const string requiredEvent = "27016";
        Card? card = null;
        Card? wild = null;
        Card? mental = null;
        var runner = Runner(requiredEvent, "Action", """{ "if": { "test": { "paidWithResource": "Y" }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        var(_, world) = Playing(board =>
        {
            Hand(board, Physicals, 0);
            card = board.CreateCard(requiredEvent, board.Seats[0].Hand);
            wild = board.CreateCard("01011", board.Seats[0].Hand);
            mental = board.CreateCard(Mentals, board.Seats[0].Hand);
        }, heroes: ["captain_marvel"], abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        int held = world.Seats[0].Hand.Cards.Count;
        runner.Act(world, action, [wild!.ObjectId, mental!.ObjectId], []);
        // The requirement declared the wild physical, so the energy branch
        // did not draw. The event and both generators have left the hand.
        Assert.Equal(held - 3, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void EveryRepresentablePaidResourceChoiceRemainsAdvertised()
    {
        const string forJustice = "01060";
        Card? card = null;
        Card? doubleEnergy = null;
        Card? doubleMental = null;
        Card? safeTriple = null;
        Card? ambiguousTriple = null;
        var runner = AuthoredCards.Runner();
        var(_, world) = Playing(board =>
        {
            Hand(board, Physicals, 0);
            card = board.CreateCard(forJustice, board.Seats[0].Hand);
            doubleEnergy = board.CreateCard("01088", board.Seats[0].Hand);
            doubleMental = board.CreateCard("01089", board.Seats[0].Hand);
            safeTriple = board.CreateCard("01014", board.Seats[0].Hand);
            ambiguousTriple = board.CreateCard("21183", board.Seats[0].Hand);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        }, hero: true, abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Contains(price.Generators, source => source.Effect == doubleEnergy!.ObjectId);
        Assert.Contains(price.Generators, source => source.Effect == doubleMental!.ObjectId);
        Assert.Contains(price.Generators, source => source.Effect == safeTriple!.ObjectId);
        Assert.Contains(price.Generators, source => source.Effect == ambiguousTriple!.ObjectId);
    }
}
