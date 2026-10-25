using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HeroDefenseTimingTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    [Rule("rr:defend-defense.2")]
    [Rule("rr:attack-enemy-activation.step.2")]
    public void BasicDefenseOffersItsInterruptAfterCommitmentBeforeBoosts()
    {
        // "A hero must exhaust to use this power." The selected hero and seat
        // must be known before a "when your hero defends" interrupt is offered.
        var (world, runner, hero, source, _) = Board(labeled: false);
        var events = new List<GameEvent>();
        var defense = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(Question.Defender, defense.Asking);
        Sequence.Answer(world, Cards, runner, defense, Decision.Take(hero.ObjectId), events);
        Assert.False(hero.Ready);
        Assert.Single(world.Cards, card => card.Area.Type == DeckType.BoostCardsDeck);
        var interrupt = Sequence.Work(world, Cards, runner, events);
        Assert.NotNull(interrupt);
        Assert.Equal(Question.Opportunity, interrupt.Asking);
        Assert.Equal(TimingPriority.Interrupt, interrupt.When);
        Assert.Equal(0, interrupt.Player);
        Assert.Equal("Spider-Man is defending — interrupt?", interrupt.DisplayQuestion);
        Assert.Contains("defending against Rhino", interrupt.Description);
        Assert.Equal([hero.ObjectId, world.Attack!.Enemy], interrupt.ContextCardIds);
        Assert.Contains(interrupt.Affordances, offer => offer.AnchorId == source.ObjectId);
        Assert.Equal(0, hero.Damage);
        Assert.Single(world.Cards, card => card.Area.Type == DeckType.BoostCardsDeck);
    }

    [Theory]
    [InlineData("envelope")]
    [InlineData("inner")]
    [InlineData("declaration")]
    [Rule("rr:defend-defense.4.1")]
    [Rule("rr:defend-defense.4.2")]
    public void DefenseOffersItsInterruptBeforeTheUnderlyingEffect(string mode)
    {
        // "Abilities that trigger 'when your hero defends against an attack'
        // can be triggered when resolving a defense-labeled ability."
        var (world, runner, hero, source, drawn) = Board(labeled: true, mode: mode);
        var events = new List<GameEvent>();
        var ability = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(Question.Opportunity, ability.Asking);
        Sequence.Answer(world, Cards, runner, ability,
            Decision.Take(Assert.Single(ability.Affordances).Id), events);
        Assert.Equal(hero.ObjectId, world.Attack!.Defender);
        Assert.True(hero.Ready);
        Assert.Equal(DeckType.PlayerDeck, drawn.Area.Type);
        var interrupt = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(TimingPriority.Interrupt, interrupt.When);
        Assert.Contains(interrupt.Affordances, offer => offer.AnchorId == source.ObjectId);
        Sequence.Answer(world, Cards, runner, interrupt, Decision.Decline, events);
        for (int decisions = 0; ; decisions++)
        {
            var next = Sequence.Work(world, Cards, runner, events);
            if (next is null) break;
            Assert.True(decisions < 10, next.Label);
            Sequence.Answer(world, Cards, runner, next, Decision.Decline, events);
        }
        Assert.Equal(DeckType.HandsArea, drawn.Area.Type);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.True(hero.Ready);
        Assert.Null(world.Attack);
    }

    [Theory]
    [InlineData("envelope")]
    [InlineData("inner")]
    [Rule("rr:defend-defense.4.2")]
    [Rule("rr:defend-defense.4.3")]
    public void ExpertDefenseResolvesDuringALabeledDefenseWithoutBasicDamageReduction(string mode)
    {
        // "can be triggered when resolving a defense-labeled ability"; that ability
        // "does not use the hero's DEF value to reduce the amount of damage dealt".
        var (world, runner, hero, expert, drawn) = Board(true, mode: mode, expert: true);
        var events = new List<GameEvent>();
        var ability = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, ability,
            Decision.Take(Assert.Single(ability.Affordances).Id), events);
        var interrupt = Sequence.Work(world, Cards, runner, events)!;
        var offer = Assert.Single(interrupt.Affordances, value => value.AnchorId == expert.ObjectId);
        Sequence.Answer(world, Cards, runner, interrupt, Decision.Take(offer.Id), events);
        for (int decisions = 0; ; decisions++)
        {
            var next = Sequence.Work(world, Cards, runner, events);
            if (next is null) break;
            Assert.True(decisions < 10, next.Label);
            Sequence.Answer(world, Cards, runner, next, Decision.Decline, events);
        }
        Assert.Equal(DeckType.DiscardPile, expert.Area.Type);
        Assert.Equal(DeckType.HandsArea, drawn.Area.Type);
        Assert.Equal(3, hero.Damage);
        Assert.True(hero.Ready);
        Assert.Equal(3, StateFields.Modified(world, hero, "defense", Cards, world.Players));
        Assert.Null(world.Attack);
    }

    [Fact]
    public void RepeatedDefenseBodiesInOneAbilityDoNotReinitiateItsLabel()
    {
        // The DSL represents one labeled ability with two effect bodies, not two
        // triggered abilities. This authoring convention must survive suspension.
        var (world, runner, _, source, _) = Board(true, mode: "repeated");
        var events = new List<GameEvent>();
        var ability = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, ability,
            Decision.Take(Assert.Single(ability.Affordances).Id), events);
        int defenses = 0;
        for (int decisions = 0; ; decisions++)
        {
            var next = Sequence.Work(world, Cards, runner, events);
            if (next is null) break;
            Assert.True(decisions < 10, next.Label);
            if (next.Affordances.Any(offer => offer.AnchorId == source.ObjectId)) defenses++;
            Sequence.Answer(world, Cards, runner, next, Decision.Decline, events);
        }
        Assert.Equal(1, defenses);
        Assert.Equal(2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void DeclarationStageIsAnInterruptBeforeAnyDefenderHasCommitted()
    {
        // The legacy stage condition names the selection procedure; a new
        // committed-defense occurrence must not reuse its pre-choice window.
        var (world, runner, hero, source, _) = Board(
            labeled: false, eventName: "WhenDefenderDeclared");
        var interrupt = Sequence.Work(world, Cards, runner, [])!;
        Assert.Equal(Question.Opportunity, interrupt.Asking);
        Assert.Contains(interrupt.Affordances, offer => offer.AnchorId == source.ObjectId);
        Assert.True(hero.Ready);
        Assert.Equal(-1, world.Attack!.Defender);
        Assert.Null(world.Agenda.Occurrence!.ActorFacts);
    }
    private static (World World, AbilityRunner Runner, Card Hero, Card Source, Card Drawn)
        Board(bool labeled, string eventName = "WhenHeroDefends", string mode = "envelope", bool expert = false)
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        var hero = world.CreateCard("01001a", seat.Hero);
        seat.IdentityCard = hero;
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var source = world.CreateCard(expert ? "03033" : "01007", expert ? seat.Hand : world.AreaOf(
            DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01081", world.AreaOf(
            DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01088", seat.Deck);
        world.CreateCard("01089", seat.Deck);
        var drawn = world.CreateCard("01087", seat.Deck);
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        // Synthetic observers exercise engine timing without admitting pack content.
        string defense = labeled ? """
            ,{"card":"01081","abilities":[{
              "trigger":{"event":"WhenAttackInitiated","timing":"Interrupt","subject":"game"},
              "labels":["defense"],
              "effect":{"draw":{"player":"you","count":1}}}]}
            """ : "";
        if (mode != "envelope")
        {
            defense = defense.Replace("\"labels\":[\"defense\"],", "", StringComparison.Ordinal);
            const string draw = "{\"draw\":{\"player\":\"you\",\"count\":1}}";
            string body = mode == "inner" ? "{\"defense\":{\"effect\":" + draw + "}}"
                : "{\"seq\":[{\"declareDefender\":{\"card\":\"you\"}}," + draw + "]}";
            if (mode == "repeated")
                body = "{\"seq\":[{\"defense\":{\"effect\":" + draw + "}},{\"defense\":{\"effect\":" + draw + "}}]}";
            defense = defense.Replace(draw, body, StringComparison.Ordinal);
        }
        var book = JsonNode.Parse($$$$"""
            {"cards":[{"card":"01007","abilities":[{
              "trigger":{"event":"{{{{eventName}}}}","timing":"Interrupt","subject":"you"},
              "effect":{"draw":{"player":"you","count":1}}}]}{{{{defense}}}}]}
            """)!;
        if (expert)
        {
            var authored = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
            book["cards"]![0] = authored["cards"]!.AsArray().Single(row => row!["card"]!.GetValue<string>() == "03033")!.DeepClone();
        }
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(),
            1, 1, Subject: villain.ObjectId, Seat: 0));
        return (world, runner, hero, source, drawn);
    }
}
