using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class LabeledAbilityTests
{
    [Rule("rr:labeled-ability")]
    [Rule("rr:labeled-ability.1")]
    [Rule("rr:labeled-ability.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EventLabelsArePerformedByTheIdentityAfterCosts()
    {
        // "The identity of the player using the labeled ability is considered
        // to be performing the labeled effect when the labeled ability begins
        // resolving (after costs have been paid)." Attack and thwart labels
        // are both made by that identity.
        var (world, facts) = Board();
        var identity = world.Seats[0].IdentityCard;
        var source = world.CreateCard(
            "event", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));

        Assert.Same(identity, LabeledAbilities.Begin(
            world, facts, 0, source, [BasicPowers.AttackVerb], []));
        Assert.Same(identity, LabeledAbilities.Begin(
            world, facts, 0, source, [BasicPowers.ThwartVerb], []));
    }

    [Rule("rr:labeled-ability.3")]
    [Rule("rr:labeled-ability.3.1")]
    [Fact]
    public void DefenseLabelNamesTheIdentityThatWillBecomeDefender()
    {
        // A defense-labeled ability "is considered to be a defense made by
        // that player's identity"; during an attack that identity becomes the
        // defender. `BeginDefenseAbility` applies the role after attribution.
        var (world, facts) = Board();
        var identity = world.Seats[0].IdentityCard;
        var source = world.CreateCard(
            "event", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        var enemy = world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.Attack = new EnemyAttack(enemy.ObjectId, 0, identity.ObjectId);

        var performer = LabeledAbilities.Begin(
            world, facts, 0, source, [Attack.DefenseVerb], []);
        Attack.BeginDefenseAbility(world, 0, performer!);

        Assert.Same(identity, performer);
        Assert.Equal(identity.ObjectId, world.Attack.Defender);
        Assert.Equal(identity.ObjectId, world.Attack.Target);
    }

    [Rule("rr:labeled-ability.5")]
    [Rule("rr:labeled-ability.6")]
    [Rule("rr:labeled-ability.6.1")]
    [Rule("rr:labeled-ability.6.2")]
    [Fact]
    public void AnyStatusMatchingAMultiLabelCancelsAllOfItAndEveryMatchingStatusLeaves()
    {
        // If any status cancels one label, "the entire ability (except for its
        // costs) is canceled" and "each status card ... that cancels any" of
        // its labels is removed. No performer means no attack or thwart began.
        var (world, facts) = Board();
        var identity = world.Seats[0].IdentityCard;
        var source = world.CreateCard(
            "event", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        Statuses.Give(world, identity, Statuses.Stunned);
        Statuses.Give(world, identity, Statuses.Confused);

        var performer = LabeledAbilities.Begin(
            world, facts, 0, source,
            [BasicPowers.AttackVerb, BasicPowers.ThwartVerb], []);

        Assert.Null(performer);
        Assert.False(Statuses.Has(world, identity, Statuses.Stunned));
        Assert.False(Statuses.Has(world, identity, Statuses.Confused));
    }

    [Rule("rr:support.3")]
    [Fact]
    public void SupportAbilityIsNotPerformedByTheIdentity()
    {
        // "Attacks, thwarts, defenses, actions, and triggered abilities that
        // resolve from a support in play are not considered to be performed by
        // the identity of the player who controls that support." An identity's
        // stun therefore cannot cancel the support's labeled attack.
        var (world, facts) = Board();
        var identity = world.Seats[0].IdentityCard;
        var support = world.CreateCard(
            "support", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        Statuses.Give(world, identity, Statuses.Stunned);

        var performer = LabeledAbilities.Begin(
            world, facts, 0, support, [BasicPowers.AttackVerb], []);

        Assert.Same(support, performer);
        Assert.True(Statuses.Has(world, identity, Statuses.Stunned));
    }

    [Rule("rr:upgrade.4")]
    [Fact]
    public void UpgradeUsesTheIdentityExceptWhenHostedByAnotherFriendlyCharacter()
    {
        // "Unless an upgrade is attached to a different friendly character,
        // an upgrade is considered to be an extension of the controlling
        // player's identity." The friendly-host exception makes the ally the
        // performer; an identity-hosted upgrade remains the identity's.
        var (world, facts) = Board();
        var identity = world.Seats[0].IdentityCard;
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var onAlly = world.CreateCard(
            "upgrade",
            world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(0), ally.ObjectId, cardOwner: 0));
        var onIdentity = world.CreateCard(
            "upgrade",
            world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(0), identity.ObjectId, cardOwner: 0));

        Assert.Same(ally, LabeledAbilities.Performer(world, facts, 0, onAlly));
        Assert.Same(identity, LabeledAbilities.Performer(world, facts, 0, onIdentity));
    }

    [Rule("rr:resource-card.2")]
    [Fact]
    public void ResourceCardAbilitiesAreAnExtensionOfTheIdentity()
    {
        // A resource card "is considered to be an extension of the identity
        // belonging to the player who controls that card." The same performer
        // answer is used for a resource ability and for a labeled event.
        var (world, facts) = Board();
        var resource = world.CreateCard("resource", world.Seats[0].Hand);

        Assert.Same(
            world.Seats[0].IdentityCard,
            LabeledAbilities.Performer(world, facts, 0, resource));
    }

    private static (World World, Facts Facts) Board()
    {
        var facts = new Facts();
        var world = new World(facts, 1);
        world.CreateSeat("p0");
        world.Seats[0].IdentityCard = world.CreateCard("hero", world.Seats[0].Hero);
        return (world, facts);
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "hero" => CardKind.Hero,
            "event" => CardKind.Event,
            "resource" => CardKind.Resource,
            "support" => CardKind.Support,
            "upgrade" => CardKind.Upgrade,
            "ally" => CardKind.Ally,
            "villain" => CardKind.EncounterVillain,
            "stunned" or "confused" => CardKind.Status,
            _ => CardKind.Unknown,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) => fallback;

        public string Title(string faceId) => faceId;
    }
}
