using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ShadowUniqueRulingTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [Rule("rr:nemesis-encounter-set.3.1")]
    [Rule("rr:unique-icon.4.2")]
    [Rule("ruling:a956304c470dac6c")]
    public void ShadowUsesTheDesignatedNemesisAndHonorsUniqueConflict(bool nemesisSetAside, bool reorder)
    {
        // A set-aside nemesis blocked by uniqueness is discarded and deals one
        // encounter card, "without giving it surge" (ruling:a956304c470dac6c).
        // An absent nemesis instead follows Shadow's printed surge branch.
        // rr:nemesis-encounter-set.3.1: the nemesis minion "is designated by
        // parenthetical text printed on one or more of those minions".
        var runner = AuthoredCards.Runner();
        var world = WorldSetup.Deal(CardCatalogData,
            Blueprints.From(Dealer.DealOrder(SetupCatalogData, "taskmaster", ["black_widow"]), CardCatalogData),
            [SetupCatalogData.Hero("black_widow").Name], 825, runner);
        var game = Game.Begin(world, CardCatalogData, runner);
        while (game.Pending is { } prompt && prompt.Affordances.Any(a => a.Verb == Game.ResolveMulligans))
            game.Resolve(Decision.Take(Assert.Single(prompt.Affordances, a => a.Verb == Game.ResolveMulligans).Id));
        var nemesis = Assert.Single(world.Cards, card => card.FaceId == "08026");
        var side = Assert.Single(world.Cards, card => card.FaceId == "08027");
        Assert.Equal(DeckType.AsideDeck, nemesis.Area.Type);
        if (!nemesisSetAside && reorder)
            World.MoveToTop(nemesis.Area.Cards.First(card => card.FaceId == "08028"),
                world.AreaOf(DeckType.EncounterDeck));
        var rest = nemesis.Area.Cards.Where(card => card != nemesis && card != side).ToArray();
        if (reorder) World.MoveToTop(nemesis, nemesis.Area);
        if (!nemesisSetAside) World.MoveToTop(nemesis, world.AreaOf(DeckType.EncounterDiscardPile));
        var shadow = Assert.Single(world.Cards, card => card.FaceId == "01190");
        var queue = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0));
        Assert.Empty(queue.Cards);
        World.MoveToTop(shadow, queue);
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: shadow.ObjectId, Seat: 0));

        var events = new RedSkullFixture(world, runner).Finish();

        Assert.Equal(DeckType.EncounterDiscardPile, nemesis.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, side.Area.Type);
        Assert.All(rest, card => Assert.Equal(DeckType.EncounterDeck, card.Area.Type));
        Assert.Equal(DeckType.EncounterDiscardPile, shadow.Area.Type);
        Assert.Single(queue.Cards);
        var deal = Assert.Single(events.OfType<CardsMoved>(), move =>
            move.From.Zone == nameof(DeckType.EncounterDeck)
            && move.To.Zone == nameof(DeckType.DealtEncounterCardsDeck));
        Assert.Equal(nemesisSetAside, deal.Trigger == "unique");
        Assert.Empty(world.Agenda.Outstanding);
    }
}
