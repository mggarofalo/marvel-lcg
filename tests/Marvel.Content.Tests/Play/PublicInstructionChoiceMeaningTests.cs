using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PublicInstructionChoiceMeaningTests : ChoosingCardsTestBase
{
    [Fact]
    public void ExhaustionNamesTheResolvingPlayersIdentityInsteadOfTheFirstSeat()
    {
        World world = Deal("spider_man", "black_panther");
        var runner = AuthoredCards.Runner();
        Card source = world.CreateCard("01155", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)));
        runner.WhenRevealed(world, source, 1);
        runner.Chose(world, source, 1, 1, Decision.Take(1));

        Prompt prompt = runner.Choosing(world, source, 1, 2)!;

        Assert.Equal("Exhaust T'Challa; if completed, remove Affairs of State from the game",
            prompt.Affordances[0].DisplayLabel);
        Assert.DoesNotContain("Peter Parker", prompt.Affordances[0].Description);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void UnknownLaterInstructionDoesNotBecomeAPartialPromiseOrEvaluateItsHiddenAmount(int concealed)
    {
        // Synthetic public-prefix/hidden-expression sequence isolates summary
        // completeness. The helper must not describe just the exhaust prefix.
        World world = Deal();
        for (int index = 0; index < concealed; index++)
            world.CreateCard("01046", world.Seats[0].Deck);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [{ "card": "01110", "abilities": [{
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed", "subject": "this" },
              "effect": { "choose": { "options": [
                { "seq": [ { "exhaust": "you" },
                  { "dealDamage": { "cards": "you", "amount":
                    { "count": { "cardsIn": { "area": "yourDeck", "kind": "Upgrade" } } } } }
                ] }, { "seq": [] }
              ] } }
            }]}]}
            """));
        Card source = world.CreateCard("01110", world.AreaOf(DeckType.RevealingArea));
        runner.WhenRevealed(world, source, 0);
        string before = world.Digest().Fingerprint();

        Prompt prompt = runner.Choosing(world, source, 0, 1)!;

        Affordance unknown = prompt.Affordances.Single(offer => offer.Id == 0);
        Assert.Null(unknown.DisplayLabel);
        Assert.Null(unknown.CommitLabel);
        Assert.True(string.IsNullOrEmpty(unknown.Description));
        Assert.Equal("Continue without this effect", prompt.Affordances.Single(offer => offer.Id == 1).DisplayLabel);
        Assert.Equal(before, world.Digest().Fingerprint());
    }
}
