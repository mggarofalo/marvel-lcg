using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class BasicPowerANewStageTests : BasicPowerTestBase
{
    [Rule("rr:villain-defeat.3")]
    [Rule("rr:villain-defeat.3.2")]
    [Fact]
    public void ANewStageWithTheSameTitleKeepsWhatWasOnTheOldOne()
    {
        // "Attachments, upgrades, status cards, counters, and non-damage tokens
        // on a villain **carry over** to the new stage." Rhino's three stages
        // share a title and Charge attaches to Rhino, so this is the ordinary
        // case in the one scenario the engine plays.
        var printed = new Printed().With("hero", ("ATK", "9")).With("villain", ("HP", "5")).With("villain2", ("HP", "12"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var next = world.CreateCard("villain2", world.AreaOf(DeckType.VillainDeck));
        villain.PlaceTokens("k_threat", 3);
        var attached = world.CreateCard("upgrade", world.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));
        // Stunned rather than tough on purpose: a tough card would prevent all
        // the damage (`rr:tough.2`) and the stage would never be defeated at
        // all, which is correct and not what this is about.
        Statuses.Give(world, villain, Statuses.Stunned);
        BasicPowerInitiation.BasicAttack(world, printed, 0, villain, []);
        Agendas.Finish(world, printed);
        Assert.Equal(next.ObjectId, attached.Area.Host);
        Assert.Equal(3, next.Tokens["k_threat"]);
        Assert.True(Statuses.Has(world, next, Statuses.Stunned));
    }

    [Rule("rr:villain-defeat.4")]
    [Rule("rr:villain-defeat.4.2")]
    [Fact]
    public void ANewStageWithADifferentTitleKeepsNothing()
    {
        // "Attachments, upgrades, status cards, counters, and non-damage tokens
        // do **not** carry over." The title is the whole of the difference.
        var printed = new Printed().With("hero", ("ATK", "9")).With("villain", ("HP", "5")).With("stranger", ("HP", "12"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var next = world.CreateCard("stranger", world.AreaOf(DeckType.VillainDeck));
        villain.PlaceTokens("k_threat", 3);
        var attached = world.CreateCard("upgrade", world.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));
        BasicPowerInitiation.BasicAttack(world, printed, 0, villain, []);
        Agendas.Finish(world, printed);
        Assert.Equal(DeckType.EncounterDiscardPile, attached.Area.Type);
        Assert.Equal(0, next.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:enters-play")]
    [Rule("rr:toughness")]
    [Rule("rr:toughness.1")]
    [Fact]
    public void ANewStageEntersPlayWithTheKeywordsItPrints()
    {
        // A villain stage comes out of the villain deck, and `rr:enters-play`
        // is "any time when a card transitions from an out-of-play area into
        // play" -- so `rr:toughness`'s "when a character with the toughness
        // keyword enters play, place a tough status card on it" applies to the
        // stage the deck advances to, not only to the one setup dealt.
        //
        // Rhino's third stage is the card that made this visible: it prints
        // toughness and nothing was reading it, because the scenario the engine
        // could play never advanced the villain deck to a stage with any.
        var printed = new Printed().With("hero", ("ATK", "9")).With("villain", ("HP", "5")).With("villain2", ("HP", "12"), ("Toughness", "1"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var next = world.CreateCard("villain2", world.AreaOf(DeckType.VillainDeck));
        BasicPowerInitiation.BasicAttack(world, printed, 0, villain, []);
        Agendas.Finish(world, printed);
        Assert.True(Statuses.Has(world, next, Statuses.Tough));
    }

    [Rule("rr:villain-defeat.3.2")]
    [Rule("rr:toughness.1")]
    [Fact]
    public void AToughCardCarriedOverIsNotDoubledByToughness()
    {
        // The two rules meet on the same card. `rr:villain-defeat.3.2` carries
        // a tough status across to a stage of the same title, and `rr:toughness`
        // would give it one for entering play -- but `rr:status-cards.1` caps a
        // character at one tough card, so the keyword finds its work already
        // done. Order is why this is a test: inheriting first is what makes the
        // cap the thing that decides, rather than the sequence.
        //
        // Confused rather than tough on the defeated stage would not do: a tough
        // card would prevent all the damage (`rr:tough.2`) and the stage would
        // never be defeated at all.
        var printed = new Printed().With("hero", ("ATK", "9")).With("villain", ("HP", "5")).With("villain2", ("HP", "12"), ("Toughness", "1"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var next = world.CreateCard("villain2", world.AreaOf(DeckType.VillainDeck));
        BasicPowerInitiation.BasicAttack(world, printed, 0, villain, []);
        Agendas.Finish(world, printed);
        Assert.Single(world.Areas.Where(area => area.Host == next.ObjectId).SelectMany(area => area.Cards), card => card.FaceId == Statuses.Tough);
    }

    [Rule("rr:villain-defeat")]
    [Rule("rr:winning-the-game")]
    [Fact]
    public void DefeatingTheFinalStageWinsTheGame()
    {
        // "If the final villain stage is defeated, the players win the game."
        // The other ending is the villain completing the main
        // scheme, and a boolean could not tell them apart.
        var printed = new Printed().With("hero", ("ATK", "9")).With("villain", ("HP", "5"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var attachment = world.CreateCard("upgrade", world.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));
        BasicPowerInitiation.BasicAttack(world, printed, 0, villain, []);
        Agendas.Finish(world, printed);
        Assert.Equal(Outcome.PlayersWin, world.Result);
        Assert.True(world.IsOver);
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
    }

    [Fact]
    public void AGameCannotEndTwice()
    {
        // Two endings racing would mean a rule resolved after the game stopped.
        var printed = new Printed();
        var world = Board(printed);
        world.Finish(Outcome.PlayersWin);
        var thrown = Assert.Throws<RulesNotImplementedException>(() => world.Finish(Outcome.VillainWins));
        Assert.Contains("already ended", thrown.Message, StringComparison.Ordinal);
    }
}
