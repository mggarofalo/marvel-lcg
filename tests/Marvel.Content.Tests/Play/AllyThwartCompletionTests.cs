using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AllyThwartCompletionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:thwart.2")]
    [Rule("rr:initiating-abilities.2")]
    [Rule("rr:ability.11")]
    [Rule("rr:response.1")]
    [Rule("rr:ownership-and-control.2.1")]
    [Rule("rr:consequential-damage.1")]
    public void LabeledThwartOffersControllerAnOptionalResponseBeforeFollowingEffect(bool accept)
    {
        // "resolving that ability is considered to thwart"; responses occur
        // "immediately after that triggering condition becomes true" and are "optional".
        // "Players can only trigger response abilities on cards they control";
        // attached upgrades "are controlled by that other player".
        var fixture = new AllyThwartCompletionFixture(followingEffect: true);
        int hand = fixture.World.Seats[1].Hand.Cards.Count;
        var prompt = fixture.BeginLabeledThwart();
        Assert.Equal(2, fixture.Scheme.Tokens["k_threat"]);
        Assert.Equal(0, fixture.Ally.Damage);
        Assert.Equal(1, prompt.Player);
        Assert.True(prompt.Cancellable);
        Assert.Equal(TimingPriority.Response, prompt.When);
        Assert.Equal(hand, fixture.World.Seats[1].Hand.Cards.Count);
        Assert.True(fixture.World.Agenda.Occurrence!.Is(CharacterPowerTiming.ThwartEnds));
        Assert.False(fixture.World.Agenda.Occurrence.Is(BasicPowerTiming.ThwartEnds));
        var response = Assert.Single(prompt.Affordances, option => option.AnchorId == fixture.Gloves.ObjectId);
        if (accept)
        {
            fixture.Answer(prompt, Decision.Take(response.Id));
            var target = Assert.IsType<Prompt>(fixture.Work());
            Assert.Equal(hand, fixture.World.Seats[1].Hand.Cards.Count);
            Assert.Contains(target.Affordances, item => item.Id == fixture.Villain.ObjectId);
            Assert.DoesNotContain(target.Affordances, item => item.Id == fixture.Ally.ObjectId);
            fixture.Answer(target, Decision.Take(fixture.Villain.ObjectId));
        }
        else fixture.Answer(prompt, Decision.Decline);
        Assert.Null(fixture.Work());
        Assert.Equal(accept ? 1 : 0, fixture.Villain.Damage);
        Assert.Equal(hand + 1, fixture.World.Seats[1].Hand.Cards.Count);
        Assert.Equal(1, fixture.Ally.Damage);
        Assert.False(fixture.World.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:consequential-damage.1")]
    public void BasicAttackAndThwartRespondBeforeConsequentialDamage(bool attack)
    {
        // "Consequential damage is dealt to an ally after resolving abilities
        // that are triggered by the ally attacking or thwarting."
        var fixture = new AllyThwartCompletionFixture();
        AllyBasicPowers.AllyPower(fixture.World, fixture.World.Facts, fixture.Ally,
            attack ? fixture.Villain : fixture.Scheme,
            attack ? BasicPowers.AttackVerb : BasicPowers.ThwartVerb, []);
        var prompt = Assert.IsType<Prompt>(fixture.Work());
        Assert.Equal(0, fixture.Ally.Damage);
        Assert.Equal(attack ? 2 : 0, fixture.Villain.Damage);
        Assert.Equal(attack ? 3 : 2, fixture.Scheme.Tokens["k_threat"]);
        var occurrence = fixture.World.Agenda.Occurrence!;
        Assert.True(occurrence.Is(attack ? Steps.AttackEnds : CharacterPowerTiming.ThwartEnds));
        Assert.True(occurrence.Is(attack ? Steps.BasicAttackEnds : BasicPowerTiming.ThwartEnds));
        fixture.Answer(prompt, Decision.Take(Assert.Single(prompt.Affordances).Id));
        fixture.Answer(Assert.IsType<Prompt>(fixture.Work()), Decision.Take(fixture.Villain.ObjectId));
        Assert.Null(fixture.Work());
        Assert.Equal(attack ? 3 : 1, fixture.Villain.Damage);
        Assert.Equal(1, fixture.Ally.Damage);
    }

    [Fact]
    public void AnotherAllyThwartDoesNotTriggerAttachedGloves()
    {
        var fixture = new AllyThwartCompletionFixture();
        var other = fixture.World.CreateCard("01068", fixture.Ally.Area);
        var action = Assert.Single(fixture.Runner.Actions(fixture.World, 1), item => item.Card == other.ObjectId);
        fixture.Runner.ExecuteAction(fixture.World, new PlayerAction(action, [], []));
        Assert.Null(fixture.Work());
        Assert.Equal(2, fixture.Scheme.Tokens["k_threat"]);
        Assert.Equal(0, fixture.Villain.Damage);
        Assert.Equal(1, other.Damage);
    }

    [Theory]
    [InlineData("blank")]
    [InlineData("departed")]
    [InlineData("hostDeparted")]
    [Rule("rr:ability.2")]
    public void AnUnavailableGlovesSourceCannotRespond(string state)
    {
        // Player card abilities "may only be used if the card is in play".
        var fixture = new AllyThwartCompletionFixture();
        if (state == "blank")
            fixture.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, PrintedText.Blanked,
                Card: fixture.Gloves.ObjectId, Affects: fixture.Gloves.ObjectId)
                { TargetIncarnation = fixture.Gloves.Incarnation });
        else if (state == "departed") Discard.Card(fixture.World, fixture.Gloves, "test", []);
        else Discard.Card(fixture.World, fixture.Ally, "test", []);
        if (state == "hostDeparted")
        {
            Assert.DoesNotContain(fixture.Runner.Actions(fixture.World, 1), item => item.Card == fixture.Ally.ObjectId);
            Assert.Equal(DeckType.DiscardPile, fixture.Gloves.Area.Type);
        }
        else
        {
            var action = Assert.Single(fixture.Runner.Actions(fixture.World, 1), item => item.Card == fixture.Ally.ObjectId);
            fixture.Runner.ExecuteAction(fixture.World, new PlayerAction(action, [], []));
            Assert.Null(fixture.Work());
            Assert.Equal(2, fixture.Scheme.Tokens["k_threat"]);
        }
        Assert.Equal(0, fixture.Villain.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:initiating-abilities.3")]
    [Rule("rr:leaves-play.1")]
    public void InitiatedResponseSurvivesHostDepartureAndNestedChoice(bool reenter)
    {
        // "the sequence does not stop from completing if that card leaves play".
        var fixture = new AllyThwartCompletionFixture(followingEffect: true, nestedResponse: true);
        int hand = fixture.World.Seats[1].Hand.Cards.Count;
        var response = fixture.BeginLabeledThwart();
        fixture.Answer(response, Decision.Take(Assert.Single(response.Affordances).Id));
        var choice = Assert.IsType<Prompt>(fixture.Work());
        Assert.Equal(2, choice.Affordances.Count);
        var area = fixture.Ally.Area;
        Discard.Card(fixture.World, fixture.Ally, "test", []);
        if (reenter) World.MoveToTop(fixture.Ally, area);
        fixture.Answer(choice, Decision.Take(choice.Affordances[0].Id));
        var target = Assert.IsType<Prompt>(fixture.Work());
        Assert.Equal(hand + 1, fixture.World.Seats[1].Hand.Cards.Count);
        fixture.Answer(target, Decision.Take(fixture.Villain.ObjectId));
        Assert.Null(fixture.Work());
        Assert.Equal(1, fixture.Villain.Damage);
        Assert.Equal(hand + 2, fixture.World.Seats[1].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, fixture.Gloves.Area.Type);
        Assert.Equal(0, fixture.Ally.Damage);
        Assert.False(fixture.World.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:ally.3")]
    [Rule("rr:confuse-confused.1")]
    public void ConfusedAllyNeverCompletesItsBasicOrLabeledThwart(bool basic)
    {
        // "If an ally attempts to attack or thwart while stunned or confused,
        // respectively, that ally will not take consequential damage."
        var fixture = new AllyThwartCompletionFixture();
        Statuses.Give(fixture.World, fixture.Ally, Statuses.Confused);
        if (basic)
            AllyBasicPowers.AllyPower(fixture.World, fixture.World.Facts, fixture.Ally,
                fixture.Scheme, BasicPowers.ThwartVerb, []);
        else
        {
            var action = Assert.Single(fixture.Runner.Actions(fixture.World, 1), item => item.Card == fixture.Ally.ObjectId);
            fixture.Runner.ExecuteAction(fixture.World, new PlayerAction(action, [], []));
        }
        Assert.Null(fixture.Work());
        Assert.Equal(3, fixture.Scheme.Tokens["k_threat"]);
        Assert.Equal(0, fixture.Ally.Damage);
        Assert.False(Statuses.Has(fixture.World, fixture.Ally, Statuses.Confused));
    }

    [Fact]
    [Rule("rr:consequential-damage.2")]
    [Rule("rr:consequential-damage.2.1")]
    public void DepartedBasicThwartTargetAbortsCompletionAndConsequentialDamage()
    {
        // If the target "leaves play before ... removes threat equal to its THW",
        // "That ally is not considered to have attacked or thwarted".
        var fixture = new AllyThwartCompletionFixture();
        var sideScheme = fixture.World.CreateCard("01107", fixture.World.AreaOf(DeckType.SideSchemesArea));
        sideScheme.PlaceTokens("k_threat", 3);
        AllyBasicPowers.AllyPower(fixture.World, fixture.World.Facts, fixture.Ally,
            sideScheme, BasicPowers.ThwartVerb, []);
        Discard.Card(fixture.World, sideScheme, "test", []);
        Assert.Null(fixture.Work());
        Assert.Equal(0, fixture.Ally.Damage);
        Assert.False(fixture.Ally.Ready);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:leaves-play.1")]
    public void BasicConsequentialDamageCannotFollowAReenteredAlly(bool attack)
    {
        // A card that leaves play "is considered to be a new copy of the card".
        var fixture = new AllyThwartCompletionFixture();
        AllyBasicPowers.AllyPower(fixture.World, fixture.World.Facts, fixture.Ally,
            attack ? fixture.Villain : fixture.Scheme,
            attack ? BasicPowers.AttackVerb : BasicPowers.ThwartVerb, []);
        var prompt = Assert.IsType<Prompt>(fixture.Work());
        var area = fixture.Ally.Area;
        Discard.Card(fixture.World, fixture.Ally, "test", []);
        World.MoveToTop(fixture.Ally, area);
        fixture.Answer(prompt, Decision.Decline);
        Assert.Null(fixture.Work());
        Assert.Equal(0, fixture.Ally.Damage);
    }

    [Fact]
    public void UnlabeledThreatRemovalIsNotAnAllyThwart()
    {
        var fixture = new AllyThwartCompletionFixture(unlabeled: true);
        var action = Assert.Single(fixture.Runner.Actions(fixture.World, 1), item => item.Card == fixture.Ally.ObjectId);
        fixture.Runner.ExecuteAction(fixture.World, new PlayerAction(action, [], []));
        Assert.Null(fixture.Work());
        Assert.Equal(2, fixture.Scheme.Tokens["k_threat"]);
        Assert.Equal(0, fixture.Ally.Damage);
        Assert.Equal(0, fixture.Villain.Damage);
    }
}
