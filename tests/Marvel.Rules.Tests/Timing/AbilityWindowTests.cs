using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Timing;

/// <summary>
/// The order abilities act in around one occurrence.
/// </summary>
/// <remarks>
/// Every one of these is a claim about the published rules and nothing else —
/// no recorded game reaches an interrupt window, because the milestone game's
/// hero never leaves alter-ego form and declines every option. So the citations
/// are the whole of the evidence here, and each test carries the clause it
/// stands on.
/// </remarks>
public sealed class AbilityWindowTests
{
    private static PendingAbility An(AbilityType type, int card = 1, int player = 0) =>
        new(card, type, player);

    [Rule("rr:ability.step.1")]
    [Rule("rr:ability.step.2.a")]
    [Rule("rr:ability.step.2.b")]
    [Rule("rr:ability.step.2.c")]
    [Rule("rr:ability.step.3")]
    [Rule("rr:ability.step.4.a")]
    [Rule("rr:ability.step.4.b")]
    [Rule("rr:ability.step.5")]
    [Fact]
    public void TheTiersAreTheOnesTheRulebookLists()
    {
        // The list at the head of `rr:ability`, in order. Written out as one
        // assertion so that reordering the enum -- which is what every other
        // test in this file rests on -- fails here first and obviously.
        Assert.Equal(
            [
                TimingPriority.Continuous,
                TimingPriority.StatusForcedInterrupt,
                TimingPriority.ForcedInterrupt,
                TimingPriority.Interrupt,
                TimingPriority.Occurrence,
                TimingPriority.ForcedResponse,
                TimingPriority.Response,
                TimingPriority.ConsequentialDamage,
            ],
            Enum.GetValues<TimingPriority>()
                .Where(priority => priority != TimingPriority.Untimed)
                .OrderBy(priority => priority));
    }

    [Rule("rr:ability.step.2.a")]
    [Fact]
    public void AStatusCardsForcedInterruptGoesBeforeAnOrdinaryOne()
    {
        // "Status card abilities have timing priority over all conflicting
        // triggered abilities." Stun, Confuse and Tough are status cards, and
        // this tier is why they beat whatever else wants the same window.
        // Collapsing 2a into 2b would let an ordinary forced interrupt resolve
        // first and cancel the attack the status card was there to change.
        var tiers = AbilityWindow.Tiers(
            [An(AbilityType.ForcedInterrupt, card: 1), An(AbilityType.StatusForcedInterrupt, card: 2)],
            WindowKind.Interrupt,
            new Occurrence(1, "WhenAttacked"));

        Assert.Equal(
            [TimingPriority.StatusForcedInterrupt, TimingPriority.ForcedInterrupt],
            tiers.Select(tier => tier.Priority));
    }

    [Rule("rr:forced.4")]
    [Fact]
    public void AForcedInterruptGoesBeforeAnOptionalOneWhoeverControlsIt()
    {
        // "Forced interrupts take priority and initiate before non-forced
        // interrupts." Ordering is by tier and not by player: the last player's
        // forced interrupt still goes ahead of the first player's optional one.
        var tiers = AbilityWindow.Tiers(
            [An(AbilityType.Interrupt, card: 1, player: 0),
             An(AbilityType.ForcedInterrupt, card: 2, player: 3)],
            WindowKind.Interrupt,
            new Occurrence(1, "WhenAttacked"));

        Assert.Equal(
            [TimingPriority.ForcedInterrupt, TimingPriority.Interrupt],
            tiers.Select(tier => tier.Priority));
    }

    [Rule("rr:forced.5")]
    [Rule("rr:simultaneous-resolution")]
    [Fact]
    public void TwoAbilitiesInOneTierAreLeftForTheFirstPlayerToOrder()
    {
        // "If two or more forced abilities would initiate at the same moment,
        // the first player determines the order... regardless of who controls
        // the cards." So this is a decision, and returning a sorted list would
        // be answering it -- by object id, which is not a rule.
        var tiers = AbilityWindow.Tiers(
            [An(AbilityType.ForcedInterrupt, card: 7, player: 1),
             An(AbilityType.ForcedInterrupt, card: 3, player: 0)],
            WindowKind.Interrupt,
            new Occurrence(1, "WhenAttacked"));

        var tier = Assert.Single(tiers);
        Assert.Equal(2, tier.Abilities.Count);
    }

