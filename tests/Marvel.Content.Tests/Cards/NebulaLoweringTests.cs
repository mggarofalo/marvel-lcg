using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Play;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Cards;

// The vocabulary the Nebula scenario added, on its own.
public sealed class NebulaLoweringTests : CoreCardTestBase
{
    private const string Reveal = """{"discardUntil":{"from":"encounterDeck","kind":"attachment","trait":"TECHNIQUE","then":"reveal"}}""";
    private const string Trigger = """{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"}""";

    private static AbilityRunner Lower(string abilities) =>
        new(AbilityCatalog.Parse($$"""{"cards":[{"card":"16101","abilities":[{{abilities}}]}]}"""));

    [Fact]
    public void TheRevealedCardNeedsAnEffectThatReveals()
    {
        var failure = Assert.Throws<AbilityException>(() => Lower(
            """{"trigger":""" + Trigger + ""","effect":{"resolveSpecials":{"cards":"revealed"}}}"""));
        Assert.Contains("revealed requires an effect that reveals a card", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheRevealedCardIsNotReadBeforeTheEffect()
    {
        var failure = Assert.Throws<AbilityException>(() => Lower(
            """{"trigger":""" + Trigger + ""","when":{"exists":"revealed"},"effect":{"seq":[""" + Reveal + """,{"resolveSpecials":{"cards":"revealed"}}]}}"""));
        Assert.Contains("revealed is read only by an effect", failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"resolveSpecials":{"cards":"attachedThis"}}""", "attachedThis requires an effect that attaches this card")]
    [InlineData("""{"seq":[{"attachCards":{"cards":{"titled":"Wide Stance"},"host":{"titled":"Nebula"}}},{"resolveSpecials":{"cards":"attachedThis"}}]}""",
        "attachedThis requires an effect that attaches this card")]
    public void TheAttachedCardNeedsAnEffectThatAttachesThisCard(string effect, string message)
    {
        var failure = Assert.Throws<AbilityException>(() => Lower(
            """{"trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},"effect":""" + effect + "}"));
        Assert.Contains(message, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheAttachedCardFollowsAnAttachmentOfThisCard()
    {
        Assert.Null(Record.Exception(() => Lower(
            """{"trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},"effect":{"seq":[{"attachCards":{"cards":"this","host":{"titled":"Nebula"}}},{"resolveSpecials":{"cards":"attachedThis"}}]}}""")));
    }

    [Fact]
    public void ACounterNamedRevealedIsNotTheRevealedCard()
    {
        Assert.Null(Record.Exception(() => Lower(
            """{"trigger":""" + Trigger + ""","effect":{"placeCounters":{"card":"you","counter":"revealed","count":1}}}""")));
    }

    [Fact]
    public void TheRevealedCardFollowsADiscardUntilReveal()
    {
        Assert.Null(Record.Exception(() => Lower(
            """{"trigger":""" + Trigger + ""","effect":{"seq":[""" + Reveal + """,{"resolveSpecials":{"cards":"revealed"}}]}}""")));
    }

    [Fact]
    [Rule("rr:when-revealed-abilities")]
    public void AWhenRevealedAbilityTestsItsConditionInsideItsEffect()
    {
        // "When a player reveals a card ... all 'When Revealed' abilities on the card resolve."
        var failure = Assert.Throws<AbilityException>(() => Lower(
            """{"trigger":""" + Trigger + ""","when":{"markedThisRound":"Technique"},"effect":{"gainSurge":1}}"""));
        Assert.Contains("a When Revealed ability tests a condition with if", failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"markThisRound":1}""")]
    [InlineData("""{"if":{"test":{"markedThisRound":2},"then":{"gainSurge":1}}}""")]
    public void ARoundMarkIsNamedByAWord(string effect)
    {
        Assert.Throws<AbilityException>(() => Lower("""{"trigger":""" + Trigger + ""","effect":""" + effect + "}"));
    }

    [Fact]
    public void ARoundMarkHoldsUntilTheRoundEndsAndNamesOneFact()
    {
        var world = NebulaBoards.Board();

        Assert.False(RoundMarks.Has(world, "Technique"));
        RoundMarks.Record(world, "Technique");
        RoundMarks.Record(world, "Technique");

        Assert.True(RoundMarks.Has(world, "Technique"));
        Assert.False(RoundMarks.Has(world, "Other"));
        Assert.Single(world.Effects.Active(), effect => effect.Kind == "roundMark:Technique");
        world.Effects.Expire(TimingPoints.EndOfPhase);
        Assert.True(RoundMarks.Has(world, "Technique"));
        world.Effects.Expire(TimingPoints.EndOfRound);
        Assert.False(RoundMarks.Has(world, "Technique"));
    }

    [Fact]
    [Rule("rr:cannot")]
    public void TheSmallestAttackDamageMaximumApplies()
    {
        // Cutthroat Ambition's 5 beside a synthetic constant maximum of 3 on the villain.
        var world = NebulaBoards.Board();
        var nebula = GrootBoards.Villain(world);
        NebulaBoards.Technique(world, NebulaBoards.Cutthroat);
        Assert.Equal(5, AttackDamageMaximum.Amount(world, nebula));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, AttackDamageMaximum.Field, 3,
            Affects: nebula.ObjectId));

        Assert.Equal(3, AttackDamageMaximum.Amount(world, nebula));
        // A base definition of the field is not a maximum.
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, AttackDamageMaximum.Field, 1,
            Affects: nebula.ObjectId) { ValueRole = ContinuousValueRole.BaseDefinition });
        Assert.Equal(3, AttackDamageMaximum.Amount(world, nebula));
        Assert.Null(AttackDamageMaximum.Amount(world, world.Seats[0].IdentityCard));
        Assert.True(StateFields.IsModifiable(AttackDamageMaximum.Field));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:choose-option.1")]
    public void AnOptionThatDiscardsEachSelectedCardNeedsOneInPlay(bool attached)
    {
        // A synthetic Milano action choosing between discarding each attachment in the
        // villain's play area and drawing a card.
        var world = NebulaBoards.Board();
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(
            """{"cards":[{"card":"16142","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},"effect":{"choose":{"descriptions":["Discard each attachment","Draw a card"],"options":[{"discardEach":{"cardsIn":{"area":"villainPlayArea","kind":"Attachment"}}},{"draw":{"player":"you","count":1}}]}}}]}]}"""));
        var milano = ShipCommandBoards.PutMilano(world);
        if (attached) NebulaBoards.Technique(world, NebulaBoards.WideStance);
        var action = Assert.Single(ShipCommandBoards.ActionsOn(world, milano, 0));
        world.Agenda.AddPlayerAction(0, new PlayerAction(action, [], []));

        var asked = NebulaBoards.Choose(world, "Draw a card");

        Assert.Equal(attached, asked.Any(prompt => prompt.Affordances.Any(a =>
            (a.Description ?? a.Label).Contains("Discard each attachment", StringComparison.Ordinal))));
    }

    [Fact]
    [Rule("rr:player-deck.1")]
    public void RemovingTheTopCardIsNotOfferedFromAnEmptyDeck()
    {
        // Nebula (III)'s optional removal needs a card on top of the attacked player's deck.
        var world = NebulaBoards.Board(villain: NebulaBoards.NebulaThree);
        var mastery = NebulaBoards.Technique(world, NebulaBoards.WeaponMastery);
        foreach (var card in world.Seats[0].Deck.Cards.ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: false, 0);
        var asked = NebulaBoards.Choose(world, "Remove the top card");

        Assert.DoesNotContain(asked, prompt => prompt.Affordances.Any(a =>
            (a.Description ?? a.Label).Contains("Remove the top card", StringComparison.Ordinal)));
        Assert.Equal(GrootBoards.Villain(world).ObjectId, mastery.Area.Host);
    }
}
