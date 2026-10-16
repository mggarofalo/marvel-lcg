using Marvel.Rules.Setup;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SpiderManUpgradeTests : SpiderManCardTestBase
{
    [Rule("rr:uses-x-type")]
    [Rule("rr:resource-ability.1")]
    [Fact]
    public void WebShooterPaysItsCostsAndLeavesWhenTheLastCounterIsRemoved()
    {
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var shooter = world.CreateCard(
            AuthoredCards.WebShooter,
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0),
                world.Seats[0].IdentityCard.ObjectId, 0));
        Reveal.EnterPlay(world, Cards, shooter, []);
        shooter.PlaceTokens("c_web", -2);

        var offered = Assert.Single(runner.ResourceAbilities(world, 0), source => source.Effect == shooter.ObjectId);
        Assert.Equal("Web-Shooter", offered.AbilityName);
        Assert.Equal("Exhaust Web-Shooter; Remove 1 web counter from Web-Shooter", offered.CostDescription);

        var events = new List<GameEvent>();
        var execution = runner.ExecuteResource(world, 0, shooter.ObjectId);
        events.AddRange(execution.Execution.Events);
        string generated = execution.Generation.Resources;

        Assert.Equal("G", generated);
        Assert.Contains(events.OfType<FieldSet>(), change => change.Card == shooter.ObjectId
            && change.Field == "is_exhaust" && change.To == 1);
        Assert.True(shooter.Ready);
        Assert.Equal(0, shooter.Tokens.GetValueOrDefault("c_web"));
        Assert.Equal(DeckType.DiscardPile, shooter.Area.Type);
    }

    [Rule("rr:resource-ability")]
    [Fact]
    public void WebShooterIsAvailableOnlyInHeroFormAndWhileItsCostsCanBePaid()
    {
        var world = Deal();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var shooter = world.CreateCard(
            AuthoredCards.WebShooter,
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0),
                world.Seats[0].IdentityCard.ObjectId, 0));
        Reveal.EnterPlay(world, Cards, shooter, []);

        Assert.DoesNotContain(
            runner.ResourceAbilities(world, 0), source => source.Effect == shooter.ObjectId);

        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        Assert.Contains(
            runner.ResourceAbilities(world, 0), source => source.Effect == shooter.ObjectId);

        runner.ExecuteResource(world, 0, shooter.ObjectId);
        Assert.DoesNotContain(
            runner.ResourceAbilities(world, 0), source => source.Effect == shooter.ObjectId);
    }

    [Rule("rr:stun-stunned.1")]
    [Rule("rr:stun-stunned.2")]
    [Rule("rr:upgrade.3")]
    [Fact]
    public void WebbedUpDiscardsItselfAndStunsItsEnemy()
    {
        // An attached upgrade "modif[ies] the card [it is] attached to", not
        // the identity who played it. "If an ability 'stuns' a character, give
        // that character a stunned status card." Webbed Up's interrupt does
        // both printed effects to its enemy host.
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var webbed = world.CreateCard(
            AuthoredCards.WebbedUp,
            world.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId, 0));
        var occurrence = Occurrence.ForAttack(
            1, [Steps.AttackInitiated], world, Cards,
            villain.ObjectId, world.Seats[0].IdentityCard.ObjectId, player: 0);
        var ability = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            pending => pending.Card == webbed.ObjectId);

        runner.ResolveWindow(world, occurrence, ability, new Decision(ability.Card, [], []));

        Assert.Equal(DeckType.DiscardPile, webbed.Area.Type);
        Assert.True(Statuses.Has(world, villain, Statuses.Stunned));
    }

    [Rule("rr:choose-game-element.1")]
    [Fact]
    public void SpiderTracerLetsItsControllerChooseTheScheme()
    {
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        var minion = world.CreateCard(
            AuthoredCards.Shocker,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var tracer = world.CreateCard(
            AuthoredCards.SpiderTracer,
            world.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId, 0));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 4);
        var occurrence = new Occurrence(
            1, [Steps.CardDefeated], Subject: minion.ObjectId, Player: 0);
        occurrence.Also(new Defeated(minion.ObjectId, 0, "Attack") { Kind = CardKind.Minion });
        var ability = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt));

        runner.ResolveWindow(world, occurrence, ability, new Decision(ability.Card, [], []));
        var waiting = Assert.Single(world.Agenda.Outstanding);
        runner.Chose(world, waiting, Decision.Take(scheme.ObjectId));

        Assert.Equal(1, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:attach-to")]
    [Fact]
    public void TheTwoAttachmentsExposeTheirPrintedLegalHosts()
    {
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        var minion = world.CreateCard(
            AuthoredCards.Shocker,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var tracer = world.CreateCard(AuthoredCards.SpiderTracer, world.Seats[0].Hand);
        var webbed = world.CreateCard(AuthoredCards.WebbedUp, world.Seats[0].Hand);
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        Assert.Equal([minion.ObjectId], runner.AttachmentTargets(world, tracer));
        Assert.Equal(
            [villain.ObjectId, minion.ObjectId],
            runner.AttachmentTargets(world, webbed));
    }

}
