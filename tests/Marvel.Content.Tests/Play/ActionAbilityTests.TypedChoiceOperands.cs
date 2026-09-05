using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Fact]
    public void SimultaneousChoiceAdvertisesEveryEffectAndExecutesTheChosenOrder()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"and":[{"dealDamage":{"cards":"you","amount":1}},
              {"heal":{"card":"you","amount":2}},
              {"giveStatus":{"card":"you","status":"tough"}}]}
            """, cost: """{"exhaust":"this"}""");
        var (world, source) = FixedCountBoard(runner);
        var identity = world.Seats[0].IdentityCard;
        identity.TakeDamage(1);
        runner.Act(world, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, source, 0, choice.Index, choice.Tier)!;
        var order = Assert.Single(prompt.Affordances);

        Assert.Equal(Question.Order, prompt.Asking);
        Assert.Equal(world.FirstPlayer, prompt.Player);
        Assert.Equal([0, 1, 2], order.Targets!.Legal);
        Assert.Equal(3, order.Targets.Min);
        Assert.Equal(3, order.Targets.Max);
        runner.Chose(world, source, 0, choice.Index,
            Decision.Take(order.Id, [1, 0, 2], []), choice.Tier);

        Assert.Equal(1, identity.Damage);
        Assert.Equal(1, Statuses.Count(world, identity, Statuses.Tough));
        Assert.False(source.Ready);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChoiceDescriptionsComeFromTheCompiledOptions(bool editBeforeAction)
    {
        var (runner, fields) = MutableEffectRunner("choose", """
            {"descriptions":["Heal one","Heal two"],"options":[
              {"heal":{"card":"you","amount":1}},
              {"heal":{"card":"you","amount":2}}]}
            """, false);
        var (world, source) = FixedCountBoard(runner);
        world.Seats[0].IdentityCard.TakeDamage(3);
        if (editBeforeAction) fields["descriptions"] = new AbilityValue.Number(0);

        runner.Act(world, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        fields["descriptions"] = new AbilityValue.Number(0);
        var prompt = runner.Choosing(world, source, 0, choice.Index, choice.Tier)!;

        Assert.Equal(Question.Option, prompt.Asking);
        Assert.Equal([0, 1], prompt.Affordances.Select(option => option.Id));
        Assert.Equal(["Heal one", "Heal two"], prompt.Affordances.Select(option => option.Description));
        runner.Chose(world, source, 0, choice.Index, Decision.Take(1), choice.Tier);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.False(source.Ready);
    }

    [Theory]
    [InlineData("payOrEffect", false)]
    [InlineData("payOrEffect", true)]
    [InlineData("payOrExhaust", false)]
    [InlineData("payOrExhaust", true)]
    public void PaymentChoicesOfferAndRequireTheCompiledResources(string operation, bool editBeforeAction)
    {
        var (runner, fields) = MutableEffectRunner(operation,
            """{"resources":"BB","otherwise":{"exhaust":"you"}}""", false);
        var (world, source) = FixedCountBoard(runner);
        var hand = world.Seats[0].Hand;
        var genius = world.CreateCard("01089", hand);
        var energy = world.CreateCard("01088", hand);
        world.CreateCard("01005", world.Seats[0].Deck);
        if (editBeforeAction) fields["resources"] = new AbilityValue.Word("YYY");

        runner.Act(world, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        fields["resources"] = new AbilityValue.Word("YYY");
        var prompt = runner.Choosing(world, source, 0, choice.Index, choice.Tier)!;
        Assert.Equal([0, 1], prompt.Affordances.Select(option => option.Id));
        var payment = Assert.Single(prompt.Affordances[0].Costs!);
        Assert.Equal("2", payment.Cost);
        Assert.Equal(["BB"], payment.Rule);

        // Changing the syntax cannot substitute energy for the compiled
        // mental-resource requirement, even after the question was offered.
        fields["resources"] = new AbilityValue.Word("YY");
        Assert.Throws<RulesNotImplementedException>(() => runner.Chose(
            world, source, 0, choice.Index, Decision.Take(0, [], [energy.ObjectId]), choice.Tier));
        Assert.Equal([genius, energy], hand.Cards);
        runner.Chose(world, source, 0, choice.Index,
            Decision.Take(0, [], [genius.ObjectId]), choice.Tier);

        Assert.Equal([energy], hand.Cards);
        Assert.Equal(DeckType.DiscardPile, genius.Area.Type);
        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.False(source.Ready);
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
    }

    [Theory]
    [InlineData("yourDeck", "plain", true)]
    [InlineData("encounterDeck", "plain", true)]
    [InlineData("encounterDiscardPile", "plain", false)]
    [InlineData("yourDeck", "discardable", true)]
    [InlineData("yourDeck", "withoutAnotherCopyAttached", true)]
    [InlineData("yourDeck", "minBy", true)]
    [InlineData("yourDeck", "maxBy", true)]
    [InlineData("yourDeck", "withTrait", true)]
    public void CardChoicesKeepTheirCompiledCandidatesAndConcealment(string area, string wrapper, bool concealed)
    {
        string selection = $$$"""{"cardsIn":{"area":"{{{area}}}"}}""";
        selection = wrapper switch
        {
            "plain" => selection,
            "withTrait" => $$$"""{"withTrait":{"cards":{{{selection}}},"trait":"TECH"}}""",
            "minBy" or "maxBy" => $$$"""{"{{{wrapper}}}":{"of":{{{selection}}},"by":"cost"}}""",
            _ => $$$"""{"{{{wrapper}}}":{{{selection}}}}""",
        };
        var (runner, fields) = MutableEffectRunner("chooseCard",
            $$$"""{"from":{{{selection}}},"effect":{"addToHand":"chosen"}}""", false);
        var (world, source) = FixedCountBoard(runner);
        var pile = area switch
        {
            "yourDeck" => world.Seats[0].Deck,
            "encounterDeck" => world.AreaOf(DeckType.EncounterDeck),
            _ => world.AreaOf(DeckType.EncounterDiscardPile),
        };
        string face = wrapper == "withTrait" ? "01035" : "01005";
        var first = world.CreateCard(face, pile);
        var second = world.CreateCard(face, pile);

        runner.Act(world, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        fields["from"] = new AbilityValue.Word("you");
        var prompt = runner.Choosing(world, source, 0, choice.Index, choice.Tier)!;
        Assert.Equal(Question.Element, prompt.Asking);
        Assert.Equal(concealed, prompt.ExposesConcealedCandidates);
        Assert.Equal([first.ObjectId, second.ObjectId], prompt.Affordances.Select(option => option.Id));
        Assert.Throws<RulesNotImplementedException>(() => runner.Chose(world, source, 0, choice.Index,
            Decision.Take(world.Seats[0].IdentityCard.ObjectId), choice.Tier));
        Assert.Equal([first, second], pile.Cards);
        Assert.Empty(world.Seats[0].Hand.Cards);

        runner.Chose(world, source, 0, choice.Index, Decision.Take(second.ObjectId), choice.Tier);

        Assert.Equal([first], pile.Cards);
        Assert.Equal([second], world.Seats[0].Hand.Cards);
        Assert.False(source.Ready);
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
    }

    [Fact]
    public void SpecialOrderingUsesTheCompiledUpgradeSelection()
    {
        var (runner, fields) = MutableEffectRunner("resolveSpecials",
            """{"cards":{"query":"upgradesYouControl"}}""", false);
        var (world, source) = FixedCountBoard(runner);
        var upgrades = world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0);
        var first = world.CreateCard("01046", upgrades);
        var second = world.CreateCard("01047", upgrades);
        fields["cards"] = new AbilityValue.Word("you");
        runner.Act(world, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, source, 0, choice.Index, choice.Tier)!;
        var order = Assert.Single(prompt.Affordances);

        Assert.Equal([first.ObjectId, second.ObjectId], order.Targets!.Legal);
        Assert.Equal(2, order.Targets.Min);
        Assert.Equal(2, order.Targets.Max);
        Assert.Throws<RulesNotImplementedException>(() => runner.Chose(world, source, 0, choice.Index,
            Decision.Take(order.Id, [world.Seats[0].IdentityCard.ObjectId], []), choice.Tier));
        Assert.DoesNotContain(world.Agenda.Outstanding, step => step.What == Steps.ResolveSpecial);
        runner.Chose(world, source, 0, choice.Index,
            Decision.Take(order.Id, [second.ObjectId, first.ObjectId], []), choice.Tier);

        Assert.Equal([second.ObjectId, first.ObjectId], world.Agenda.Outstanding
            .Where(step => step.What == Steps.ResolveSpecial).Select(step => step.Subject));
        Assert.False(source.Ready);
    }

    [Theory]
    [InlineData("thwartDifferentSchemes")]
    [InlineData("legalPractice")]
    public void ThwartChoicesOfferAndValidateTheCompiledSchemes(string operation)
    {
        var (runner, fields) = MutableEffectRunner(operation, """
            {"schemes":{"query":"sideSchemes"},"power":{"thwart":{
              "target":"chosen","effect":{"removeThreat":{"scheme":"chosen","amount":1}}}}}
            """, false);
        var (world, source) = FixedCountBoard(runner);
        var main = world.CreateCard("01116a", world.AreaOf(DeckType.MainSchemesArea));
        var side = world.CreateCard("01127", world.AreaOf(DeckType.SideSchemesArea));
        main.PlaceTokens("k_threat", 4);
        side.PlaceTokens("k_threat", 3);
        var handCard = world.CreateCard("01088", world.Seats[0].Hand);
        world.CreateCard("01005", world.Seats[0].Deck);
        runner.Act(world, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        fields["schemes"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
        {
            ["query"] = new AbilityValue.Word("mainScheme"),
        });
        var prompt = Sequence.Work(world, Cards, runner, [])!;
        var selection = Assert.Single(prompt.Affordances);
        if (operation == "legalPractice") Assert.Equal(side.ObjectId, selection.Id);
        else Assert.Equal([side.ObjectId], selection.Targets!.Legal);
        var wrong = operation == "legalPractice"
            ? Decision.Take(main.ObjectId, [handCard.ObjectId], [])
            : Decision.Take(selection.Id, [main.ObjectId], []);
        Assert.Throws<RulesNotImplementedException>(() => runner.Chose(
            world, source, 0, choice.Index, wrong, choice.Tier));
        Assert.Equal([handCard], world.Seats[0].Hand.Cards);
        Assert.Equal(4, main.Tokens["k_threat"]);
        Assert.Equal(3, side.Tokens["k_threat"]);

        var answer = operation == "legalPractice"
            ? Decision.Take(side.ObjectId, [handCard.ObjectId], [])
            : Decision.Take(selection.Id, [side.ObjectId], []);
        Sequence.Answer(world, Cards, runner, prompt, answer, []);
        Sequence.Finish(world, Cards, runner, []);

        Assert.Equal(4, main.Tokens["k_threat"]);
        Assert.Equal(2, side.Tokens["k_threat"]);
        Assert.Equal(operation == "legalPractice" ? DeckType.DiscardPile : DeckType.HandsArea, handCard.Area.Type);
        Assert.False(source.Ready);
        Assert.False(world.Agenda.IsBusy);
    }
}
