using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowPreparationCardTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:resolve.2")]
    public void ActualBiteReactsToEitherEngagedSeatAndPublishesOwnReceipt(int seat)
    {
        // "An ability is resolved when it is triggered and one or more of its effects resolve."
        var f = new PreparationPrintedFixture(entrySeat: seat);
        var entry = Entry(f);
        Assert.Equal(PlayArea.Of(seat), f.Minion.Area.PlayArea);
        Answer(f, entry, f.Bite.ObjectId);
        var receiptPrompt = Work(f);
        var receipt = Assert.IsType<ResolvedPreparationAbility>(f.World.Agenda.Occurrence!.ActorFacts!.PreparationAbility);
        Assert.Equal("08010", receipt.Face);
        Assert.Equal(1, receipt.Controller);
        Assert.Equal(DeckType.DiscardPile, f.Bite.Area.Type);
        Assert.Equal(2, f.Minion.Damage);
        Assert.True(Statuses.Has(f.World, f.Minion, Statuses.Stunned));
        Assert.Equal(1, receiptPrompt.Player);
        Assert.Equal(2, receiptPrompt.Affordances.Count);
        Sequence.Answer(f.World, f.Cards, f.Runner, receiptPrompt, Decision.Decline, f.Events);
        Assert.Null(Sequence.Work(f.World, f.Cards, f.Runner, f.Events));
    }

    [Theory]
    [InlineData("01186", true)]
    [InlineData("01101", false)]
    [Rule("rr:damage.step.6")]
    [Rule("rr:damage.step.7")]
    public void ActualBiteStunsLokiAfterHealingButCannotStunTheDefeatedCopy(string top, bool heals)
    {
        // Step 6 interrupts precede step 7: "If a character has zero or fewer remaining hit points, it is defeated."
        var f = new PreparationPrintedFixture("06028", entrySeat: 1);
        var entry = Entry(f);
        f.Minion.TakeDamage(4);
        f.World.CreateCard(top, f.World.AreaOf(DeckType.EncounterDeck));
        Answer(f, entry, f.Bite.ObjectId);
        var responses = Work(f);
        Assert.Equal(heals ? DeckType.EngagedEnemiesArea : DeckType.EncounterDiscardPile, f.Minion.Area.Type);
        Assert.Equal(heals, Statuses.Has(f.World, f.Minion, Statuses.Stunned));
        Assert.Equal(0, f.Minion.Damage);
        Assert.Equal(2, responses.Affordances.Count);
        Assert.False(f.World.IsOver);
        Sequence.Answer(f.World, f.Cards, f.Runner, responses, Decision.Decline, f.Events);
        Assert.Null(Sequence.Work(f.World, f.Cards, f.Runner, f.Events));
    }

    [Fact]
    [Rule("rr:referential-ability.step.2")]
    public void ActualSynthSuitReferencesBlackWidowIdentityRatherThanItsControllerOrTheAlly()
    {
        // A shared title first references "Cards associated with the same identity".
        var world = ThorActualFixture.Board("06001a", 2);
        World.MoveToTop(world.Seats[0].IdentityCard, world.AreaOf(DeckType.RemovedArea));
        world.Seats[0].IdentityCard = world.CreateCard("08001a,08001b", world.Seats[0].Hero);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var widow = world.Seats[0].IdentityCard;
        var other = world.Seats[1].IdentityCard;
        var ally = world.CreateCard("01058", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        world.CreateCard("08009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Assert.Equal(3, StateFields.Modified(world, widow, "defense", world.Facts, 2));
        Assert.Equal(world.Facts.Attributes(other.FaceId).GetValueOrDefault("DEF", "0"),
            StateFields.Modified(world, other, "defense", world.Facts, 2).ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(0, StateFields.Modified(world, ally, "defense", world.Facts, 2));
        widow.TurnTo("08001b");
        Assert.Equal(0, StateFields.Modified(world, widow, "defense", world.Facts, 2));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:resource-ability.1.1")]
    public void ActualGauntletPaysActualPreparationAndMatchingAbilityWithDeclaredWildLedger(bool hero)
    {
        // A resource for a specific type "cannot be triggered when paying a cost not of that type".
        var board = PreparationResourceFixture.Board(hero, face: "08010");
        var offer = Assert.Single(board.Game.Pending!.Affordances, a => a.Verb == "Play" && a.AnchorId == board.Target.ObjectId);
        ResourceAllocation[] allocations = [new(board.First.ObjectId, 0, "Y")];
        Assert.True(ResourcePayment.Allows(Assert.Single(offer.Costs!), [board.First.ObjectId], null, allocations));
        board.Game.Resolve(new Decision(offer.Id, [board.World.Seats[0].IdentityCard.ObjectId],
            [board.First.ObjectId], Allocations: allocations));
        Assert.Equal(DeckType.UpgradesArea, board.Target.Area.Type);
        Assert.False(board.First.Ready);
        var paid = AbilityResourcePayment.Spend(board.World, board.World.Abilities!,
            new(board.Target, 0, [board.Second.ObjectId], 1, "B")
            { Allocations = [new(board.Second.ObjectId, 0, "B")] }, []);
        Assert.True(paid.IsComplete);
        Assert.Equal("B", paid.Paid);
        Assert.False(board.Second.Ready);
    }

    [Fact]
    [Rule("rr:resolve.2")]
    public void AnotherControllersBiteDoesNotOfferWidowmakerOrOwnSuit()
    {
        // The printed response requires a Preparation "you control".
        var f = new PreparationPrintedFixture();
        foreach (var suit in f.World.Cards.Where(card => card.FaceId == "08009"
            && card != f.Suit).ToArray())
            World.MoveToTop(suit, f.World.AreaOf(DeckType.RemovedArea));
        World.MoveToTop(f.Bite, f.World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var entry = Entry(f);
        Assert.Equal(0, entry.Player);
        Answer(f, entry, f.Bite.ObjectId);
        Assert.Null(Sequence.Work(f.World, f.Cards, f.Runner, f.Events));
        Assert.Equal(2, f.Minion.Damage);
        Assert.True(f.Suit.Ready);
        Assert.False(f.Hero.Ready);
        Assert.Equal(0, f.Villain.Damage);
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    public void ActualBiteRetainsOriginalControllerAndSourceIncarnationAfterReentry()
    {
        // A reentered card is "considered to be a new copy of the card".
        var f = new PreparationPrintedFixture("01182", entrySeat: 1);
        var observer = f.World.CreateCard("08023",
            f.World.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var entry = Entry(f);
        int incarnation = f.Bite.Incarnation;
        f.Minion.TakeDamage(2);
        Answer(f, entry, f.Bite.ObjectId);
        Answer(f, Work(f), observer.ObjectId);
        var choice = Work(f);
        // Synthetic external source movement challenges the retained receipt while a legal interrupt suspends it.
        World.MoveToTop(f.Bite, f.World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Sequence.Answer(f.World, f.Cards, f.Runner, choice, Decision.Take(choice.Affordances[0].Id), f.Events);
        var responses = Work(f);
        var receipt = Assert.IsType<ResolvedPreparationAbility>(f.World.Agenda.Occurrence!.ActorFacts!.PreparationAbility);
        Assert.Equal(1, receipt.Controller);
        Assert.Equal(incarnation, receipt.Incarnation);
        Assert.True(f.Bite.Incarnation > incarnation);
        Assert.Equal(1, responses.Player);
        Assert.Equal(2, responses.Affordances.Count);
        Sequence.Answer(f.World, f.Cards, f.Runner, responses, Decision.Decline, f.Events);
        Assert.Null(Sequence.Work(f.World, f.Cards, f.Runner, f.Events));
    }

    [Fact]
    public void NewRowsHaveNoStartingCountersAndUnauthoredFacesRemainAbsent()
    {
        using var book = System.Text.Json.JsonDocument.Parse(AuthoredCards.Text);
        string[] added = ["08001a", "08005", "08007", "08009", "08010"];
        var rows = book.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(AuthoredCards.ExpectedFaceCount, rows.Length);
        Assert.Equal(added, rows.Skip(623).Take(5).Select(row => row.GetProperty("card").GetString()));
        Assert.All(rows.Skip(623).Take(5), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        Assert.DoesNotContain("11001", AuthoredCards.Book.Authored);
    }

    private static Prompt Work(PreparationPrintedFixture f) =>
        Assert.IsType<Prompt>(Sequence.Work(f.World, f.Cards, f.Runner, f.Events));

    private static Prompt Entry(PreparationPrintedFixture f)
    {
        for (int index = 0; index < 12; index++)
        {
            var prompt = Work(f);
            if (prompt.Affordances.Any(a => a.AnchorId == f.Bite.ObjectId)) return prompt;
            Assert.True(prompt.Cancellable);
            Sequence.Answer(f.World, f.Cards, f.Runner, prompt, Decision.Decline, f.Events);
        }
        throw new InvalidOperationException("Bite entry response was not reached");
    }

    private static void Answer(PreparationPrintedFixture f, Prompt prompt, int source) =>
        Sequence.Answer(f.World, f.Cards, f.Runner, prompt,
            Decision.Take(Assert.Single(prompt.Affordances, a => a.AnchorId == source).Id), f.Events);
}
