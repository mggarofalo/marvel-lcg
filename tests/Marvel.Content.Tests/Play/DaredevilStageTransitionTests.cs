using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DaredevilStageTransitionTests
{
    [Rule("rr:response.3")]
    [Rule("rr:consequential-damage.1")]
    [Fact]
    public void ResponseDamageFinishesTheNewVillainsRevealBeforeConsequentialDamage()
    {
        // rr:response.3: "If single effect causes multiple triggering conditions to occur,
        // responses to each of those triggering conditions can be resolved in any order."
        // rr:consequential-damage.1: "Consequential damage is dealt to an ally after resolving
        // abilities that are triggered by the ally attacking or thwarting."
        // Daredevil: "After Daredevil thwarts, deal 1 damage to an enemy."
        // Rhino II reveals Breakin' & Takin'; that child reveal and its threat
        // finish before the thwart response and its consequential damage finish.
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = AuthoredCards.Runner();
        var world = new World(cards, 1) { Abilities = runner };
        var seat = world.CreateSeat("Spider-Man");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        world.CreateCard("01089", seat.Deck);
        var rhino = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var next = world.CreateCard("01095", world.AreaOf(DeckType.VillainDeck));
        rhino.TakeDamage(Damage.Health(world, cards, rhino) - 1);
        var main = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        main.PlaceTokens("k_threat", 3);
        var side = world.CreateCard("01107", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var daredevil = world.CreateCard("01058",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var events = new List<GameEvent>();

        AllyBasicPowers.AllyPower(world, cards, daredevil, main, BasicPowers.ThwartVerb, events);
        var response = Assert.IsType<Prompt>(Sequence.Work(world, cards, runner, events));
        var offer = Assert.Single(response.Affordances, item => item.AnchorId == daredevil.ObjectId);
        Sequence.Answer(world, cards, runner, response, Decision.Take(offer.Id), events);
        var target = Assert.IsType<Prompt>(Sequence.Work(world, cards, runner, events));
        Sequence.Answer(world, cards, runner, target, Decision.Take(rhino.ObjectId), events);
        Assert.Equal(0, daredevil.Damage);
        Assert.Null(Sequence.Work(world, cards, runner, events));

        Assert.False(world.IsOver);
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(DeckType.RemovedArea, rhino.Area.Type);
        Assert.Equal(DeckType.VillainArea, next.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, side.Area.Type);
        Assert.Equal(3, side.Tokens["k_threat"]);
        Assert.Equal(1, main.Tokens["k_threat"]);
        Assert.Equal(1, daredevil.Damage);
        int revealed = events.FindIndex(item => item is CardsMoved moved
            && moved.Cards.Any(card => card.Card == side.ObjectId));
        int consequential = events.FindIndex(item => item is FieldSet field
            && field.Card == daredevil.ObjectId && field.Field == "health");
        Assert.True(revealed >= 0 && consequential > revealed);
    }
}
