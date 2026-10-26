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

internal sealed class PreparationResolutionFixture
{
    internal CardCatalog Cards { get; } = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    internal World World { get; }
    internal AbilityRunner Runner { get; }
    internal Card Preparation { get; }
    internal Card Suit { get; }
    internal Card Hero { get; }
    internal Card Villain { get; }
    internal List<GameEvent> Events { get; } = [];

    internal PreparationResolutionFixture()
    {
        Runner = new AbilityRunner(AbilityCatalog.Parse("""
        {"cards":[
          {"card":"08010","abilities":[{
            "trigger":{"event":"WhenCardEntersPlay","timing":"Response","subject":"game"},
            "cost":{"discard":"this"},
            "effect":{"choose":{"options":[
              {"giveStatus":{"card":{"query":"villain"},"status":"stunned"}},
              {"giveStatus":{"card":{"query":"villain"},"status":"confused"}}
            ]}}
          }]},
          {"card":"08001a","abilities":[{
            "trigger":{"event":"WhenPreparationAbilityResolved","timing":"Response","subject":"you"},
            "effect":{"dealDamage":{"cards":{"query":"villain"},"amount":1}}
          }]},
          {"card":"08009","abilities":[{
            "trigger":{"event":"WhenPreparationAbilityResolved","timing":"Response","subject":"you","form":"hero"},
            "cost":{"exhaust":"this"},"effect":{"ready":"you"}
          }]}
        ]}
        """));
        World = new World(Cards, 2, 12345) { Abilities = Runner };
        var first = World.CreateSeat("Spider-Man");
        first.IdentityCard = World.CreateCard("01001a", first.Hero);
        var second = World.CreateSeat("Black Widow");
        Hero = second.IdentityCard = World.CreateCard("08001a", second.Hero);
        Hero.Exhaust();
        World.CreateCard("01087", first.Deck);
        World.CreateCard("01087", second.Deck);
        Villain = World.CreateCard("01094", World.AreaOf(DeckType.VillainArea));
        Preparation = World.CreateCard("08010", World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        Suit = World.CreateCard("08009", World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        World.CreateCard("08009", World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        World.Agenda.Add(new PhaseStep(new LifecycleOperation(LifecycleKind.CardEntersPlay), 1, 1, Subject: Villain.ObjectId, Seat: 1));
    }

    internal Prompt Work() => Assert.IsType<Prompt>(Sequence.Work(World, Cards, Runner, Events));
    internal void Answer(Prompt prompt, int source)
    {
        var offer = Assert.Single(prompt.Affordances, option => option.AnchorId == source);
        Sequence.Answer(World, Cards, Runner, prompt, Decision.Take(offer.Id), Events);
    }
}
