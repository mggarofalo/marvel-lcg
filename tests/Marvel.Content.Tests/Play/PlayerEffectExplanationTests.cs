using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PlayerEffectExplanationTests
{
    [Theory]
    [Rule("rr:lasting-effects.4")]
    [InlineData("01010b", "choose a player to draw 1 card", "Peter's seat draws 1 card")]
    [InlineData("01091", "choose a player to draw 1 card", "Peter's seat draws 1 card")]
    [InlineData("01092", "next card this phase costs 1 fewer resources", "Peter's seat: next card")]
    [InlineData("01070", "+1 THW and +1 ATK until the end of the player phase", "Peter's seat's characters +1 THW and +1 ATK")]
    public void CorePlayerEffectsExplainTheRecipientAndCommitmentAcrossTheirChoice(
        string face, string effect, string commitment)
    {
        // Real Core programs in a focused two-seat contract fixture. Description must not reveal a deck.
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));
        var world = new World(facts, players: 2) { Abilities = runner };
        var carol = world.CreateSeat("Carol's seat");
        carol.IdentityCard = world.CreateCard(face == "01010b" ? face : "01010a", carol.Hero);
        var peter = world.CreateSeat("Peter's seat");
        peter.IdentityCard = world.CreateCard("01001a", peter.Hero);
        world.CreateCard("01003", carol.Deck);
        world.CreateCard("01004", peter.Deck);
        var source = face == "01010b" ? carol.IdentityCard
            : world.CreateCard(face, face == "01070" ? carol.Hand
                : world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        int[] paying = face == "01070" ? [world.CreateCard("01088", carol.Hand).ObjectId] : [];
        var action = Assert.Single(runner.Actions(world, 0), ability => ability.Card == source.ObjectId);
        string before = world.Digest().Fingerprint();
        var offer = runner.Describe(world, action);
        Assert.Contains(effect, offer.Description!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(before, world.Digest().Fingerprint());
        if (face is "01091" or "01092") Assert.Contains("Exhaust", offer.CostDescription);
        runner.Act(world, action, paying, []);
        var step = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, source, 0, step.Index, step.Tier)!;
        Assert.Contains(effect, prompt.Description!, StringComparison.OrdinalIgnoreCase);
        var choice = Assert.Single(prompt.Affordances, option => option.Id == peter.IdentityCard.ObjectId);
        Assert.Contains(commitment, choice.CommitLabel);
        Assert.Contains(commitment, choice.Description);
        Assert.DoesNotContain("Black Cat", choice.Description);
        if (face == "01070")
        {
            Assert.Contains("until the end of the player phase", choice.Description);
            // "If a card enters play ... after the creation of a lasting effect, it is still affected by that lasting effect."
            Assert.Contains("characters they control later", choice.Description);
        }
        runner.Chose(world, source, 0, step.Index, Decision.Take(choice.Id), step.Tier);
        if (face is "01010b" or "01091")
        {
            Assert.Single(peter.Hand.Cards);
            Assert.Empty(carol.Hand.Cards);
        }
    }

    [Theory]
    [InlineData("EndOfRound", "attack")]
    [InlineData("EndOfPlayerPhase", "health")]
    public void UnsupportedModifierShapesDoNotBorrowAPlayerPhasePowerPromise(string until, string field)
    {
        var choice = new AbilityEffect.ChooseCard(
            new AbilityCardSelection.Query(AbilityCardQuery.Identities),
            new AbilityEffect.GrantControlledCharacters(AbilityPlayer.ChosenPlayer,
                [field], new AbilityNumber.Constant(1), until));
        Assert.Null(AbilityEffectDescription.Summary(choice));
    }
}
