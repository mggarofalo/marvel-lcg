using Marvel.Rules.Setup;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Campaign;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual Condition cards in an isolated Core encounter fixture; not complete campaign acceptance.</summary>
public sealed class CampaignConditionSetupTests
{
    [Theory]
    [InlineData("04159a", 2)]
    [InlineData("04159b", 2)]
    [InlineData("04160a", 1)]
    [InlineData("04160b", 1)]
    [InlineData("04161a", 3)]
    [InlineData("04161b", 3)]
    [InlineData("04162a", 4)]
    [InlineData("04162b", 4)]
    [Rule("rr:appendix-ii-setup.step.13")]
    public void RecordedHitPointsGainExactlyThePrintedConditionBonusBeforeOpeningHands(string face, int bonus)
    {
        // ruling:1516351f87fc4382: record base HP; next setup adds the Condition bonus.
        // Printed Conditions 04159–04162: "You get +[1–4] hit points."
        var game = Start(face, bonus);
        var world = game.State;
        var identity = world.Seats[0].IdentityCard;
        var condition = Assert.Single(world.Cards, card => card.FaceId == face);
        Assert.Equal(DeckType.UpgradesArea, condition.Area.Type);
        Assert.Equal(10 + bonus, DamagePlacement.Health(world, world.Facts, identity));
        Assert.Equal(5, identity.Damage);
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.True(game.Pending!.Cancellable);
        game.Resolve(Decision.Decline);
        Assert.Equal(GamePhase.Mulligan, game.Phase);
        Assert.Equal(5, identity.Damage);
        Assert.DoesNotContain(world.Campaign!.Cards, card => card.Assigned);
    }

    [Theory]
    [InlineData("04159a", 2)]
    [InlineData("04160a", 1)]
    [InlineData("04161a", 3)]
    [InlineData("04162a", 4)]
    [Rule("rr:appendix-ii-setup.step.13")]
    public void ExpertRecoveryHealsToThePrintedConditionMaximumWithoutGrantingItAgain(string face, int bonus)
    {
        // pack:mc10:persistent-damage permits adding an obligation to heal to full.
        var game = Start(face, bonus);
        var world = game.State;
        Assert.Contains($"{10 + bonus} HP", Assert.Single(game.Pending!.Affordances).Description);
        game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances).Id));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(10 + bonus, DamagePlacement.Health(world, world.Facts, world.Seats[0].IdentityCard));
        Assert.Equal("04163", Assert.Single(world.Campaign!.Cards, card => card.Assigned).Binding.Card.Face);
        Assert.Equal(GamePhase.Mulligan, game.Phase);
    }

    [Fact]
    [Rule("rr:setup-keyword.1")]
    public void ASecondPlayersSetupConditionAttachesToItsOwnerInsteadOfTheFirstPlayer()
    {
        var game = Start("04162b", 4, owner: 1);
        var world = game.State;
        var condition = Assert.Single(world.Cards, card => card.FaceId == "04162b");
        Assert.Equal(1, condition.Owner);
        Assert.Equal(PlayArea.Of(1), condition.Area.PlayArea);
        Assert.Equal(world.Seats[1].IdentityCard.ObjectId, condition.Area.Host);
        Assert.Equal(10, DamagePlacement.Health(world, world.Facts, world.Seats[0].IdentityCard));
        Assert.Equal(16, DamagePlacement.Health(world, world.Facts, world.Seats[1].IdentityCard));
        Assert.Equal(1, game.Pending!.Player);
    }

    private static Game Start(string face, int bonus, int owner = 0)
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var blueprints = Blueprints.From(Dealer.DealOrder(setup, "rhino", owner == 0 ? ["spider_man"] : ["spider_man", "captain_marvel"]), facts).ToList();
        var condition = new CampaignCard("condition", face, CampaignCardRole.Condition);
        var obligation = new CampaignCard("obligation", "04163", CampaignCardRole.Obligation);
        var bindings = new CampaignCardBinding[]
        {
            new(blueprints.Count, owner, condition), new(blueprints.Count + 1, owner, obligation),
        };
        blueprints.Add(new(face, SetupSlot.PlayerDeck, owner));
        blueprints.Add(new(obligation.Face, SetupSlot.SetAside, owner));
        var seat = new CampaignSeatSetup(owner, [condition], 5, false, [obligation], false)
            { ConditionHitPointBonus = bonus };
        var campaign = new CampaignGameSetup("condition-component", true, CampaignStage.Taskmaster, 1,
            owner == 0 ? [seat] : [new CampaignSeatSetup(0, [], 10, false, [], false), seat], [], 0);
        var abilities = AuthoredCards.Runner();
        var world = WorldSetup.DealConfigured(facts, blueprints, owner == 0 ? ["Spider-Man"] : ["Spider-Man", "Captain Marvel"], 7, abilities,
            new WorldSetupOptions(Campaign: campaign, CampaignCards: bindings));
        return Game.Begin(world, facts, abilities);
    }
}
