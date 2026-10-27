using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Nonpublic engine prerequisite; neither hero pack is admitted by this fixture.</summary>
public sealed class ThreeFaceFormTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData("12001", "a", "b")]
    [InlineData("12001", "a", "c")]
    [InlineData("12001", "b", "a")]
    [InlineData("12001", "b", "c")]
    [InlineData("12001", "c", "a")]
    [InlineData("12001", "c", "b")]
    [InlineData("13001", "a", "b")]
    [InlineData("13001", "a", "c")]
    [InlineData("13001", "b", "a")]
    [InlineData("13001", "b", "c")]
    [InlineData("13001", "c", "a")]
    [InlineData("13001", "c", "b")]
    [Rule("rr:form-change-form.1")]
    public void EachFaceOffersBothExplicitDestinationsAndSpendsOneVoluntaryChange(
        string identity, string from, string to)
    {
        // mc12/mc13 Rules Clarifications permit either other face. RR: "Once each
        // round, during their turn, each player is permitted to change form".
        var (world, game) = Playing(identity, from);
        var offers = game.Pending!.Affordances.Where(a => a.Verb == Game.ChangeForm).ToArray();
        Assert.Equal(2, offers.Length);
        Assert.Equal(2, offers.Select(a => a.Id).Distinct().Count());
        Assert.Equal(Forms.Destinations(world.Seats[0], Cards), offers.Select(a => a.Label));
        Assert.All(offers, offer => Assert.Contains(Cards.Title(offer.Label), offer.DisplayLabel!));

        var result = game.Resolve(Decision.Take(offers.Single(a => a.Label == identity + to).Id));

        Assert.Equal(identity + to, world.Seats[0].IdentityCard.FaceId);
        var changed = Assert.Single(result.Events.OfType<CardFormChanged>());
        Assert.Equal(identity + from, changed.From);
        Assert.Equal(identity + to, changed.To);
        Assert.Equal(game.Round, world.Seats[0].FormChangedInRound);
        Assert.DoesNotContain(game.Pending!.Affordances, a => a.Verb == Game.ChangeForm);
    }

    [Theory]
    [InlineData("12001")]
    [InlineData("13001")]
    [Rule("rr:form-change-form.2")]
    public void ExplicitChangePreservesThePhysicalIdentityState(string identity)
    {
        // "When a player changes form, only the form changes."
        var (world, _) = Playing(identity, "a");
        var seat = world.Seats[0];
        var card = seat.IdentityCard;
        card.Exhaust();
        card.TakeDamage(3);
        Statuses.Give(world, card, "stunned");
        var attached = world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(0), host: card.ObjectId, cardOwner: 0));

        Forms.Change(seat, Cards, identity + "c");

        Assert.Same(card, seat.IdentityCard);
        Assert.False(card.Ready);
        Assert.Equal(3, card.Damage);
        Assert.True(Statuses.Has(world, card, "stunned"));
        Assert.Equal(card.ObjectId, attached.Area.Host);
    }

    [Theory]
    [InlineData("12001a")]
    [InlineData("13001a")]
    [InlineData("01001a")]
    [InlineData("01008")]
    public void NoOpForeignAndNonIdentityDestinationsRejectWithoutMutation(string destination)
    {
        var (world, game) = Playing("12001", "a");
        var pending = game.Pending;
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var agenda = world.Agenda.Outstanding.ToArray();

        Assert.Throws<ArgumentException>(() => Forms.ChangeAndSchedule(
            world, world.Seats[0], Cards, game.Round, destination));

        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(agenda, world.Agenda.Outstanding);
        Assert.Same(pending, game.Pending);
        Assert.Equal(0, world.Seats[0].FormChangedInRound);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cannot")]
    public void AProhibitionRejectsAnAlreadyOfferedDestinationWithoutMutation(bool spent)
    {
        // "The word cannot is absolute"; revalidate before clearing information or changing state.
        var (world, game) = Playing("12001", "b");
        int offered = game.Pending!.Affordances.First(a => a.Verb == Game.ChangeForm).Id;
        if (spent) world.Seats[0].FormChangedInRound = game.Round;
        else world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: FormChangePermissions.ProhibitedField, Amount: 1,
            Card: world.Seats[0].IdentityCard.ObjectId, Affects: world.Seats[0].IdentityCard.ObjectId));
        string before = world.Digest().Canonical();
        var pending = game.Pending;
        var agenda = world.Agenda.Outstanding.ToArray();
        var random = world.Random.Generator.GetState();

        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(offered)));

        Assert.Equal(before, world.Digest().Canonical());
        Assert.Same(pending, game.Pending);
        Assert.Equal(agenda, world.Agenda.Outstanding);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
    }

    [Theory]
    [InlineData("12001", 0)]
    [InlineData("12001", 1)]
    [InlineData("13001", 0)]
    [InlineData("13001", 1)]
    [Rule("rr:form-change-form.3")]
    [Rule("rr:after")]
    public void ANonVoluntaryHeroToHeroProcedureOpensTaskmastersActualResponse(string identity, int priorChange)
    {
        // "If a card ability causes a player to change forms, it does not count
        // against the one voluntary form change". mc12/mc13 permit hero-to-hero.
        // 04093: "After a player changes to hero form"; ruling 9218c9f1b9506cdd
        // confirms this includes Tiny -> Giant, consistent with RR 1.8 form/flip.
        var (world, _) = Playing(identity, "a");
        var seat = world.Seats[0];
        seat.FormChangedInRound = priorChange;
        world.CreateCard("04093", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        var boost = world.CreateCard("04064", world.AreaOf(DeckType.EncounterDeck));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        Forms.ChangeAndSchedule(world, seat, Cards, 1, identity + "c");
        var events = new List<GameEvent>();
        Sequence.Work(world, Cards, runner, events);

        Assert.Equal(identity + "c", seat.IdentityCard.FaceId);
        Assert.Equal(Cards.PrintedValue(boost.FaceId, "Boost", 1), seat.IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, boost.Area.Type);
        Assert.Equal(priorChange, seat.FormChangedInRound);
    }

    [Fact]
    public void AnOfferedFaceThatIsNowCurrentRejectsWithoutAdvancingThePrompt()
    {
        var (world, game) = Playing("13001", "b");
        int offered = game.Pending!.Affordances.Single(a => a.Label == "13001c").Id;
        Forms.Change(world.Seats[0], Cards, "13001c");
        var pending = game.Pending;
        string digest = world.Digest().Canonical();
        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(offered)));
        Assert.Same(pending, game.Pending);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(0, world.Seats[0].FormChangedInRound);
    }

    [Fact]
    public void AnUnofferedHandleCannotSelectAnUnchosenForm()
    {
        var (world, game) = Playing("12001", "b");
        var pending = game.Pending;
        string before = world.Digest().Canonical();
        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(int.MaxValue)));
        Assert.Same(pending, game.Pending);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal("12001b", world.Seats[0].IdentityCard.FaceId);
    }

    [Fact]
    public void TwoFaceOffersKeepTheirSemanticIdentityAndToggle()
    {
        var (world, game) = Playing("01001", "b", threeFaces: false);
        var offer = Assert.Single(game.Pending!.Affordances, a => a.Verb == Game.ChangeForm);
        Assert.Equal(Game.ChangeForm, offer.Label);
        game.Resolve(Decision.Take(offer.Id));
        Assert.Equal("01001a", world.Seats[0].IdentityCard.FaceId);
    }

    private static (World World, Game Game) Playing(string identity, string from, bool threeFaces = true)
    {
        var world = new World(Cards, 1, seed: 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard($"{identity}a,{identity}b" + (threeFaces ? $",{identity}c" : ""), seat.Hero);
        seat.IdentityCard.TurnTo(identity + from);
        var game = Game.BeginWithoutCardAbilities(world, Cards);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        return (world, game);
    }
}
