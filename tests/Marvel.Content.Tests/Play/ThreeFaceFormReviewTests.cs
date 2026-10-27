using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent engine checks on manually built, nonpublic three-face boards.</summary>
public sealed class ThreeFaceFormReviewTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData("12001")]
    [InlineData("13001")]
    public void FormOffersBelongOnlyToTheActiveSeatAndKeepDistinctPrintedSizeNames(string identity)
    {
        var (_, game) = Playing(identity, "b", players: 2);
        var prompt = game.Pending!;
        var offers = prompt.Affordances.Where(a => a.Verb == Game.ChangeForm).ToArray();
        Assert.Equal(new[] { identity + "a", identity + "c" }, offers.Select(a => a.Label));
        Assert.Contains("TINY", offers[0].DisplayLabel!);
        Assert.Contains("GIANT", offers[1].DisplayLabel!);
        Assert.All(offers, offer =>
        {
            Assert.Equal(0, offer.AnchorPlayer);
            Assert.Equal(DecisionRejection.UnauthorizedActor,
                DecisionAssessment.Assess(prompt, Decision.Take(offer.Id), actor: 1).Rejection);
            Assert.True(DecisionAssessment.Assess(prompt, Decision.Take(offer.Id), actor: 0).IsAccepted);
        });
        Assert.Null(game.PromptFor(1));
    }

    [Theory]
    [InlineData("12001")]
    [InlineData("13001")]
    public void IdenticalSeedAndAnswersProduceTheSameHandlesEventsAndNextDecision(string identity)
    {
        var (left, first) = Playing(identity, "b");
        var (right, second) = Playing(identity, "b");
        Assert.Equal(first.Pending!.Affordances, second.Pending!.Affordances);
        int selected = first.Pending.Affordances.Single(a => a.Label == identity + "c").Id;
        var result = first.Resolve(Decision.Take(selected));
        var control = second.Resolve(Decision.Take(selected));
        Assert.Equal(result.Events, control.Events);
        Assert.Equal(left.Digest().Canonical(), right.Digest().Canonical());
        Assert.Equal(first.Pending!.Affordances, second.Pending!.Affordances);
        Assert.Equal(left.Random.Generator.GetState().Words, right.Random.Generator.GetState().Words);
        Assert.Equal(left.Random.Generator.GetState().Index, right.Random.Generator.GetState().Index);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("current")]
    [InlineData("spent")]
    [InlineData("prohibited")]
    public void RejectedSelectionPreservesInformationSignalsAndAllPendingState(string reason)
    {
        var (world, game) = Playing("12001", "b");
        int selected = game.Pending!.Affordances.Single(a => a.Label == "12001c").Id;
        if (reason == "unknown") selected = int.MaxValue;
        if (reason == "current") Forms.Change(world.Seats[0], Cards, "12001c");
        if (reason == "spent") world.Seats[0].FormChangedInRound = game.Round;
        if (reason == "prohibited") world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, Kind: FormChangePermissions.ProhibitedField,
            Amount: 1, Card: world.Seats[0].IdentityCard.ObjectId,
            Affects: world.Seats[0].IdentityCard.ObjectId));
        world.RecordInformation(InformationKind.Search);
        string digest = world.Digest().Canonical();
        var pending = game.Pending;
        var agenda = world.Agenda.Outstanding.ToArray();
        var random = world.Random.Generator.GetState();
        int receipt = world.Seats[0].FormChangedInRound;

        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(selected)));

        Assert.Same(pending, game.Pending);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(agenda, world.Agenda.Outstanding);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(receipt, world.Seats[0].FormChangedInRound);
        Assert.Equal(InformationKind.Search, Assert.Single(new Resolution(world, pending, []).Information).Kind);
    }

    [Theory]
    [InlineData("12001", "a", "c", 3)]
    [InlineData("12001", "c", "b", 0)]
    [InlineData("13001", "a", "c", 3)]
    [InlineData("13001", "c", "b", 0)]
    [Rule("rr:after")]
    [Rule("rr:form-change-form.1")]
    public void VoluntaryChangeRunsTaskmasterExactlyOnceOnlyWhenArrivingInHeroForm(
        string identity, string from, string to, int damage)
    {
        // 04093: "After a player changes to hero form". RR after: the condition
        // "has resolved completely"; RR form: "Once each round, during their turn".
        var (world, game) = Playing(identity, from);
        world.CreateCard("04093", world.AreaOf(DeckType.VillainArea));
        var bottom = world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        var boost = world.CreateCard("04064", world.AreaOf(DeckType.EncounterDeck));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        int selected = game.Pending!.Affordances.Single(a => a.Label == identity + to).Id;

        game.Resolve(Decision.Take(selected));

        Assert.Equal(identity + to, world.Seats[0].IdentityCard.FaceId);
        Assert.Equal(damage, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(damage == 0 ? DeckType.EncounterDeck : DeckType.EncounterDiscardPile, boost.Area.Type);
        Assert.Equal(DeckType.EncounterDeck, bottom.Area.Type);
        Assert.Equal(game.Round, world.Seats[0].FormChangedInRound);
        Assert.Equal(0, game.Pending!.Player);
        Assert.DoesNotContain(game.Pending.Affordances, a => a.Verb == Game.ChangeForm);
        Sequence.Work(world, Cards, runner, []);
        Assert.Equal(damage, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDeck, bottom.Area.Type);
    }

    private static (World World, Game Game) Playing(string identity, string from, int players = 1)
    {
        var world = new World(Cards, players, seed: 776);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat("p" + player);
            seat.IdentityCard = world.CreateCard(identity + "a," + identity + "b," + identity + "c", seat.Hero);
            seat.IdentityCard.TurnTo(identity + from);
        }
        var game = Game.BeginWithoutCardAbilities(world, Cards);
        for (int player = 0; player < players; player++)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        return (world, game);
    }
}
