using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Canonical obligations disclose each typed alternative before the player commits.</summary>
public sealed class CoreObligationChoiceMeaningTests : ChoosingCardsTestBase
{
    [Theory]
    [InlineData("black_panther", "01155")]
    [InlineData("spider_man", "01165")]
    [InlineData("she_hulk", "01160")]
    [InlineData("iron_man", "01170")]
    [InlineData("captain_marvel", "01175")]
    public void OptionalFormChangeNamesBothChoicesWithoutCancellingTheObligation(string hero, string obligation)
    {
        // The five canonical obligations first offer a form change, followed
        // by a separate required consequence. These words are presentation choices.
        World world = Deal(hero);
        Card identity = world.Seats[0].IdentityCard;
        string heroFace = identity.Faces.Single(face => Cards.Kind(face) == CardKind.Hero);
        identity.TurnTo(heroFace);
        world.CreateCard("01046", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var (source, _) = Reveal(world, obligation);
        var runner = AuthoredCards.Runner();
        string before = world.Digest().Fingerprint();
        Prompt prompt = runner.Choosing(world, source, 0, 1)!;

        Assert.False(prompt.Cancellable);
        Assert.Equal([0, 1], prompt.Affordances.Select(offer => offer.Id));
        Assert.Equal(["changeForm", "seq"], prompt.Affordances.Select(offer => offer.Label));
        Assert.Equal(["Change to alter-ego form", "Remain in your current form"],
            prompt.Affordances.Select(offer => offer.DisplayLabel));
        Assert.All(prompt.Affordances, offer =>
        {
            Assert.Equal(offer.DisplayLabel, offer.CommitLabel);
            Assert.Equal(offer.DisplayLabel, offer.Description);
        });
        Assert.Equal(before, world.Digest().Fingerprint());

        runner.Chose(world, source, 0, 1, Decision.Take(1));
        Assert.Equal(heroFace, identity.FaceId);
        Assert.Contains(world.Agenda.Outstanding, step => step.What == Steps.ChooseOption && step.Index == 2);
    }

    [Theory]
    [InlineData("black_panther", "01155", "Choose a BLACK PANTHER upgrade you control; then discard the chosen card; then discard Affairs of State")]
    [InlineData("spider_man", "01165", "Discard 1 random card from your hand; then give Eviction Notice surge (deal yourself 1 facedown encounter card to reveal after this card and its responses resolve); then discard Eviction Notice")]
    [InlineData("she_hulk", "01160", "Place 1 acceleration token on the main scheme; then discard Legal Work")]
    [InlineData("iron_man", "01170", "Exhaust all upgrades you control; then discard Business Problems")]
    [InlineData("captain_marvel", "01175", "Give Carol Danvers a stunned status card; then give Family Emergency surge (deal yourself 1 facedown encounter card to reveal after this card and its responses resolve); then discard Family Emergency")]
    public void RequiredAlternativesRetainEveryOrderedCostAndConsequence(string hero, string obligation, string alternative)
    {
        World world = Deal(hero);
        world.CreateCard("01046", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Card source = world.CreateCard(obligation, world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)));
        var runner = AuthoredCards.Runner();
        runner.WhenRevealed(world, source, 0);
        runner.Chose(world, source, 0, 1, Decision.Take(1));
        string before = world.Digest().Fingerprint();

        Prompt prompt = runner.Choosing(world, source, 0, 2)!;

        string title = Cards.Title(source.FaceId);
        string identity = Cards.Title(world.Seats[0].IdentityCard.FaceId);
        Assert.Equal([0, 1], prompt.Affordances.Select(offer => offer.Id));
        Assert.Equal($"Exhaust {identity}; if completed, remove {title} from the game", prompt.Affordances[0].DisplayLabel);
        Assert.Equal(alternative, prompt.Affordances[1].DisplayLabel);
        Assert.All(prompt.Affordances, offer =>
        {
            Assert.Equal(offer.DisplayLabel, offer.CommitLabel);
            Assert.Contains(offer.DisplayLabel!, offer.Description);
        });
        Assert.Equal(before, world.Digest().Fingerprint());
    }

    [Rule("rr:discard.1")]
    [Rule("rr:discard.2")]
    [Fact]
    public void AffairsUpgradeChoiceNamesBothDiscardsAndKeepsChosenAndSourceBindingsDistinct()
    {
        World world = Deal("black_panther");
        Card upgrade = world.CreateCard("01046", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Card unrelated = world.CreateCard("01077", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var runner = AuthoredCards.Runner();
        Card source = world.CreateCard("01155", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)));
        runner.WhenRevealed(world, source, 0);
        runner.Chose(world, source, 0, 1, Decision.Take(1));
        runner.Chose(world, source, 0, 2, Decision.Take(1));
        PhaseStep next = world.Agenda.Outstanding.Last(step => step.What == Steps.ChooseOption);

        Prompt prompt = runner.Choosing(world, source, 0, next.Index, next.Tier)!;
        Affordance choice = Assert.Single(prompt.Affordances);
        string expected = $"Discard {Cards.Title(upgrade.FaceId)}; then discard Affairs of State";
        Assert.Equal(upgrade.ObjectId, choice.AnchorId);
        Assert.Equal(expected, choice.Description);
        Assert.Equal(expected, choice.CommitLabel);
        Assert.DoesNotContain(Cards.Title(unrelated.FaceId), choice.Description);
        runner.Chose(world, source, 0, next.Index, Decision.Take(choice.Id), next.Tier);
        // "If a player card is discarded, it is placed faceup on top of the owning player's discard pile."
        // "If an encounter card is discarded, it is placed faceup on top of the encounter discard pile."
        Assert.Equal(DeckType.DiscardPile, upgrade.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, unrelated.Area.Type);
    }
}
