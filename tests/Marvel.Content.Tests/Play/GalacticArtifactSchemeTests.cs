using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.GalaxyModularBoards;

namespace Marvel.Content.Tests.Play;

// The production Galactic Artifacts side schemes and The Poison on synthetic boards.
public sealed class GalacticArtifactSchemeTests : CoreCardTestBase
{
    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 7)]
    [Rule("rr:hinder-x.2")]
    public void AnArtifactSchemeEntersWithHinderTwoPerPlayerBesideItsStartingThreat(int players, long threat)
    {
        // 16127: starting threat 3 and "Hinder 2[per_hero]".
        var world = Board(players);
        var egg = ShipCommandBoards.Reveal(world, MonarchEgg);

        Drive(world);

        Assert.Equal(threat, egg.Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:victory-x.2")]
    public void TheDefeaterOfTheMonarchEggMayReadyTheirIdentity(bool ready)
    {
        // 16127: "When Defeated: The player who defeated this scheme may ready their identity."
        // Player 1 thwarts the last threat; the scheme goes to the victory display.
        var world = Board(players: 2);
        var egg = GrootBoards.Side(world, MonarchEgg, 1);
        var thwarter = world.Seats[1].IdentityCard;
        Prompt? asked = null;

        BasicPowers.BasicThwart(world, Cards, 1, egg, []);
        Drive(world, prompt => Choose(prompt, ready ? "Ready your identity" : "Do not ready your identity", ref asked));

        Assert.Equal(1, asked!.Player);
        Assert.Equal(ready, thwarter.Ready);
        Assert.Equal(DeckType.VictoryDisplay, egg.Area.Type);
    }

    [Fact]
    [Rule("rr:victory-x.2")]
    public void TheDefeaterOfTheMagicalTeapotMayHealFourDamage()
    {
        // 16128: "The player who defeated this scheme may heal 4 damage from their identity."
        var world = Board();
        var hero = world.Seats[0].IdentityCard;
        hero.TakeDamage(6);
        var teapot = GrootBoards.Side(world, MagicalTeapot, 1);
        Prompt? asked = null;

        BasicPowers.BasicThwart(world, Cards, 0, teapot, []);
        Drive(world, prompt => Choose(prompt, "Heal 4 damage from your identity", ref asked));

        Assert.NotNull(asked);
        Assert.Equal(2, hero.Damage);
        Assert.Equal(DeckType.VictoryDisplay, teapot.Area.Type);
    }

    [Fact]
    [Rule("rr:victory-x.2")]
    public void TheDefeaterOfThePhilosophersStoneMayDrawTwoCards()
    {
        // 16129: "The player who defeated this scheme may draw 2 cards."
        var world = Board();
        var stone = GrootBoards.Side(world, PhilosophersStone, 1);
        Prompt? asked = null;

        BasicPowers.BasicThwart(world, Cards, 0, stone, []);
        Drive(world, prompt => Choose(prompt, "Draw 2 cards", ref asked));

        Assert.NotNull(asked);
        Assert.Equal(2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.VictoryDisplay, stone.Area.Type);
    }

    [Fact]
    [Rule("rr:attachment.2")]
    public void ThePoisonDamagesItsHostForEachCounterAsTheirTurnBegins()
    {
        // 16125: "Forced Interrupt: When your turn begins, place 1 poison counter here, then take
        // 1 damage for each poison counter here." With one counter already, the second turn deals 2.
        Card? poison = null;
        var (_, world) = ActionAbilitySupportFixtures6.Playing(board =>
        {
            poison = board.CreateCard(ThePoison, board.AreaOf(DeckType.EncounterDiscardPile));
            var host = board.Seats[0].IdentityCard;
            World.MoveToTop(poison, board.AreaOf(DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId, host.Area.CardOwner));
            poison.PlaceTokens("c_poison", 1);
        }, hero: true);

        Assert.Equal(2, poison!.Tokens.GetValueOrDefault("c_poison"));
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:attachment.2.1")]
    public void AnyPlayerMaySpendThreeDifferentResourcesToDiscardThePoison()
    {
        // "Hero Action: Spend 3 resources of different types → discard this card. Any player can do this."
        var world = Board(players: 2);
        var poison = ShipCommandBoards.Attach(world, world.Seats[1].IdentityCard, ThePoison);
        var paying = new[] { GrootBoards.Hand(world, "01003"), GrootBoards.Hand(world, "01004"), GrootBoards.Hand(world, "01006") };

        var action = Assert.Single(ShipCommandBoards.ActionsOn(world, poison, 0));
        world.Agenda.AddPlayerAction(0, new PlayerAction(action, [.. paying.Select(card => card.ObjectId)], [],
            Allocations: [new(paying[0].ObjectId, 0, "R"), new(paying[1].ObjectId, 0, "B"), new(paying[2].ObjectId, 0, "Y")]));
        Drive(world);

        Assert.Equal(DeckType.EncounterDiscardPile, poison.Area.Type);
    }

    private static Decision? Choose(Prompt prompt, string description, ref Prompt? asked)
    {
        if (prompt.Affordances.FirstOrDefault(a => (a.Description ?? "").StartsWith(description, StringComparison.Ordinal)) is not { } option) return null;
        asked = prompt;
        return Decision.Take(option.Id);
    }
}
