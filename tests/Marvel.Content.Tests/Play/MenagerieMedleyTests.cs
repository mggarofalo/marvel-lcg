using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.GalaxyModularBoards;

namespace Marvel.Content.Tests.Play;

// The production Menagerie Medley modular set on synthetic boards.
public sealed class MenagerieMedleyTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:status-cards.1")]
    public void PsionicGhostConfusesTheRevealer()
    {
        // 16135: "When Revealed: You are confused. If you are already confused, take 1 damage."
        var world = Board(face: "01001b");
        var identity = world.Seats[0].IdentityCard;
        var ghost = ShipCommandBoards.Reveal(world, PsionicGhost);

        Drive(world);

        Assert.Equal(DeckType.EngagedEnemiesArea, ghost.Area.Type);
        Assert.True(Statuses.Has(world, identity, Statuses.Confused));
        Assert.Equal(0, identity.Damage);
    }

    [Fact]
    [Rule("rr:status-cards.1")]
    public void PsionicGhostDamagesARevealerWhoIsAlreadyConfused()
    {
        var world = Board(face: "01001b");
        var identity = world.Seats[0].IdentityCard;
        Statuses.Give(world, identity, Statuses.Confused);
        ShipCommandBoards.Reveal(world, PsionicGhost);

        Drive(world);

        Assert.True(Statuses.Has(world, identity, Statuses.Confused));
        Assert.Equal(1, identity.Damage);
    }

    [Fact]
    [Rule("rr:boost-boost-icon")]
    public void PsionicGhostAsABoostCardEngagesThePlayerAttacked()
    {
        // "Boost: Put Psionic Ghost into play engaged with you." Its boost area prints no icon.
        var world = Board(players: 2);
        var ghost = OnTop(world, PsionicGhost);

        VillainAttacks(world, player: 1);

        Assert.Equal(DeckType.EngagedEnemiesArea, ghost.Area.Type);
        Assert.Equal(PlayArea.Of(1), ghost.Area.PlayArea);
        Assert.Equal(2, world.Seats[1].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:guard")]
    public void ServantBotPrintsGuardAndPatrol()
    {
        var world = Board();
        var bot = GrootBoards.Engaged(world, ServantBot);

        Assert.True(Marvel.Rules.Timing.Keywords.Has(world, bot, "guard", world.Facts));
        Assert.True(Marvel.Rules.Timing.Keywords.Has(world, bot, "patrol", world.Facts));
        Assert.Contains(ServantBot, Marvel.Content.Tests.Cards.AuthoredCards.Book.Authored);
    }

    [Fact]
    [Rule("rr:indirect-damage.5")]
    public void StarsharksQuickstrikeAttackDealsIndirectDamageAfterTheDefense()
    {
        // 16137: "Starshark's attacks deal indirect damage." With Black Cat in play the
        // player divides Starshark's 3 damage between the identity and the ally.
        var world = Board();
        var hero = world.Seats[0].IdentityCard;
        var cat = Ally(world);
        ShipCommandBoards.Reveal(world, Starshark);
        Prompt? assignment = null;

        Drive(world, prompt =>
        {
            if (prompt.Affordances.FirstOrDefault(a => a.Targets is { AllowRepeated: true }) is not { } assign) return null;
            assignment = prompt;
            return Decision.Take(assign.Id) with { Targets = [cat.ObjectId, cat.ObjectId, hero.ObjectId] };
        });

        Assert.NotNull(assignment);
        Assert.Equal([hero.ObjectId, cat.ObjectId], assignment.Affordances.Single(a => a.Targets is { AllowRepeated: true }).Targets!.Legal);
        Assert.Equal(DeckType.DiscardPile, cat.Area.Type);
        Assert.Equal(1, hero.Damage);
    }

    [Fact]
    [Rule("rr:indirect-damage.5")]
    public void AnotherMinionsAttackIsNotIndirect()
    {
        // The control: Kree Private's quickstrike attack deals its 1 damage to the identity.
        var world = Board();
        var hero = world.Seats[0].IdentityCard;
        var cat = Ally(world);
        ShipCommandBoards.Reveal(world, KreePrivate);
        var asked = new List<Prompt>();

        Drive(world, prompt => { asked.Add(prompt); return null; });

        Assert.DoesNotContain(asked, prompt => prompt.Affordances.Any(a => a.Targets is { AllowRepeated: true }));
        Assert.Equal(1, hero.Damage);
        Assert.Equal(0, cat.Damage);
    }

    [Fact]
    [Rule("rr:boost-boost-icon.3")]
    public void StarsharkAsABoostCardDamagesEachCharacterThePlayerControls()
    {
        // "Boost: Deal 1 damage to each character you control." Its boost area prints no icon,
        // so Rhino's attack deals 2; the ally takes the boost's 1.
        var world = Board();
        var hero = world.Seats[0].IdentityCard;
        var cat = Ally(world);
        OnTop(world, Starshark);

        VillainAttacks(world);

        Assert.Equal(3, hero.Damage);
        Assert.Equal(1, cat.Damage);
    }
}