    [Rule("rr:interrupt.3")]
    [Fact]
    public void AnInterruptDoesNotAppearInTheResponseWindow()
    {
        // "An interrupt ability is resolved when its triggering condition
        // initiates, but before that triggering condition resolves." A window
        // holds one kind, and the two are not interchangeable.
        var pending = new[] { An(AbilityType.Interrupt), An(AbilityType.Response) };
        var occurrence = new Occurrence(1, "WhenAttacked");

        Assert.Equal(
            [TimingPriority.Interrupt],
            AbilityWindow.Tiers(pending, WindowKind.Interrupt, occurrence)
                .Select(tier => tier.Priority));
        Assert.Equal(
            [TimingPriority.Response],
            AbilityWindow.Tiers(pending, WindowKind.Response, occurrence)
                .Select(tier => tier.Priority));
    }

    [Rule("rr:triggering-condition.1")]
    [Rule("rr:interrupt.2")]
    [Rule("rr:response.2")]
    [Fact]
    public void AnAbilityGetsOneTurnPerOccurrenceAndOnePerWindow()
    {
        // Once per occurrence of its triggering condition -- but the interrupt
        // window and the response window are different windows, so spending one
        // must not spend the other.
        var occurrence = new Occurrence(1, "WhenAttacked");
        var pending = new[] { An(AbilityType.Interrupt), An(AbilityType.Response) };

        Assert.True(occurrence.Trigger(WindowKind.Interrupt, card: 1));
        Assert.False(occurrence.Trigger(WindowKind.Interrupt, card: 1));

        Assert.Empty(AbilityWindow.Tiers(pending, WindowKind.Interrupt, occurrence));
        Assert.Single(AbilityWindow.Tiers(pending, WindowKind.Response, occurrence));
    }

    [Rule("rr:triggering-condition.1.1")]
    [Rule("rr:interrupt.2.1")]
    [Rule("rr:response.2.1")]
    [Theory]
    [InlineData(WindowKind.Interrupt, AbilityType.Interrupt)]
    [InlineData(WindowKind.Response, AbilityType.Response)]
    public void TwoCopiesOfACardEachGetATurn(WindowKind window, AbilityType type)
    {
        // "Multiple copies of a card with an interrupt or response can each be
        // triggered by the same triggering condition." Keyed on the card in
        // play, so a second copy is a second card and not a spent one.
        var occurrence = new Occurrence(1, "WhenAttacked");
        occurrence.Trigger(window, card: 1);

        var tier = Assert.Single(AbilityWindow.Tiers(
            [An(type, card: 1), An(type, card: 2)],
            window,
            occurrence));

        Assert.Equal([2], tier.Abilities.Select(ability => ability.Card));
    }

    [Rule("rr:triggering-condition.2")]
    [Fact]
    public void OneOccurrenceGetsOneWindowHoweverManyConditionsItCreates()
    {
        // "A single attack causing a character to both take damage and be
        // defeated" is one occurrence with two triggering conditions, and the
        // rules give it a single interrupt window and a single response window.
        // An engine opening a window per condition would let one interrupt fire
        // twice against what the rulebook calls one moment.
        var occurrence = new Occurrence(1, ["WhenDamaged", "WhenDefeated"]);
        Assert.Equal(2, occurrence.Conditions.Count);

        occurrence.Trigger(WindowKind.Interrupt, card: 1);
        Assert.False(occurrence.MayTrigger(WindowKind.Interrupt, card: 1));
    }

    [Rule("rr:ability.11")]
    [Rule("rr:ability.7")]
    [Rule("rr:ability.8")]
    [Fact]
    public void ForcedIsWhatMakesAnAbilityMandatory()
    {
        // `rr:ability.11` states the rule and the two lists follow it: the
        // mandatory types of `rr:ability.7` against the optional ones of
        // `rr:ability.8`.
        Assert.All(
            new[]
            {
                AbilityType.Constant, AbilityType.Keyword, AbilityType.Setup,
                AbilityType.WhenRevealed, AbilityType.WhenDefeated, AbilityType.WhenCompleted,
                AbilityType.ForcedAction, AbilityType.ForcedInterrupt,
                AbilityType.StatusForcedInterrupt, AbilityType.ForcedResponse, AbilityType.Boost,
            },
            type => Assert.True(AbilityTypes.IsMandatory(type), $"{type} should be mandatory"));

        Assert.All(
            new[]
            {
                AbilityType.Action, AbilityType.Interrupt,
                AbilityType.Response, AbilityType.Resource,
            },
            type => Assert.False(AbilityTypes.IsMandatory(type), $"{type} should be optional"));
    }

