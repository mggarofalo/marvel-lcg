using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nonpublic resolver fixture: this does not admit Ant-Man content.
public sealed class AbilityFormDestinationTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void IndependentDrawShapeRequiresTheResolvingPlayerForBothSteps()
    {
        var draw = new AbilityEffect.Draw(new AbilityPlayerSelection.OnePlayer(AbilityPlayer.You), 1);
        var other = new AbilityEffect.ChangeForm(AbilityPlayer.FirstPlayer, new AbilityFormDestination.OtherHero());
        Assert.Throws<RulesNotImplementedException>(() =>
            AbilityFormDestinationResolver.RequireStandalone(new AbilityEffect.Sequence([other, draw])));
        var own = new AbilityEffect.ChangeForm(AbilityPlayer.You, new AbilityFormDestination.OtherHero());
        AbilityFormDestinationResolver.RequireStandalone(new AbilityEffect.Sequence([own, draw]));
    }

    [Fact]
    public void ReadyCompositionRequiresTheResolvingPlayersHeroAndExactOrder()
    {
        var change = new AbilityEffect.ChangeForm(AbilityPlayer.You, new AbilityFormDestination.OtherHero());
        var ready = new AbilityEffect.CardAction(AbilityCardInstruction.Ready,
            new AbilityCardSelection.Bound(AbilityCardBinding.YourHero));
        AbilityFormDestinationResolver.RequireStandalone(new AbilityEffect.Sequence([change, ready]));
        var wrongTarget = ready with { Selection = new AbilityCardSelection.Bound(AbilityCardBinding.You) };
        var wrongPlayer = change with { Player = AbilityPlayer.FirstPlayer };
        Assert.Throws<RulesNotImplementedException>(() =>
            AbilityFormDestinationResolver.RequireStandalone(new AbilityEffect.Sequence([change, wrongTarget])));
        Assert.Throws<RulesNotImplementedException>(() =>
            AbilityFormDestinationResolver.RequireStandalone(new AbilityEffect.Sequence([wrongPlayer, ready])));
        Assert.Throws<RulesNotImplementedException>(() =>
            AbilityFormDestinationResolver.RequireStandalone(new AbilityEffect.Sequence([ready, change])));
        Assert.Throws<RulesNotImplementedException>(() =>
            AbilityFormDestinationResolver.RequireStandalone(new AbilityEffect.Simultaneous([change, ready])));
    }

    [Theory]
    [InlineData("12001a", "12001b")]
    [InlineData("12001a", "12001c")]
    [InlineData("12001b", "12001a")]
    [InlineData("12001b", "12001c")]
    [InlineData("12001c", "12001a")]
    [InlineData("12001c", "12001b")]
    [Rule("rr:flip.1")]
    public void ExactDestinationUsesTheNamedFaceWithoutMutatingTheCard(string from, string to)
    {
        // rr:flip.1: a three-sided card has flipped when its faceup side changes.
        // mc12 rules clarifications permit changing to either other face.
        var identity = Identity(from);
        Assert.Equal(to, AbilityFormDestinationResolver.Resolve(identity, Cards, from,
            FormsFor(from), new AbilityFormDestination.Face(to)));
        Assert.Equal(from, identity.FaceId);
    }

    [Theory]
    [InlineData("12001a", "12001c")]
    [InlineData("12001c", "12001a")]
    [InlineData("12001b", null)]
    public void OtherHeroRequiresAnActiveHeroFace(string from, string? expected)
    {
        var identity = Identity(from);
        Assert.Equal(expected, AbilityFormDestinationResolver.Resolve(identity, Cards, from,
            FormsFor(from), new AbilityFormDestination.OtherHero()));
    }

    [Fact]
    public void SameFaceIsNoEffectButForeignFaceAndAmbiguousHeroAreRejected()
    {
        var identity = Identity("12001b");
        Assert.Null(AbilityFormDestinationResolver.Resolve(identity, Cards, "12001b", FormsFor("12001b"),
            new AbilityFormDestination.Face("12001b")));
        Assert.Throws<RulesNotImplementedException>(() => AbilityFormDestinationResolver.Resolve(identity, Cards,
            "12001b", FormsFor("12001b"), new AbilityFormDestination.Face("01001a")));
        Assert.Throws<RulesNotImplementedException>(() => AbilityFormDestinationResolver.Resolve(identity, Cards,
            "12001b", FormsFor("12001b"), new AbilityFormDestination.Form(Forms.Hero)));
    }

    [Fact]
    public void ProjectedFaceSelectsOtherHeroWithoutReadingTheLiveFace()
    {
        var identity = Identity("12001a");
        Assert.Equal("12001a", AbilityFormDestinationResolver.Resolve(identity, Cards, "12001c",
            FormsFor("12001c"), new AbilityFormDestination.OtherHero()));
        Assert.Equal("12001a", identity.FaceId);
    }

    [Fact]
    [Rule("rr:flip.1")]
    public void ConsecutiveHypotheticalHeroChangesUseThePreviousDestination()
    {
        // rr:flip.1: a three-sided card has flipped when its faceup side changes.
        var identity = Identity("12001a");
        var otherHero = new AbilityFormDestination.OtherHero();
        string first = AbilityFormDestinationResolver.Resolve(identity, Cards, identity.FaceId,
            FormsFor(identity.FaceId), otherHero)!;
        string second = AbilityFormDestinationResolver.Resolve(identity, Cards, first,
            FormsFor(first), otherHero)!;

        Assert.Equal("12001c", first);
        Assert.Equal("12001a", second);
        Assert.Equal("12001a", identity.FaceId);
    }

    [Fact]
    public void GenericDestinationsPreserveTwoFaceAndAlreadyInFormBehavior()
    {
        var world = new World(Cards, 1, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01019a,01019b", seat.Hero);
        seat.IdentityCard.TurnTo("01019b");
        Assert.Equal("01019a", AbilityFormDestinationResolver.Resolve(seat.IdentityCard, Cards, "01019b",
            FormsFor("01019b"), new AbilityFormDestination.Form(Forms.Hero)));
        Assert.Null(AbilityFormDestinationResolver.Resolve(seat.IdentityCard, Cards, "01019b",
            FormsFor("01019b"), new AbilityFormDestination.Form(Forms.AlterEgo)));
    }

    private static Card Identity(string face)
    {
        var world = new World(Cards, 1, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        seat.IdentityCard.TurnTo(face);
        return seat.IdentityCard;
    }

    private static HashSet<string> FormsFor(string face) =>
        new HashSet<string>(StringComparer.Ordinal) { Forms.OfFace(face, Cards)! };
}
