using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EventRecipientCommitmentTests : ChoosingCardsTestBase
{
    [Theory]
    [InlineData("01005", DeckType.VillainArea)]
    [InlineData("01060", DeckType.MainSchemesArea)]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:initiating-abilities.step.7")]
    public void ExplicitEventRecipientKeepsPaymentAndEffectOrderOfTheDeferredRoute(string face, DeckType targetArea)
    {
        // "Pay the cost(s)." precedes "its effects resolve".
        var (direct, runner, source, paying) = Board(face);
        int target = direct.TheCardIn(targetArea)!.ObjectId;
        var action = new PendingAbility(source.ObjectId, AbilityType.Action, 0);
        var offer = runner.Describe(direct, action);
        Assert.Contains(target, offer.Targets!.Legal);
        Assert.True(offer.PlaysCard);
        Assert.False(offer.DirectRecipientCommit);
        var actual = runner.Act(direct, action, paying, [target]).ToList();
        Sequence.Finish(direct, Cards, runner, actual);

        var (deferred, other, sameSource, samePaying) = Board(face);
        var expected = other.Act(deferred, new PendingAbility(sameSource.ObjectId, AbilityType.Action, 0),
            samePaying, []).ToList();
        var prompt = Sequence.Work(deferred, Cards, other, expected)!;
        Sequence.Answer(deferred, Cards, other, prompt, Decision.Take(target), expected);
        Sequence.Finish(deferred, Cards, other, expected);
        Assert.Equal(expected.Select(entry => entry.ToString()), actual.Select(entry => entry.ToString()));
        Assert.Equal(deferred.Digest().Canonical(), direct.Digest().Canonical());
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.2")]
    public void InvalidEventRecipientRejectsBeforeAnyPayment()
    {
        // "If the card or ability specifies one or more targets, check that it has at least one valid target."
        var (world, runner, source, paying) = Board("01005");
        string before = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => runner.Act(world,
            new PendingAbility(source.ObjectId, AbilityType.Action, 0), paying, [world.Seats[0].IdentityCard.ObjectId]));
        Assert.Equal(before, world.Digest().Canonical());
    }

    [Fact]
    public void PaymentThatRemovesTheIntendedRecipientLeavesANewExplicitChoiceAndKeepsCostsPaid()
    {
        // A synthetic event contract exposes a public hand recipient to exercise revalidation.
        var (world, _, source, paying) = Board("01005");
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01005","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "effect":{"chooseCard":{"from":{"query":"identitySpecificInYourHand"},
                "effect":{"discard":"chosen"}}}
            }]}]}
            """));
        world.Abilities = runner;
        Card intended = world.CreateCard("01006", world.Seats[0].Hand);
        Card remaining = world.CreateCard("01006", world.Seats[0].Hand);
        var events = runner.Act(world, new PendingAbility(source.ObjectId, AbilityType.Action, 0),
            [paying[0], intended.ObjectId], [intended.ObjectId]).ToList();
        Assert.Equal(DeckType.DiscardPile, intended.Area.Type);
        Assert.Equal(DeckType.DiscardPile, world.Cards[paying[0]].Area.Type);
        Assert.Equal(DeckType.RevealingArea, source.Area.Type);
        var prompt = Sequence.Work(world, Cards, runner, events)!;
        Assert.Contains(prompt.Affordances, offer => offer.Id == remaining.ObjectId);
        Assert.Equal(DeckType.HandsArea, remaining.Area.Type);
    }

    private static (World World, AbilityRunner Runner, Card Source, int[] Paying) Board(string face)
    {
        World world = Deal("spider_man");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        foreach (Card card in world.Seats[0].Hand.Cards.ToArray()) World.MoveToTop(card, world.Seats[0].Deck);
        Card source = world.CreateCard(face, world.Seats[0].Hand);
        Card genius = world.CreateCard("01089", world.Seats[0].Hand);
        Card energy = world.CreateCard("01088", world.Seats[0].Hand);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        return (world, runner, source, face == "01060" ? [genius.ObjectId] : [genius.ObjectId, energy.ObjectId]);
    }
}
