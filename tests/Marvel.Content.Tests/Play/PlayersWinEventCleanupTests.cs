using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic event tests cleanup after a terminal effect, not printed card text.
public sealed class PlayersWinEventCleanupTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:winning-the-game.1")]
    public void EventRemainsResolvingWhenItsEffectEndsTheGame(bool win)
    {
        // "If these conditions are met, the players win or lose accordingly."
        string effect = win ? """{"playersWin":"game"}""" : """{"draw":{"player":"you","count":1}}""";
        var runner = Runner("01004", "Action", effect);
        Card? card = null;
        Card? resource = null;
        var (_, world) = Playing(board =>
        {
            card = board.CreateCard("01004", board.Seats[0].Hand);
            resource = board.CreateCard("01088", board.Seats[0].Hand);
        }, abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [resource!.ObjectId], [])).Events;
        Assert.Equal(win ? Outcome.PlayersWin : Outcome.Unfinished, world.Result);
        Assert.Equal(win ? DeckType.RevealingArea : DeckType.DiscardPile, card!.Area.Type);
        Assert.Equal(win ? 0 : 1, events.OfType<CardsMoved>().Count(move =>
            move.Cards.Any(landing => landing.Card == card.ObjectId)
            && move.To.Zone == nameof(DeckType.DiscardPile)));
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:winning-the-game.1")]
    public void TerminalPowerRetainsItsModifierWhileControlCompletes(bool win)
    {
        // "If these conditions are met, the players win or lose accordingly."
        string body = win ? """{"playersWin":"game"}"""
            : """{"dealAttackDamage":{"cards":{"query":"villain"},"amount":1}}""";
        var runner = Runner("01004", "Action", """
            {"seq":[{"attack":{"target":{"query":"villain"},"effect":BODY}},
              {"placeCounters":{"card":"you","counter":"observed","count":1}}]}
            """.Replace("BODY", body, StringComparison.Ordinal));
        Card? card = null;
        Card? resource = null;
        ContinuousEffect? modifier = null;
        var (_, world) = Playing(board =>
        {
            card = board.CreateCard("01004", board.Seats[0].Hand);
            resource = board.CreateCard("01088", board.Seats[0].Hand);
            modifier = new ContinuousEffect(EffectSource.LastingEffect, "attackDamage", 1,
                Affects: card.ObjectId, Lasts: new Duration(Uses: 1));
            board.Effects.Register(modifier);
        }, hero: true, abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [resource!.ObjectId], [])).Events.ToList();
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(win ? Outcome.PlayersWin : Outcome.Unfinished, world.Result);
        Assert.Equal(win, world.Effects.Active().Contains(modifier!));
        Assert.Equal(win ? 0 : 1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_observed"));
        Assert.Equal(win ? DeckType.RevealingArea : DeckType.DiscardPile, card!.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:winning-the-game.1")]
    public void TerminalEntryAbilityStopsTheNextLabelledAbility(bool win)
    {
        // "If these conditions are met, the players win or lose accordingly."
        string first = win ? """{"playersWin":"game"}""" : """{"draw":{"player":"you","count":1}}""";
        var runner = new Marvel.Cards.Run.AbilityRunner(Marvel.Cards.Dsl.AbilityCatalog.Parse("""
            {"cards":[{"card":"01083","abilities":[
              {"trigger":{"event":"WhenCardEntersPlay","timing":"WhenRevealed","subject":"this"},
                "effect":FIRST},
              {"trigger":{"event":"WhenCardEntersPlay","timing":"WhenRevealed","subject":"this"},
                "labels":["Attack"],"effect":{"draw":{"player":"you","count":1}}}
            ]}]}
            """.Replace("FIRST", first, StringComparison.Ordinal)));
        var (_, world) = Playing(_ => { }, abilities: runner);
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Statuses.Give(world, ally, Statuses.Stunned);
        Assert.True(Statuses.Afflicted(world, world.Facts, ally, Statuses.Stunned));
        var entry = world.Agenda.Executions.BeginBatch(new Occurrence(0, [Steps.CardEntersPlay], Subject: ally.ObjectId, Player: 0));
        var events = runner.EntersPlay(world, entry, ally).Events;
        Assert.Equal(win ? Outcome.PlayersWin : Outcome.Unfinished, world.Result);
        Assert.Equal(win, Statuses.Afflicted(world, world.Facts, ally, Statuses.Stunned));
        Assert.Equal(win ? 0 : 1, events.OfType<CardsMoved>().Count(move =>
            move.To.Zone == world.Seats[0].Hand.Type.ToString()));
    }
}
