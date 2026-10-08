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

public sealed class IndomitableResponseMeaningTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectedResponseKeepsDiscardAndReadyMeaningAlongsideTheCompletedAttack(bool restricted)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));
        var world = new World(cards, 2) { Abilities = runner };
        Seat seat = world.CreateSeat("Black Panther");
        Card hero = seat.IdentityCard = world.CreateCard("01040a", seat.Hero);
        world.CreateCard("01087", seat.Deck);
        Seat other = world.CreateSeat("Spider-Man");
        other.IdentityCard = world.CreateCard("01001a", other.Hero);
        other.IdentityCard.Exhaust();
        world.CreateCard("01002", other.Hand);
        Card villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        Card source = world.CreateCard("01082", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        hero.Exhaust();
        world.Attack = new EnemyAttack(villain.ObjectId, 0, hero.ObjectId,
            Defender: hero.ObjectId, BasicDefense: true, CalculatedDamage: 0);
        world.Agenda.Add(new PhaseStep(Steps.EndAttack, 1, 2,
            Subject: villain.ObjectId, Seat: 0, Character: hero.ObjectId));
        Prompt prompt = Assert.IsType<Prompt>(Sequence.Work(world, cards, runner, []));
        Assert.Equal(PublicDecisionKind.Response, prompt.PublicKind);
        Assert.True(prompt.Cancellable);
        IVisibilityPolicy policy = restricted ? new RestrictedVisibilityPolicy(0) : new PermissiveVisibilityPolicy();
        VisibleResult visible = WorldProjection.For(world, prompt, [], policy.Authorize(null, 2));
        PromptPresentation presentation = PromptPresentation.From(visible.Prompt!, visible.World);
        AffordancePresentation offer = Assert.Single(presentation.Affordances, item => item.AnchorId == source.ObjectId);
        var draft = new DecisionComposer(visible.Prompt!);
        string before = world.Digest().Fingerprint();

        AssertMeaning(DecisionCopy.CompactActionSummary(offer));
        draft.SelectAffordance(offer.Id);
        AssertMeaning(Assert.IsType<string>(TableDraftSummary.From(draft, presentation, visible.World, compact: true)));
        Assert.Contains("Rhino finished attacking Black Panther", presentation.Resolution);
        Assert.Contains("11/11 HP", presentation.Resolution);
        Assert.Contains("Indomitable", offer.SourceName);
        Assert.Null(offer.CostDescription);
        Assert.Equal(before, world.Digest().Fingerprint());
        Assert.False(hero.Ready);
        Assert.Equal(DeckType.UpgradesArea, source.Area.Type);
        Assert.Null(WorldProjection.For(world, prompt, [], new RestrictedVisibilityPolicy(1).Authorize(null, 2)).Prompt);
        if (restricted)
        {
            var hidden = Assert.Single(Assert.Single(visible.World.Areas, area => area.Id == other.Hand.Id).Cards);
            Assert.Null(hidden.Id);
            Assert.Null(hidden.Face);
        }

        Assert.True(draft.TryBuild(out EngineDecision? answer, out string? error), error);
        Assert.Empty(answer!.Targets);
        Assert.Empty(answer.Resources ?? []);
        Sequence.Answer(world, cards, runner, prompt, Decision.Take(answer.Affordance), []);
        Assert.True(hero.Ready);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.False(other.IdentityCard.Ready);
    }

    private static void AssertMeaning(string summary)
    {
        Assert.Equal("Discard this card, then ready your identity.", summary);
        Assert.DoesNotContain("Rhino", summary);
    }
}
