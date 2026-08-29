using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreFinalCardBatchTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:each-player.1")]
    [Rule("rr:you-your")]
    [Fact]
    public void UnderAttackPreservesEachPlayersChoiceAndHero()
    {
        // "The first player decides the order in which the 'each player'
        // effect resolves." Each answer must still use the answering player's
        // hero when the chosen branch runs.
        var world = Board(players: 2);
        var scheme = world.CreateCard("01151", world.AreaOf(DeckType.SideSchemesArea));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        runner.WhenRevealed(world, scheme, player: 0);
        var ordering = Sequence.Work(world, Cards, runner, [])!;
        var order = Assert.Single(ordering.Affordances);
        int[] identities = [
            world.Seats[1].IdentityCard.ObjectId,
            world.Seats[0].IdentityCard.ObjectId,
        ];
        Sequence.Answer(world, Cards, runner, ordering,
            new Decision(order.Id, identities), []);

        var first = Sequence.Work(world, Cards, runner, [])!;
        Assert.Equal(1, first.Player);
        Sequence.Answer(world, Cards, runner, first, Decision.Take(1), []);
        var second = Sequence.Work(world, Cards, runner, [])!;
        Assert.Equal(0, second.Player);
        Sequence.Answer(world, Cards, runner, second, Decision.Take(0), []);
        Sequence.Finish(world, Cards, runner, []);

        Assert.Equal(3, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(2, scheme.Tokens["k_threat"]);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:resource.1")]
    [Fact]
    public void ElectromagneticBacklashCountsEachPlayersPrintedEnergyIcons()
    {
        // "For each printed [energy] resource a player discards this way, that
        // player takes 1 damage." Energy Absorption prints three icons, so it
        // deals three damage while a card with no icon deals none.
        var world = Board(players: 2);
        foreach (var seat in world.Seats)
        {
            for (int card = 0; card < 5; card++)
            {
                world.CreateCard("01086", seat.Deck);
            }
        }
        world.CreateCard("01014", world.Seats[0].Deck);
        world.CreateCard("01002", world.Seats[1].Deck);
        var backlash = world.CreateCard("01174", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        runner.WhenRevealed(world, backlash, player: 0);
        var ordering = Sequence.Work(world, Cards, runner, [])!;
        var order = Assert.Single(ordering.Affordances);
        var identities = world.Seats.Select(seat => seat.IdentityCard.ObjectId).ToArray();
        Sequence.Answer(world, Cards, runner, ordering,
            new Decision(order.Id, identities), []);
        Sequence.Finish(world, Cards, runner, []);

        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
    }

    [Rule("rr:resource.1")]
    [Rule("rr:alteration-effect")]
    [Fact]
    public void RepulsorBlastCountsEveryPrintedEnergyResource()
    {
        var world = Board(players: 1, identity: "01029a,01029b");
        world.Seats[0].IdentityCard.TurnTo("01029a");
        var target = world.CreateCard(
            "01114", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        for (int card = 0; card < 5; card++)
        {
            world.CreateCard("01090", world.Seats[0].Deck);
        }
        world.CreateCard("01014", world.Seats[0].Deck);
        var blast = world.CreateCard("01031", world.Seats[0].Hand);
        var payment = world.CreateCard("01086", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        var action = runner.Actions(world, 0).Single(ability => ability.Card == blast.ObjectId);
        runner.Act(world, action, [payment.ObjectId], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        var prompt = Sequence.Work(world, Cards, runner, [])!;
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(target.ObjectId), []);
        Sequence.Finish(world, Cards, runner, []);

        Assert.Equal(7, target.Damage);
    }

    [Rule("rr:boost-boost-icon")]
    [Rule("rr:threat")]
    [Fact]
    public void RitualCombatUsesTheDiscardedCardsPrintedBoostValue()
    {
        var world = Board(players: 1);
        var main = world.CreateCard("01116a", world.AreaOf(DeckType.MainSchemesArea));
        world.CreateCard("01151", world.AreaOf(DeckType.EncounterDeck));
        var ritual = world.CreateCard("01159", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        runner.WhenRevealed(world, ritual, player: 0);
        var prompt = Sequence.Work(world, Cards, runner, [])!;
        Assert.Equal(Question.Option, prompt.Asking);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(1), []);
        Sequence.Finish(world, Cards, runner, []);

        Assert.Equal(4, main.Tokens["k_threat"]);
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Equal(["01151"], world.AreaOf(DeckType.EncounterDeck).Cards
            .Select(card => card.FaceId));
        Assert.Equal(1, main.Tokens[EncounterDeck.AccelerationToken]);
    }

    [Rule("rr:choose-game-element.3.1")]
    [Rule("rr:threat")]
    [Fact]
    public void LegalPracticeDiscardsOneToFiveCardsAndRemovesThatMuchThreat()
    {
        var world = Board(players: 1, identity: "01019a,01019b");
        world.Seats[0].IdentityCard.TurnTo("01019b");
        var scheme = world.CreateCard("01151", world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", 5);
        var legalPractice = world.CreateCard("01023", world.Seats[0].Hand);
        var first = world.CreateCard("01087", world.Seats[0].Hand);
        var second = world.CreateCard("01087", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        var action = runner.Actions(world, 0).Single(ability => ability.Card == legalPractice.ObjectId);
        var events = runner.Act(world, action, [], []).ToList();
        var choice = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, legalPractice, 0, choice.Index, choice.Tier)!;
        Assert.Equal(1, Assert.Single(prompt.Affordances).Targets!.Min);
        var answer = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, answer,
            Decision.Take(scheme.ObjectId, [first.ObjectId, second.ObjectId], []), events);
        Sequence.Finish(world, Cards, runner, events);

        Assert.Equal(3, scheme.Tokens["k_threat"]);
        Assert.DoesNotContain(first, world.Seats[0].Hand.Cards);
        Assert.DoesNotContain(second, world.Seats[0].Hand.Cards);
        var threat = Assert.Single(events.OfType<FieldSet>(), change =>
            change.Card == scheme.ObjectId && change.Verb == "Remove_Threat");
        Assert.Equal(Steps.TurnAction, threat.Trigger);
    }

    [Rule("rr:crisis-icon.1")]
    [Rule("rr:ability.3")]
    [Fact]
    public void LegalPracticeIsNotOfferedWhenCrisisLeavesNoThwartableScheme()
    {
        // "Player cards cannot remove threat from the main scheme." A card
        // with no legal scheme must not take payment and ask an empty question.
        var world = Board(players: 1, identity: "01019a,01019b");
        world.Seats[0].IdentityCard.TurnTo("01019b");
        var main = world.CreateCard("01116a", world.AreaOf(DeckType.MainSchemesArea));
        main.PlaceTokens("k_threat", 5);
        world.CreateCard("01125", world.AreaOf(DeckType.SideSchemesArea));
        var legalPractice = world.CreateCard("01023", world.Seats[0].Hand);
        world.CreateCard("01087", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        Assert.DoesNotContain(
            runner.Actions(world, 0), ability => ability.Card == legalPractice.ObjectId);
    }

    private static World Board(int players, string identity = "01001a,01001b")
    {
        var world = new World(Cards, players);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(identity, seat.Hero);
        }
        return world;
    }
}