    [Rule("rr:ability.11")]
    [Fact]
    public void ATierSeparatesWhatIsResolvedFromWhatIsOffered()
    {
        var tiers = AbilityWindow.Tiers(
            [An(AbilityType.ForcedInterrupt, card: 1), An(AbilityType.Interrupt, card: 2)],
            WindowKind.Interrupt,
            new Occurrence(1, "WhenAttacked"));

        var (forcedMandatory, forcedOptional) = AbilityWindow.Split(tiers[0]);
        Assert.Single(forcedMandatory);
        Assert.Empty(forcedOptional);

        var (mandatory, optional) = AbilityWindow.Split(tiers[1]);
        Assert.Empty(mandatory);
        Assert.Single(optional);
    }

    [Rule("rr:when-defeated-abilities.1")]
    [Rule("rr:when-completed-abilities.1")]
    [Fact]
    public void WhenDefeatedAndWhenCompletedAreForcedInterrupts()
    {
        // The rulebook defines both as exactly "Forced Interrupt: When this
        // card is defeated..." / "...this scheme is completed...", which is
        // what makes `rr:when-defeated-abilities.2.1` work -- the card leaves
        // play *after* the ability resolves.
        //
        // Grouped with Boost and When Revealed instead -- one tier too late --
        // a villain's dying ability would resolve after it had already left
        // play. That is the mistake this pins against.
        Assert.Equal(TimingPriority.ForcedInterrupt, AbilityTypes.PriorityOf(AbilityType.WhenDefeated));
        Assert.Equal(TimingPriority.ForcedInterrupt, AbilityTypes.PriorityOf(AbilityType.WhenCompleted));

        var tiers = AbilityWindow.Tiers(
            [An(AbilityType.WhenDefeated, card: 1), An(AbilityType.Interrupt, card: 2)],
            WindowKind.Interrupt,
            new Occurrence(1, "WhenDefeated"));

        Assert.Equal(TimingPriority.ForcedInterrupt, tiers[0].Priority);
    }

    [Rule("rr:ability.step.3")]
    [Fact]
    public void BoostAndWhenRevealedAreTheOccurrenceRatherThanAWindow()
    {
        // Tier 3 is the occurrence itself. Neither belongs in an interrupt or a
        // response window, so neither is ever offered in one.
        Assert.Equal(TimingPriority.Occurrence, AbilityTypes.PriorityOf(AbilityType.Boost));
        Assert.Equal(TimingPriority.Occurrence, AbilityTypes.PriorityOf(AbilityType.WhenRevealed));

        var pending = new[] { An(AbilityType.Boost), An(AbilityType.WhenRevealed) };
        var occurrence = new Occurrence(1, "WhenRevealed");
        Assert.Empty(AbilityWindow.Tiers(pending, WindowKind.Interrupt, occurrence));
        Assert.Empty(AbilityWindow.Tiers(pending, WindowKind.Response, occurrence));
    }

    [Rule("rr:action")]
    [Rule("rr:resource-ability.1")]
    [Rule("rr:setup-triggered-ability")]
    [Fact]
    public void AnAbilityTimedToSomethingOtherThanAnOccurrenceHasNoTier()
    {
        // An action is taken during a player's turn, a resource ability while a
        // cost is being paid, a setup ability during setup. All three have a
        // bold trigger and none of them is on `rr:ability`'s list. Giving them
        // a tier anyway would put them in windows they do not belong in.
        Assert.All(
            new[]
            {
                AbilityType.Action, AbilityType.ForcedAction,
                AbilityType.Resource, AbilityType.Setup, AbilityType.Special,
            },
            type => Assert.Equal(TimingPriority.Untimed, AbilityTypes.PriorityOf(type)));
    }

    [Rule("rr:consequential-damage.1")]
    [Fact]
    public void ConsequentialDamageComesAfterTheResponses()
    {
        // "Consequential damage is dealt to an ally after resolving abilities
        // that are triggered by the ally attacking or thwarting." Tier 5, and
        // the rulebook says it twice -- once in the list and once here.
        Assert.True(TimingPriority.ConsequentialDamage > TimingPriority.Response);
        Assert.True(TimingPriority.ConsequentialDamage > TimingPriority.ForcedResponse);
    }
}
