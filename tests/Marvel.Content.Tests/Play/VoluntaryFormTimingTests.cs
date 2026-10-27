using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class VoluntaryFormTimingTests
{
    [Theory]
    [InlineData("01019", "b", "a", false)]
    [InlineData("12001", "b", "c", true)]
    [Rule("rr:interrupt.3")]
    [Rule("rr:response")]
    public void GameInterruptSeesOldFaceAndResponseSeesDestination(
        string id, string from, string to, bool threeFaces)
    {
        // rr:interrupt.3 resolves an interrupt "before that triggering condition resolves".
        // rr:response resolves after the triggering condition occurs.
        var (world, game) = Board(id, from, threeFaces, """
            {"trigger":{"event":"WhenFormChanged","timing":"Interrupt","subject":"game"},
             "effect":{"draw":{"player":"you","count":1}}},
            {"trigger":{"event":"WhenFormChanged","timing":"Response","subject":"game"},
             "effect":{"draw":{"player":"you","count":1}}}
            """);
        var seat = world.Seats[0];
        var identity = seat.IdentityCard;
        var offer = threeFaces
            ? Assert.Single(game.Pending!.Affordances, a => a.Verb == Game.ChangeForm && a.Label == id + to)
            : Assert.Single(game.Pending!.Affordances, a => a.Verb == Game.ChangeForm);
        if (!threeFaces) Assert.Equal(Game.ChangeForm, offer.Label);
        var first = game.Resolve(Decision.Take(offer.Id));
        Assert.Equal(id + from, identity.FaceId);
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(0, seat.FormChangedInRound);
        Assert.Empty(first.Events.OfType<CardFormChanged>());
        var second = game.Resolve(Decision.Decline);
        Assert.Same(identity, seat.IdentityCard);
        Assert.Equal(id + to, identity.FaceId);
        var changed = Assert.Single(second.Events.OfType<CardFormChanged>());
        Assert.Equal((id + from, id + to), (changed.From, changed.To));
        Assert.Equal(game.Round, seat.FormChangedInRound);
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        game.Resolve(Decision.Decline);
        Assert.DoesNotContain(game.Pending!.Affordances, a => a.Verb == Game.ChangeForm);
    }

    [Fact]
    [Rule("rr:form-change-form.1")]
    public void DeferredVoluntaryChangeRechecksRoundPermission()
    {
        // Synthetic scheduler boundary: rr:form-change-form.1 permits one voluntary change per round.
        var (world, game) = Board("01019", "b", false, """
            {"trigger":{"event":"WhenFormChanged","timing":"Interrupt","subject":"game"},
             "effect":{"draw":{"player":"you","count":1}}},
            {"trigger":{"event":"WhenFormChanged","timing":"ForcedResponse","subject":"game"},
             "effect":{"draw":{"player":"you","count":1}}}
            """);
        var seat = world.Seats[0];
        var offer = Assert.Single(game.Pending!.Affordances, a => a.Verb == Game.ChangeForm);
        game.Resolve(Decision.Take(offer.Id));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        int hand = seat.Hand.Cards.Count;
        seat.FormChangedInRound = game.Round;
        var result = game.Resolve(Decision.Decline);
        Assert.Equal("01019b", seat.IdentityCard.FaceId);
        Assert.Empty(result.Events.OfType<CardFormChanged>());
        Assert.Equal(hand, seat.Hand.Cards.Count);
        Assert.False(world.Agenda.IsBusy);
        Assert.DoesNotContain(game.Pending!.Affordances, a => a.Verb == Game.ChangeForm);
    }

    [Theory]
    [InlineData("12001", "a", "c", true)]
    [InlineData("12001", "a", "b", false)]
    [Rule("rr:after")]
    public void ActualTaskmasterRespondsToHeroDestinationOnly(string id, string from, string to, bool hero)
    {
        // 04093: "After a player changes to hero form"; 9218c9f1b9506cdd includes hero-to-hero.
        var (world, game) = Board(id, from, true, "", actual: true);
        var boost = world.CreateCard("04064", world.AreaOf(DeckType.EncounterDeck));
        var offer = Assert.Single(game.Pending!.Affordances, a => a.Verb == Game.ChangeForm && a.Label == id + to);
        game.Resolve(Decision.Take(offer.Id));
        Assert.Equal(id + to, world.Seats[0].IdentityCard.FaceId);
        Assert.Equal(hero ? 3 : 0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(hero ? DeckType.EncounterDiscardPile : DeckType.EncounterDeck, boost.Area.Type);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:forced.4")]
    [Rule("rr:form-change-form.1")]
    public void VoluntarySheHulkChangeResolvesTaskmasterBeforeHerResponse(bool accept)
    {
        // rr:forced.4: forced responses "initiate before non-forced responses".
        // 04093 deals boost damage after changing to hero; 01019a may deal 2 to an enemy.
        var (world, game) = Board("01019", "b", false, "", actual: true);
        var seat = world.Seats[0];
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var boost = world.CreateCard("04064", world.AreaOf(DeckType.EncounterDeck));
        var offer = Assert.Single(game.Pending!.Affordances, a => a.Verb == Game.ChangeForm);
        var first = game.Resolve(Decision.Take(offer.Id));
        Assert.Equal("01019a", seat.IdentityCard.FaceId);
        Assert.Equal(game.Round, seat.FormChangedInRound);
        Assert.Equal(3, seat.IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, boost.Area.Type);
        var flip = Assert.Single(first.Events.OfType<CardFormChanged>());
        Assert.Equal((seat.IdentityCard.ObjectId, "01019b", "01019a"), (flip.Card, flip.From, flip.To));
        Assert.Equal(("WhenPlayerInTurn", Game.ChangeForm), (flip.Trigger, flip.Verb));
        var damage = Assert.Single(first.Events.OfType<FieldSet>(), e => e.Card == seat.IdentityCard.ObjectId && e.AbilitySource == villain.ObjectId);
        Assert.Equal("WhenFormChanged", damage.Trigger);
        Assert.NotNull(damage.AbilityOccurrence);
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        var response = Assert.Single(game.Pending.Affordances, a => a.AnchorId == seat.IdentityCard.ObjectId);
        var second = game.Resolve(accept ? Decision.Take(response.Id, [villain.ObjectId], []) : Decision.Decline);
        Assert.Equal(accept ? 2 : 0, villain.Damage);
        Assert.Empty(second.Events.OfType<CardFormChanged>());
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.DoesNotContain(game.Pending.Affordances, a => a.Verb == Game.ChangeForm);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cancel.5.1")]
    [Rule("rr:form-change-form.1")]
    public void CancelledOrNestedChangeDoesNotSpendTheOuterPermission(bool nested)
    {
        // rr:cancel.5.1: a canceled effect "is not considered to have occurred".
        // rr:form-change-form.1 permits one actual voluntary change each round.
        string effect = nested ? """{"changeForm":{"player":"you","to":"hero"}}"""
            : """{"cancelOccurrence":"trigger"}""";
        var (world, game) = Board("01019", "b", false, $$$$"""
            {"trigger":{"event":"WhenFormChanged","timing":"ForcedInterrupt","subject":"game"},
             "cost":{"exhaust":"this"},"effect":{{{{effect}}}}},
            {"trigger":{"event":"WhenFormChanged","timing":"ForcedResponse","subject":"game"},
             "effect":{"draw":{"player":"you","count":1}}}
            """);
        var offer = Assert.Single(game.Pending!.Affordances, a => a.Verb == Game.ChangeForm);
        int hand = world.Seats[0].Hand.Cards.Count;
        var result = game.Resolve(Decision.Take(offer.Id));
        Assert.Equal(nested ? "01019a" : "01019b", world.Seats[0].IdentityCard.FaceId);
        Assert.Equal(0, world.Seats[0].FormChangedInRound);
        Assert.Empty(result.Events.OfType<CardFormChanged>());
        Assert.Equal(hand + (nested ? 1 : 0), world.Seats[0].Hand.Cards.Count);
        Assert.False(world.Agenda.IsBusy);
        Assert.Contains(game.Pending!.Affordances, a => a.Verb == Game.ChangeForm);
    }

    private static (World World, Game Game) Board(
        string id, string from, bool threeFaces, string abilities, bool actual = false)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, 1, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard($"{id}a,{id}b" + (threeFaces ? $",{id}c" : ""), seat.Hero);
        seat.IdentityCard.TurnTo(id + from);
        for (int i = 0; i < 12; i++) world.CreateCard("01087", seat.Deck);
        world.CreateCard(actual ? "04093" : "01114", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        AbilityRunner runner;
        if (actual) runner = AuthoredCards.Runner();
        else
        {
            var local = AbilityCatalog.Parse($$"""
                {"cards":[{"card":"01006","abilities":[{{abilities}}]}]}
                """);
            runner = new AbilityRunner(local);
            world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }
        var game = Game.Begin(world, cards, runner);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.Verb == Game.ResolveMulligans).Id));
        return (world, game);
    }
}
