using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic resource costs isolate preflight and resumed contribution eligibility.</summary>
public sealed class HandResourceFormCommitTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:the-golden-rules")]
    [Rule("rr:resource.1")]
    public void SuspendedPaymentRechecksTheUncommittedHandSourcesLiveForm(bool changeForm)
    {
        // Resources come from "discarding cards from their hand"; printed 10007
        // restricts that spend: "Spend this card only in hero form."
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"10007","handResourceForm":"hero","abilities":[]},
              {"card":"01006","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"spend":"Y"},
                "effect":{"placeCounters":{"card":"this","counter":"test","count":1}}}]},
              {"card":"01030","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
                "cost":{"dealDamage":{"cards":"this","amount":2}},"effect":{"generate":"Y"}}]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}}]}
            ]}
            """));
        Card? source = null;
        Card? generator = null;
        Card? handCard = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, "01006");
            generator = board.CreateCard("01030",
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            handCard = board.CreateCard("10007", board.Seats[0].Hand);
            InPlay(board, "01092");
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances,
            offer => offer.Verb == Game.ActionVerb && offer.AnchorId == source!.ObjectId);
        int[] paying = [generator!.ObjectId, handCard!.ObjectId];
        var allocation = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.Allocate(Assert.Single(action.Costs!), paying));
        game.Resolve(new Decision(action.Id, [], paying, Allocations: allocation));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(DeckType.HandsArea, handCard.Area.Type);
        if (changeForm)
        {
            Forms.Change(world.Seats[0], world.Facts);
            Assert.False(runner.CanUseHandResources(world, handCard));
            Assert.Throws<RulesNotImplementedException>(() => game.Resolve(Decision.Decline));
            Assert.Equal(DeckType.HandsArea, handCard.Area.Type);
            Assert.Equal(0, source!.Tokens.GetValueOrDefault("c_test"));
            // The earlier generator cost is committed; this rejection promises no rollback.
            Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
            return;
        }
        game.Resolve(Decision.Decline);
        Assert.Equal(DeckType.DiscardPile, handCard.Area.Type);
        Assert.Equal(1, source!.Tokens.GetValueOrDefault("c_test"));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void AnUnrestrictedEmptyResourceContributionRemainsIndependentOfEligibility()
    {
        // Synthetic hand identity has no printed resource icons. The engine's
        // existing zero-cost contribution contract permits it when no text restricts it.
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 1, 73);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001b", seat.Hero);
        for (int i = 0; i < 8; i++) world.CreateCard("01003", seat.Deck);
        var source = world.CreateCard("01006", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var empty = world.CreateCard("01001a", seat.Hand);
        var runner = new AbilityRunner(AbilityBook.None);
        Assert.Equal("", runner.ResourcesGeneratedBy(world, empty, null));
        Assert.True(runner.CanUseHandResources(world, empty));
        var receipt = AbilityResourcePayment.Spend(world, runner,
            new(source, 0, [empty.ObjectId], 0, ""), new List<GameEvent>());
        Assert.True(receipt.IsComplete);
        Assert.Equal(DeckType.DiscardPile, empty.Area.Type);
    }
}
