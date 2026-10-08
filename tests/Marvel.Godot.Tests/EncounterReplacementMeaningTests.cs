using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class EncounterReplacementMeaningTests
{
    [Theory]
    [InlineData(false, "01101")]
    [InlineData(true, "01101")]
    [InlineData(true, "01109")]
    public void SelectingAndPayingForTheInterruptRetainsItsCompleteMeaningWithoutRevealingTheReplacement(
        bool restricted, string replacementFace)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));
        var world = new World(cards, players: 2) { Abilities = runner };
        Seat seat = world.CreateSeat("T'Challa");
        seat.IdentityCard = world.CreateCard("01040a", seat.Hero);
        Seat other = world.CreateSeat("Peter");
        other.IdentityCard = world.CreateCard("01001b", other.Hero);
        world.CreateCard("01087", seat.Deck);
        world.CreateCard("01135", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01138b", world.AreaOf(DeckType.MainSchemesArea));
        Card widow = world.CreateCard("01075", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        Card payment = world.CreateCard("01089", seat.Hand);
        Card current = world.CreateCard("01154", world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        world.CreateCard("01110", world.AreaOf(DeckType.EncounterDeck));
        Card replacement = world.CreateCard(replacementFace, world.AreaOf(DeckType.EncounterDeck));
        world.Agenda.Add(new PhaseStep(Steps.RevealEncounterCard, 1, 4, Subject: current.ObjectId, Seat: 0));
        Prompt prompt = Assert.IsType<Prompt>(Sequence.Work(world, cards, runner, []));
        IVisibilityPolicy visibility = restricted ? new RestrictedVisibilityPolicy(0) : new PermissiveVisibilityPolicy();
        VisibleResult visible = WorldProjection.For(world, prompt, [], visibility.Authorize(null, 2));
        PromptPresentation presentation = PromptPresentation.From(visible.Prompt!, visible.World);
        var composer = new DecisionComposer(visible.Prompt!);
        AffordancePresentation offer = Assert.Single(presentation.Affordances, item => item.AnchorId == widow.ObjectId);
        string before = world.Digest().Fingerprint();

        composer.SelectAffordance(offer.Id);
        Assert.False(composer.Progress().IsReady);
        AssertMeaning(Assert.IsType<string>(TableDraftSummary.From(composer, presentation, visible.World, compact: true)));
        composer.ToggleResource(payment.ObjectId);

        Assert.True(composer.Progress().IsReady);
        AssertMeaning(Assert.IsType<string>(TableDraftSummary.From(composer, presentation, visible.World, compact: true)));
        Assert.Contains(cards.Title(current.FaceId), presentation.Resolution);
        Assert.DoesNotContain(cards.Title(replacementFace), DecisionCopy.ActionSummary(offer));
        Assert.DoesNotContain(visible.World.Areas.SelectMany(area => area.Cards),
            card => card.Id == replacement.ObjectId && card.Face is not null);
        Assert.Null(WorldProjection.For(world, prompt, [], new RestrictedVisibilityPolicy(1).Authorize(null, 2)).Prompt);
        Assert.False(replacement.FaceUp);
        Assert.Equal(before, world.Digest().Fingerprint());
    }

    private static void AssertMeaning(string summary)
    {
        Assert.Contains("Exhaust Black Widow", summary);
        Assert.Contains("Cancel the revealed encounter card's effects and discard it", summary);
        Assert.Contains("then reveal the next encounter card", summary);
        Assert.Contains("replacement card's effects remain unresolved", summary);
    }
}
