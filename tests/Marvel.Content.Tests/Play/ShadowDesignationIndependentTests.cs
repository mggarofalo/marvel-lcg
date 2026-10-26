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

public sealed class ShadowDesignationIndependentTests
{
    [Theory]
    [InlineData("taskmaster", true)]
    [InlineData("taskmaster", false)]
    [InlineData("rhino", true)]
    [InlineData("rhino", false)]
    [Rule("rr:nemesis-encounter-set.3.1")]
    [Rule("rr:unique-icon.4.2")]
    [Rule("ruling:a956304c470dac6c")]
    public void SecondPlayersShadowSelectsThePrintedRoleAcrossEntryConflictAndAbsence(
        string scenario, bool present)
    {
        // The nemesis role "is designated by parenthetical text". Uniqueness discards
        // the blocked minion and deals one card "without giving it surge" (current ruling).
        // Shadow's printed absent-minion conditional still grants surge.
        var runner = AuthoredCards.Runner();
        var world = CreateWorld(scenario, runner);
        var aside = world.Seats[1].Nemesis;
        var nemesis = Assert.Single(aside.Cards, card => card.FaceId == "08026");
        var side = Assert.Single(aside.Cards, card => card.FaceId == "08027");
        var secondaries = aside.Cards.Where(card => card.FaceId == "08028").ToArray();
        Assert.Equal(2, secondaries.Length);
        Assert.All(secondaries, card => Assert.False(world.Facts.IsNemesisMinion(card.FaceId)));
        World.MoveToTop(nemesis, aside);
        if (!present)
        {
            World.MoveToTop(nemesis, world.AreaOf(DeckType.EncounterDiscardPile));
            World.MoveToTop(secondaries[0], world.AreaOf(DeckType.EncounterDeck));
            Assert.Single(aside.Cards, card => world.Facts.Kind(card.FaceId) == CardKind.Minion);
        }
        var shuffled = aside.Cards.Where(card => card != nemesis && card != side).ToArray();
        var firstPlayersNemesis = world.Seats[0].Nemesis.Cards.ToArray();
        var shadow = Assert.Single(world.Cards, card => card.FaceId == "01190");
        var queue = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1));
        Assert.Empty(queue.Cards);
        World.MoveToTop(shadow, queue);
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: shadow.ObjectId, Seat: 1));

        var events = new RedSkullFixture(world, runner).Finish();

        bool conflict = scenario == "taskmaster";
        bool enters = present && !conflict;
        Assert.Equal(enters ? DeckType.EngagedEnemiesArea : DeckType.EncounterDiscardPile,
            nemesis.Area.Type);
        if (enters) Assert.Equal(PlayArea.Of(1), nemesis.Area.PlayArea);
        Assert.Equal(DeckType.SideSchemesArea, side.Area.Type);
        Assert.All(shuffled, card => Assert.Equal(DeckType.EncounterDeck, card.Area.Type));
        Assert.All(firstPlayersNemesis, card => Assert.Equal(DeckType.AsideDeck, card.Area.Type));
        Assert.Equal(DeckType.EncounterDiscardPile, shadow.Area.Type);
        var deals = events.OfType<CardsMoved>().Where(move =>
            move.From.Zone == nameof(DeckType.EncounterDeck)
            && move.To.Zone == nameof(DeckType.DealtEncounterCardsDeck)).ToArray();
        Assert.Equal(enters ? 0 : 1, deals.Length);
        Assert.Equal(deals.Length, queue.Cards.Count);
        if (deals.Length > 0) Assert.Equal(present && conflict, deals[0].Trigger == "unique");
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static World CreateWorld(string scenario, Marvel.Cards.Run.AbilityRunner runner)
    {
        var world = WorldSetup.Deal(CardCatalogData,
            Blueprints.From(Dealer.DealOrder(SetupCatalogData, scenario,
                ["spider_man", "black_widow"]), CardCatalogData),
            [SetupCatalogData.Hero("spider_man").Name, SetupCatalogData.Hero("black_widow").Name],
            832, runner);
        var game = Game.Begin(world, CardCatalogData, runner);
        while (game.Pending is { } prompt && prompt.Affordances.Any(a => a.Verb == Game.ResolveMulligans))
            game.Resolve(Decision.Take(Assert.Single(prompt.Affordances, a => a.Verb == Game.ResolveMulligans).Id));
        return world;
    }
}
