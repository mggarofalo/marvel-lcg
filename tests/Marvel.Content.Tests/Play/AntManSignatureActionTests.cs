using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production book on synthetic multiplayer boards; no public pack admission.
public sealed class AntManSignatureActionTests : CoreCardTestBase
{
    [Theory]
    [InlineData("12001a", 0, 2)]
    [InlineData("12001c", 2, 0)]
    [InlineData("12001b", 0, 0)]
    [Rule("rr:ability.8")]
    public void WaspEntryUsesHerControllersCurrentForm(string face, int damage, int threat)
    {
        // Optional abilities are controlled by "the player who controls the card".
        var world = Board(face);
        var runner = AuthoredCards.Runner();
        var wasp = world.CreateCard("12002", world.Seats[1].Hand);
        var energy = world.CreateCard("12021", world.Seats[1].Hand);
        var strength = world.CreateCard("12023", world.Seats[1].Hand);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 6);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        CardPlay.Play(world, Cards, runner, world.Seats[1], wasp,
            [energy.ObjectId, strength.ObjectId], [], []);
        var prompt = Sequence.Work(world, Cards, runner, []);
        if (face == "12001b") Assert.Null(prompt);
        else
        {
            Assert.NotNull(prompt);
            Assert.Equal(1, prompt.Player);
            var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == wasp.ObjectId);
            int target = face == "12001c" ? villain.ObjectId : scheme.ObjectId;
            Sequence.Answer(world, Cards, runner, prompt, Decision.Take(offer.Id), []);
            var choice = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
            int invalid = face == "12001c" ? scheme.ObjectId : villain.ObjectId;
            Assert.DoesNotContain(choice.Affordances, a => a.Id == invalid);
            string before = world.Digest().Canonical();
            var random = world.Random.Generator.GetState();
            var pending = world.Agenda.Outstanding.ToArray();
            Assert.Throws<ArgumentException>(() => Sequence.Answer(world, Cards, runner, choice, Decision.Take(invalid), []));
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
            Choose(world, runner, world.Cards[target]);
        }
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(damage, villain.Damage);
        Assert.Equal(6 - threat, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.AlliesArea, wasp.Area.Type);
        Assert.Equal(2, Modified(world, wasp, "attack"));
        Assert.Equal(2, Modified(world, wasp, "thwart"));
        Assert.Equal(3, DamagePlacement.Health(world, Cards, wasp));
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.Equal(DeckType.DiscardPile, strength.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("12001a")]
    [InlineData("12001c")]
    [Rule("rr:ability.11")]
    public void WaspEntryResponseCanBeDeclined(string face)
    {
        // "Unless prefaced by the word Forced", responses are optional.
        var world = Board(face);
        var runner = AuthoredCards.Runner();
        var wasp = world.CreateCard("12002", world.Seats[1].Hand);
        var energy = world.CreateCard("12021", world.Seats[1].Hand);
        var strength = world.CreateCard("12023", world.Seats[1].Hand);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 6);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        CardPlay.Play(world, Cards, runner, world.Seats[1], wasp,
            [energy.ObjectId, strength.ObjectId], [], []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(1, prompt.Player);
        Assert.Contains(prompt.Affordances, a => a.AnchorId == wasp.ObjectId);
        Assert.True(prompt.Cancellable);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(6, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.AlliesArea, wasp.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [Rule("rr:ownership-and-control.2")]
    public void HiveMindCountsOnlyControlledSupports(int count)
    {
        // "Cards enter play under their owner's control."
        var world = Board("12001a");
        var runner = AuthoredCards.Runner();
        for (int i = 0; i < count; i++) Support(world, "12007", 1);
        Support(world, "12007", 0);
        world.CreateCard("12007", world.Seats[1].Hand);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 8);
        var source = world.CreateCard("12004", world.Seats[1].Hand);
        var payment = world.CreateCard("12022", world.Seats[1].Hand);
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], []));
        Choose(world, runner, scheme);
        Assert.Equal(6 - count, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("12001a", true)]
    [InlineData("12001c", false)]
    [InlineData("12001b", false)]
    [Rule("rr:cost.1")]
    public void ArmyRequiresTinyAndExhaustsItsOwnCopy(string face, bool offered)
    {
        // A cost arrow distinguishes "a cost from an effect".
        var world = Board(face);
        var runner = AuthoredCards.Runner();
        var mine = Support(world, "12007", 1);
        var theirs = Support(world, "12007", 0);
        var actions = runner.Actions(world, 1);
        Assert.DoesNotContain(actions, a => a.Card == theirs.ObjectId);
        Assert.Equal(offered, actions.Any(a => a.Card == mine.ObjectId));
        if (!offered) return;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        runner.ExecuteAction(world, new PlayerAction(actions.Single(a => a.Card == mine.ObjectId), [], []));
        Choose(world, runner, villain);
        Assert.Equal(1, villain.Damage);
        Assert.False(mine.Ready);
        Assert.True(theirs.Ready);
        Assert.DoesNotContain(runner.Actions(world, 1), a => a.Card == mine.ObjectId);
    }

    [Theory]
    [InlineData("12001a", "12021", "confused")]
    [InlineData("12001c", "12023", "stunned")]
    [Rule("rr:cost.5")]
    public void GauntletsPayTwoTypedResourcesAndExhaustAtomically(string face, string paying, string status)
    {
        // Multiple costs "must be paid simultaneously."
        var world = Board(face);
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("12010", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var foreign = world.CreateCard("12010", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var wrong = world.CreateCard("12022", world.Seats[1].Hand);
        Assert.DoesNotContain(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        var payment = world.CreateCard(paying, world.Seats[1].Hand);
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 1), a => a.Card == foreign.ObjectId);
        string before = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(action, [wrong.ObjectId], [])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.True(source.Ready);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], []));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Choose(world, runner, villain);
        Assert.True(Statuses.Has(world, villain, status));
        Assert.False(Statuses.Has(world, villain, status == "stunned" ? "confused" : "stunned"));
        Assert.False(source.Ready);
        Assert.True(foreign.Ready);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Same(world.Seats[1].Hand, wrong.Area);
        Assert.DoesNotContain(runner.Actions(world, 1), a => a.Card == source.ObjectId);
    }

    [Theory]
    [InlineData("12001b", "12004")]
    [InlineData("12001c", "12004")]
    [InlineData("12001b", "12010")]
    [Rule("rr:ability.2")]
    public void WrongFormHasNoActionDespiteSufficientPayment(string face, string card)
    {
        // Player abilities "may only be used if the card is in play" except events.
        var world = Board(face);
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard(card, card == "12004" ? world.Seats[1].Hand
            : world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        world.CreateCard("12021", world.Seats[1].Hand);
        world.CreateCard("12023", world.Seats[1].Hand);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        Assert.DoesNotContain(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        world.Seats[1].IdentityCard.TurnTo("12001a");
        Assert.Contains(runner.Actions(world, 1), a => a.Card == source.ObjectId);
    }

    [Theory]
    [InlineData("12001a", "Y", "confused")]
    [InlineData("12001c", "R", "stunned")]
    [Rule("rr:wild-resource.1")]
    public void GauntletsAcceptDeclaredWildsButRequireTwoIcons(string face, string icon, string status)
    {
        // Wild resources: players "may specify which resource type" is used.
        var world = Board(face);
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("12010", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var first = world.CreateCard("01055", world.Seats[1].Hand);
        Assert.DoesNotContain(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        var second = world.CreateCard("01055", world.Seats[1].Hand);
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [first.ObjectId, second.ObjectId], [],
            Allocations: [new(first.ObjectId, Cost: 0, PaidAs: icon), new(second.ObjectId, Cost: 0, PaidAs: icon)]));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Choose(world, runner, villain);
        Assert.True(Statuses.Has(world, villain, status));
        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Assert.False(source.Ready);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:ally.3")]
    public void WaspBasicPowersUsePrintedConsequentialDamage(bool attack)
    {
        // "After an ally attacks or thwarts, deal consequential damage to it."
        var world = Board("12001b");
        var runner = AuthoredCards.Runner();
        var wasp = world.CreateCard("12002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var target = world.TheCardIn(attack ? DeckType.VillainArea : DeckType.MainSchemesArea)!;
        if (!attack) target.PlaceTokens("k_threat", 5);
        BasicPowers.AllyPower(world, Cards, wasp, target, attack ? BasicPowers.AttackVerb : BasicPowers.ThwartVerb, []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(1, wasp.Damage);
        Assert.False(wasp.Ready);
        Assert.Equal(attack ? 2 : 0, target.Damage);
        if (!attack) Assert.Equal(3, target.Tokens["k_threat"]);
        Assert.False(world.Agenda.IsBusy);
    }

    private static void Choose(World world, AbilityRunner runner, Card target)
    {
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(1, prompt.Player);
        Assert.Contains(prompt.Affordances, a => a.Id == target.ObjectId);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(target.ObjectId), []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    private static Card Support(World world, string face, int controller) => world.CreateCard(face,
        world.AreaOf(DeckType.SupportsArea, PlayArea.Of(controller), cardOwner: controller));

    private static World Board(string face)
    {
        var world = new World(Cards, 2, 241);
        for (int p = 0; p < 2; p++)
        {
            var seat = world.CreateSeat($"p{p}");
            seat.IdentityCard = world.CreateCard(p == 1 ? "12001a,12001b,12001c" : "01001a,01001b", seat.Hero);
            if (p == 1) seat.IdentityCard.TurnTo(face);
            for (int i = 0; i < 12; i++) world.CreateCard("01087", seat.Deck);
        }
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 12; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        world.Abilities = AuthoredCards.Runner();
        return world;
    }
}

