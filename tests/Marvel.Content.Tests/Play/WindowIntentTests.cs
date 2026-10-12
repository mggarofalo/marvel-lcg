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

public sealed class WindowIntentTests : ChoosingCardsTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:response.1")]
    public void MockingbirdOffersExactEnemyInstructionsAndResolvesOnce(bool multiple)
    {
        // "A response ability may be executed after the specified triggering condition occurs."
        var (world, runner, source, occurrence, prompt) = Bird(multiple);
        var offer = Assert.Single(prompt.Affordances);
        Assert.Contains("Mockingbird entered play", prompt.Description);
        Assert.Equal(AbilityType.Response, offer.Timing);
        Assert.Equal(multiple ? 2 : 1, offer.WindowChoices.Count);
        Card enemy = multiple ? world.Cards[offer.WindowChoices[^1].TargetId!.Value]
            : world.TheCardIn(DeckType.VillainArea)!;
        var intention = Assert.Single(offer.WindowChoices, item => item.TargetId == enemy.ObjectId);
        Assert.Equal($"Stun {Cards.Title(enemy.FaceId)}", intention.Label);
        var events = new List<GameEvent>();
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(source.ObjectId, [enemy.ObjectId], []), events);
        Assert.True(Statuses.Has(world, enemy, Statuses.Stunned));
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Null(Offering.Work(world, runner, occurrence, WindowKind.Response, []));
    }

    [Fact]
    public void PassDoesNotSpendOrStunAndInvalidRecipientRejectsBeforeCosts()
    {
        var (world, runner, source, occurrence, prompt) = Bird(true);
        string digest = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => runner.Resolve(world, occurrence,
            new PendingAbility(source.ObjectId, AbilityType.Response, 0), [], [source.ObjectId]));
        Assert.Equal(digest, world.Digest().Canonical());
        Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, []);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.False(Statuses.Has(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Stunned));
    }

    [Fact]
    public void ForcedChoiceEmitsOneSuspendedReceiptAndRequiredSourceLinkedPrompt()
    {
        var (world, _, source, occurrence, _) = Bird(true);
        var runner = Synthetic("ForcedResponse", "", """
            {"chooseCard":{"from":{"query":"enemies"},"effect":{"giveStatus":{"card":"chosen","status":"stunned"}}}}
            """);
        world.Windows.Close();
        world.Abilities = runner;
        var events = new List<GameEvent>();
        Assert.Null(Offering.Work(world, runner, occurrence, WindowKind.Response, events));
        var receipt = Assert.Single(events.OfType<AbilityWindowResult>());
        Assert.False(receipt.Completed);
        Assert.Equal("Ability_Initiated", receipt.Verb);
        Assert.Equal(source.ObjectId, receipt.Source);
        Assert.Equal(occurrence.Id, receipt.Occurrence);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, source, 0, waiting.Index, waiting.Tier)!;
        Assert.False(prompt.Cancellable);
        Assert.Contains(source.ObjectId, prompt.ContextCardIds);
        var resumed = runner.Chose(world, source, 0, waiting.Index,
            Decision.Take(world.TheCardIn(DeckType.VillainArea)!.ObjectId), waiting.Tier);
        Assert.Empty(resumed.OfType<AbilityWindowResult>());
        Assert.True(Statuses.Has(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Stunned));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DrawRecipientNamesCharacterAndCostlyChoiceKeepsExplicitPayment(bool costly)
    {
        var (world, _, source, occurrence, _) = Bird(false);
        var runner = Synthetic("Response", costly ? "\"cost\":{\"spend\":\"G\"}," : "", """
            {"chooseCard":{"from":{"query":"identities"},"effect":{"draw":{"player":"chosenPlayer","count":1}}}}
            """);
        world.Abilities = runner;
        var offer = runner.Describe(world, new PendingAbility(source.ObjectId, AbilityType.Response, 0));
        if (costly) { Assert.Empty(offer.WindowChoices); Assert.NotEmpty(offer.CostOptions); return; }
        var intention = Assert.Single(offer.WindowChoices);
        Assert.Equal("Peter Parker draws 1 card", intention.Label);
        int before = world.Seats[0].Hand.Cards.Count;
        runner.Resolve(world, occurrence, new PendingAbility(source.ObjectId, AbilityType.Response, 0), [], [intention.TargetId!.Value]);
        Assert.Equal(before + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:initiating-abilities.step.7")]
    public void PayingAWindowCostThatRemovesRecipientKeepsPaidCostAndAsksActualChoice()
    {
        // "Pay the cost(s)." precedes "the ability ... resolves."
        var (world, _, source, occurrence, _) = Bird(false);
        Card remaining = world.CreateCard("01002", source.Area);
        var runner = Synthetic("Response", "\"cost\":{\"discard\":\"this\"},", """
            {"chooseCard":{"from":{"query":"alliesYouControl"},"effect":{"giveStatus":{"card":"chosen","status":"tough"}}}}
            """);
        world.Abilities = runner;
        Assert.Empty(runner.Describe(world, new PendingAbility(source.ObjectId, AbilityType.Response, 0)).WindowChoices);
        var events = runner.Resolve(world, occurrence,
            new PendingAbility(source.ObjectId, AbilityType.Response, 0), [], [source.ObjectId]);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Contains(events, item => item.Verb == "Discard");
        Assert.False(Statuses.Has(world, remaining, Statuses.Tough));
        var waiting = Assert.Single(world.Agenda.Outstanding);
        var asked = runner.Choosing(world, source, 0, waiting.Index, waiting.Tier)!;
        Assert.Equal(remaining.ObjectId, Assert.Single(asked.Affordances).Id);
        Assert.False(asked.Cancellable);
    }

    [Fact]
    public void CompleteRecipientAnswerMatchesTheDurableSeparateChoiceState()
    {
        var (direct, runner, source, occurrence, _) = Bird(true);
        runner.Resolve(direct, occurrence, new PendingAbility(source.ObjectId, AbilityType.Response, 0), [],
            [direct.TheCardIn(DeckType.VillainArea)!.ObjectId]);
        var (deferred, other, same, sameOccurrence, _) = Bird(true);
        other.Resolve(deferred, sameOccurrence, new PendingAbility(same.ObjectId, AbilityType.Response, 0), [], []);
        var waiting = Assert.Single(deferred.Agenda.Outstanding);
        other.Chose(deferred, same, 0, waiting.Index,
            Decision.Take(deferred.TheCardIn(DeckType.VillainArea)!.ObjectId), waiting.Tier);
        Assert.Equal(direct.Digest().Canonical(), deferred.Digest().Canonical());
        Assert.Equal(occurrence.Resolutions, sameOccurrence.Resolutions);
    }

    private static AbilityRunner Synthetic(string timing, string cost, string effect) => new(AbilityCatalog.Parse(
        $$"""{"cards":[{"card":"01083","abilities":[{"trigger":{"event":"WhenCardEntersPlay","timing":"{{timing}}","subject":"this"},{{cost}}"effect":{{effect}}}]}]}"""));

    private static (World World, AbilityRunner Runner, Card Source, Occurrence Occurrence, Prompt Prompt) Bird(bool multiple)
    {
        World world = Deal("spider_man");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Card source = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        if (multiple) world.CreateCard("01103", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var occurrence = new Occurrence(71, [Steps.CardEntersPlay], Subject: source.ObjectId, Player: 0);
        return (world, runner, source, occurrence, Offering.Work(world, runner, occurrence, WindowKind.Response, [])!);
    }
}
