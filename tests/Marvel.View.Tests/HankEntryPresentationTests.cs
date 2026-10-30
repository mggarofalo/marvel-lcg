using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class HankEntryPresentationTests
{
    [Fact]
    public void AnsweringPlayerSeesTheActualEntryEffect()
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));
        var world = new World(cards, players: 2, seed: 7) { Abilities = runner };
        var seat = world.CreateSeat("Scott");
        var other = world.CreateSeat("Other");
        other.IdentityCard = world.CreateCard("01001a,01001b", other.Hero);
        seat.IdentityCard = world.CreateCard("06001a,06001b", seat.Hero);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 12; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        var hank = world.CreateCard("12011", seat.Hand);
        var resource = world.CreateCard("12007", seat.Hand);
        CardPlay.Play(world, world.Facts, runner, seat, hank, [resource.ObjectId], []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var scope = new RestrictedVisibilityPolicy(0).Authorize(null, 2);
        var visible = WorldProjection.For(world, prompt, [], scope);
        var choice = Assert.Single(PromptPresentation.From(visible.Prompt!, visible.World).Affordances);
        Assert.Contains("Place 1 pym counter", choice.Description);
        Assert.Contains("+1 hit point", choice.Description);
        Assert.Contains("Declining", choice.Description);
        Assert.Null(WorldProjection.For(world, prompt, [],
            new RestrictedVisibilityPolicy(1).Authorize(null, 2)).Prompt);
        Assert.Null(WorldProjection.For(world, prompt, [], ViewScope.None).Prompt);
    }
    [Fact]
    public void CardPlayOfferProjectsOptionalEntryMeaningBeforePayment()
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));
        var world = new World(cards, players: 1, seed: 7) { Abilities = runner };
        var seat = world.CreateSeat("Scott");
        seat.IdentityCard = world.CreateCard("06001a,06001b", seat.Hero);
        for (int i = 0; i < 20; i++) world.CreateCard("01088", seat.Deck);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 12; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        var hank = world.CreateCard("12011", seat.Hand);
        var game = Game.Begin(world, cards, runner);
        while (game.Phase == GamePhase.Mulligan)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        var raw = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == hank.ObjectId && offer.Verb == CardPlay.Verb);
        Assert.Contains("Optional entry interrupt", raw.Description);
        var visible = WorldProjection.For(world, game.Pending, [], new RestrictedVisibilityPolicy(0).Authorize(null, 1));
        var shown = Assert.Single(PromptPresentation.From(visible.Prompt!, visible.World).Affordances,
            offer => offer.Id == raw.Id);
        Assert.Contains("per resource overpaid (maximum 4)", shown.Description);
        Assert.Contains("+1 hit point", shown.Description);
    }

}
