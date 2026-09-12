using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public sealed class ChoosingCardsHydraBomberAsksRatherThanDecidingTests : ChoosingCardsTestBase
{
    [Rule("rr:choose-option")]
    [Rule("rr:ability.4")]
    [Fact]
    public void HydraBomberAsksRatherThanDeciding()
    {
        // Nothing has happened yet when the ability returns. Both of its
        // options change the board, and neither has: what the reveal produced
        // is a question.
        var world = Deal();
        var identity = world.Seats[0].IdentityCard;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var(card, events) = Reveal(world, AuthoredCards.HydraBomber);
        Assert.Empty(events);
        Assert.Equal(0, identity.Damage);
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("k_threat"));
        var waiting = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.ChooseOption, waiting.What);
        Assert.Equal(card.ObjectId, waiting.Subject);
    }

    [Rule("rr:boost-boost-icon.2")]
    [Fact]
    public void ACardWithAChoiceInTwoAbilitiesResumesTheOneThatStopped()
    {
        // `rr:boost-boost-icon.2` keeps a card's "Boost" and its "When Revealed"
        // apart, and a card can put a choice in each. The card and the index the
        // ability stopped at do not say *which* ability, so the suspended step
        // carries the tier too -- without it, resuming a boost asks the
        // reveal's question.
        //
        // That failure is silent and legal-looking: two real options about the
        // wrong thing.
        var world = Deal();
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [ { "card": "01110", "abilities": [
              { "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
                "effect": { "choose": { "options": [
                    { "gainSurge": 1 }, { "discard": "this" } ] } } },
              { "trigger": { "event": "WhenCardRevealed", "timing": "Boost",
                             "subject": "this" },
                "effect": { "choose": { "options": [
                    { "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } },
                    { "dealDamage": { "cards": "you", "amount": 1 } } ] } } }
            ] } ] }
            """));
        var card = world.CreateCard("01110", world.AreaOf(DeckType.RevealingArea));
        runner.Boost(world, card, 0);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(AbilityType.Boost, waiting.Tier);
        var asked = runner.Choosing(world, card, 0, waiting.Index, waiting.Tier)!;
        Assert.Equal(["placeThreat", "dealDamage"], asked.Affordances.Select(option => option.Label));
    }

    [Rule("rr:choose-option")]
    [Fact]
    public void TwoAbilitiesAtOneTierWithAChoiceInEachAreRefused()
    {
        // The tier is as fine as the step gets, so this is the next thing it
        // would have to carry rather than something to guess at. No printed card
        // needs it; the refusal names what is missing.
        var world = Deal();
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [ { "card": "01110", "abilities": [
              { "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
                "effect": { "choose": { "options": [
                    { "gainSurge": 1 }, { "discard": "this" } ] } } },
              { "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
                "effect": { "choose": { "options": [
                    { "gainSurge": 2 }, { "discard": "this" } ] } } }
            ] } ] }
            """));
        var card = world.CreateCard("01110", world.AreaOf(DeckType.RevealingArea));
        var refused = Assert.Throws<RulesNotImplementedException>(() => runner.Choosing(world, card, 0, 1, AbilityType.WhenRevealed));
        Assert.Contains("choice in more than one 'WhenRevealed' ability", refused.Message, StringComparison.Ordinal);
    }

    [Rule("rr:choose-game-element.1")]
    [Fact]
    public void TheQuestionGoesToThePlayerResolvingTheCard()
    {
        // "The player resolving the ability", which for a revealed encounter
        // card is the player it was dealt to -- not the first player, and not
        // the card's owner, which an encounter card has not got. Revealed by
        // the second player of two so that the claim can be wrong.
        var world = Deal("spider_man", "she_hulk");
        var(card, _) = Reveal(world, AuthoredCards.HydraBomber, player: 1);
        var asked = AuthoredCards.Runner().Choosing(world, card, player: 1, stoppedAt: 1)!;
        Assert.Equal(1, asked.Player);
        Assert.Equal(Question.Option, asked.Asking);
        // Two options, and no way out of them: `rr:choose-option` offers a
        // choice between things that happen, not a chance to decline.
        Assert.Equal(2, asked.Affordances.Count);
        Assert.False(asked.Cancellable);
        Assert.Equal(["dealDamage", "placeThreat"], asked.Affordances.Select(a => a.Label));
    }

    [Rule("rr:choose-option.1")]
    [Fact]
    public void AnEncounterCardDoesNotOfferAnOptionWithNoValidTarget()
    {
        // "When an encounter card requires a player to choose an option, they
        // cannot choose an option that requires one or more targets if there
        // are no valid targets for that option." Electric Whip Attack's core
        // shape is damage or choose and discard an upgrade. With no upgrades,
        // only the damage branch is a legal answer.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [ { "card": "01173", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                           "subject": "this" },
              "effect": { "choose": { "options": [
                { "dealDamage": { "cards": "yourHero", "amount": 1 } },
                { "chooseCard": {
                    "from": { "query": "upgradesYouControl" },
                    "effect": { "discard": "chosen" } } }
              ] } }
            } ] } ] }
            """));
        var world = Deal();
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var card = world.CreateCard("01173", world.AreaOf(DeckType.RevealingArea));
        runner.WhenRevealed(world, card, 0);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        var asked = runner.Choosing(world, card, 0, waiting.Index, waiting.Tier)!;
        var damage = Assert.Single(asked.Affordances);
        Assert.Equal(0, damage.Id);
        Assert.Equal("dealDamage", damage.Label);
    }

    [Rule("rr:target.2")]
    [Rule("rr:choose-option.1")]
    [Fact]
    public void AWhenRevealedChoiceWithNoLegalOptionDoesNotInitiate()
    {
        // Electric Whip Attack computes zero damage with no controlled
        // upgrades, and its discard branch has no valid target. Neither option
        // can initiate, so the mandatory When Revealed ability resolves with
        // no effect and asks no impossible question.
        var world = Deal("iron_man");
        world.Seats[0].IdentityCard.TurnTo("01029a");
        var card = world.CreateCard("01173", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var events = runner.WhenRevealed(world, card, player: 0);
        Assert.Empty(events);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:choose-option.2")]
    [Fact]
    public void APlayerCardDoesNotOfferAnOptionThatCannotPartiallyResolve()
    {
        // A player-card option "cannot be chosen if it cannot be at least
        // partially resolved," including one that "require[s] one or more
        // targets" when none is valid. Nick Fury's core choice includes
        // removing 2 threat; at zero threat that option cannot resolve at all.
        var runner = NickFuryRunner();
        var world = Deal();
        var fury = world.CreateCard("01084", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        runner.WhenRevealed(world, fury, 0);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        var asked = runner.Choosing(world, fury, 0, waiting.Index, waiting.Tier)!;
        var draw = Assert.Single(asked.Affordances);
        Assert.Equal(1, draw.Id);
        Assert.Equal("draw", draw.Label);
        Assert.Equal("Draw 3 cards", draw.Description);
        // Prompt filtering is not the authority boundary: a forged answer for
        // the unavailable original index is refused too.
        Assert.Throws<RulesNotImplementedException>(() => runner.Chose(world, fury, 0, waiting.Index, Decision.Take(0), waiting.Tier));
    }

    [Rule("rr:choose-option.2")]
    [Rule("rr:choose-option.2.2")]
    [Rule("rr:crisis-icon.1")]
    [Fact]
    public void APlayerCardCannotChooseMainSchemeThreatThroughCrisis()
    {
        // A player-card option requiring targets cannot be chosen when none
        // are valid. Crisis makes the main scheme an invalid threat-removal
        // target even while it holds threat.
        var runner = NickFuryRunner();
        var world = Deal();
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        world.CreateCard("01108", world.AreaOf(DeckType.SideSchemesArea));
        var fury = world.CreateCard("01084", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        runner.WhenRevealed(world, fury, 0);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        var asked = runner.Choosing(world, fury, 0, waiting.Index, waiting.Tier)!;
        Assert.Equal([1], asked.Affordances.Select(option => option.Id));
        Assert.Throws<RulesNotImplementedException>(() => runner.Chose(world, fury, 0, waiting.Index, Decision.Take(0), waiting.Tier));
    }

    [Rule("rr:crisis-icon.1")]
    [Fact]
    public void APlayerCardEffectRemovesNoMainSchemeThreatThroughCrisis()
    {
        // The choice gate and the effect resolver state the same restriction.
        // A player-card effect reached without a choice must not bypass crisis.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [ { "card": "01084", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                           "subject": "this" },
              "effect": { "removeThreat": {
                "scheme": { "query": "mainScheme" }, "amount": 2 } }
            } ] } ] }
            """));
        var world = Deal();
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 2);
        world.CreateCard("01108", world.AreaOf(DeckType.SideSchemesArea));
        var fury = world.CreateCard("01084", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        runner.WhenRevealed(world, fury, 0);
        Assert.Equal(2, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:crisis-icon.2")]
    [Fact]
    public void AnEncounterCardCanRemoveMainSchemeThreatThroughCrisis()
    {
        // "Abilities on encounter cards are not affected by the crisis icon."
        // The same removal a player card cannot perform remains legal when a
        // treachery resolves it.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [ { "card": "01110", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                           "subject": "this" },
              "effect": { "removeThreat": {
                "scheme": { "query": "mainScheme" }, "amount": 2 } }
            } ] } ] }
            """));
        var world = Deal();
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 2);
        world.CreateCard("01108", world.AreaOf(DeckType.SideSchemesArea));
        var treachery = world.CreateCard("01110", world.AreaOf(DeckType.RevealingArea));
        runner.WhenRevealed(world, treachery, 0);
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:choose-option.2")]
    [Rule("rr:draw-drawing-cards")]
    [Fact]
    public void APlayerCardCannotChooseToDrawFromNoCards()
    {
        // With both deck and discard empty, drawing changes nothing. The draw
        // branch cannot be partially resolved, while damaging the villain can.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [ { "card": "01084", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                           "subject": "this" },
              "effect": { "choose": { "options": [
                { "draw": { "player": "you", "count": 3 } },
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 4 } }
              ] } }
            } ] } ] }
            """));
        var world = Deal();
        var removed = world.AreaOf(DeckType.RemovedArea);
        foreach (var card in world.Seats[0].Deck.Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }

        foreach (var card in world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)).Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }

        var fury = world.CreateCard("01084", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        runner.WhenRevealed(world, fury, 0);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        var asked = runner.Choosing(world, fury, 0, waiting.Index, waiting.Tier)!;
        Assert.Equal([1], asked.Affordances.Select(option => option.Id));
    }

    [Rule("rr:choose-option.2")]
    [Fact]
    public void APlayerCardCanChooseASequenceThatPartiallyResolves()
    {
        // "At least partially resolved" is deliberately weaker than "every
        // effect resolves." The scheme has no threat, so the first step does
        // nothing, but drawing one card makes the option legal as a whole.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [ { "card": "01084", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                           "subject": "this" },
              "effect": { "choose": { "options": [
                { "seq": [
                    { "removeThreat": {
                        "scheme": { "query": "mainScheme" }, "amount": 2 } },
                    { "draw": { "player": "you", "count": 1 } }
                ] },
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }
              ] } }
            } ] } ] }
            """));
        var world = Deal();
        var fury = world.CreateCard("01084", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        runner.WhenRevealed(world, fury, 0);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        var asked = runner.Choosing(world, fury, 0, waiting.Index, waiting.Tier)!;
        Assert.Equal([0, 1], asked.Affordances.Select(option => option.Id));
    }
}
