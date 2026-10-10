using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreCorrectionChoiceTests : ChoosingCardsTestBase
{
    [Theory]
    [InlineData("black_panther", "01155")]
    [InlineData("spider_man", "01165")]
    [InlineData("she_hulk", "01160")]
    [InlineData("iron_man", "01170")]
    [InlineData("captain_marvel", "01175")]
    [Rule("rr:form-change-form.4")]
    [Rule("rr:cost.1")]
    public void ObligationsRequireAReadyNamedAlterEgo(string hero, string obligation)
    {
        // "While a player is in hero form, card abilities that interact with
        // their alter-ego do not interact with their identity." The printed
        // arrow distinguishes "pay cost resolve effect" (rr:cost.1).
        foreach (bool heroForm in new[] { false, true })
        foreach (bool exhausted in new[] { false, true })
        {
            World world = Deal(hero);
            Card identity = world.Seats[0].IdentityCard;
            if (heroForm) identity.TurnTo(identity.Faces.Single(face => Cards.Kind(face) == CardKind.Hero));
            if (exhausted) identity.Exhaust();
            world.CreateCard("01046", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            var runner = AuthoredCards.Runner();
            Card source = world.CreateCard(obligation, world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)));
            runner.WhenRevealed(world, source, 0);
            runner.Chose(world, source, 0, 1, Decision.Take(1));
            Prompt? choices = runner.Choosing(world, source, 0, 2);
            bool mayRemove = !heroForm && !exhausted;
            Assert.Equal(mayRemove, choices?.Affordances.Any(offer => offer.Id == 0) == true);
            if (mayRemove)
            {
                runner.Chose(world, source, 0, 2, Decision.Take(0));
                Assert.False(identity.Ready);
                Assert.Equal(DeckType.RemovedArea, source.Area.Type);
            }
            else
            {
                string before = world.Digest().Fingerprint();
                var rejected = Assert.Throws<RulesNotImplementedException>(() => runner.Chose(world, source, 0, 2, Decision.Take(0)));
                Assert.Contains("cannot choose illegal option 0", rejected.Message);
                Assert.Equal(before, world.Digest().Fingerprint());
                Assert.NotEqual(DeckType.RemovedArea, source.Area.Type);
            }
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [Rule("rr:form-change-form.5")]
    [Rule("rr:choose-option.1")]
    public void ElectricWhipCannotDamageAnAlterEgo(bool heroForm, bool upgradePresent)
    {
        // "While a player is in alter-ego form, card abilities that interact
        // with their hero do not interact with their identity."
        // An option cannot be chosen "if there are no valid targets for that option."
        World world = Deal("iron_man");
        if (heroForm) world.Seats[0].IdentityCard.TurnTo("01029a");
        Card? upgrade = upgradePresent ? world.CreateCard("01035",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0)) : null;
        Card source = world.CreateCard("01173", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();
        runner.WhenRevealed(world, source, 0);
        if (!upgradePresent)
        {
            Assert.DoesNotContain(world.Agenda.Outstanding, step => step.What == Steps.ChooseOption);
            Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
            return;
        }
        Prompt prompt = runner.Choosing(world, source, 0, 0)!;
        Assert.Equal(heroForm ? 2 : 1, prompt.Affordances.Count);
        Assert.Contains(prompt.Affordances, offer => offer.Id == 1);
        Assert.Equal(heroForm, prompt.Affordances.Any(offer => offer.Id == 0));
        runner.Chose(world, source, 0, 0, Decision.Take(heroForm ? 0 : 1));
        if (heroForm) Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        else
        {
            PhaseStep choice = world.Agenda.Outstanding.Last(step => step.What == Steps.ChooseOption);
            Prompt pick = runner.Choosing(world, source, 0, choice.Index, choice.Tier)!;
            runner.Chose(world, source, 0, choice.Index, Decision.Take(Assert.Single(pick.Affordances).Id), choice.Tier);
            Assert.Equal(DeckType.DiscardPile, upgrade!.Area.Type);
            Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        }
    }

    [Fact]
    [Rule("rr:form-change-form.5")]
    public void RitualCombatOffersOnlyThreatToAnAlterEgo()
    {
        World world = Deal();
        var runner = AuthoredCards.Runner();
        Card source = world.CreateCard("01159", world.AreaOf(DeckType.RevealingArea));
        world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        runner.WhenRevealed(world, source, 0);
        PhaseStep step = world.Agenda.Outstanding.Last(item => item.What == Steps.ChooseOption);
        Prompt prompt = runner.Choosing(world, source, 0, step.Index, step.Tier)!;
        Assert.Equal(1, Assert.Single(prompt.Affordances).Id);
        runner.Chose(world, source, 0, step.Index, Decision.Take(1), step.Tier);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        // "Abilities that interact with their hero do not interact with their identity."
        Assert.Contains(world.Agenda.Outstanding, item => item.What == Steps.PlaceThreatEffect);
    }

    [Fact]
    [Rule("rr:friendly")]
    public void GetReadyCanReadyAnotherPlayersAlly()
    {
        // "Friendly is a blanket term that refers to cards the players control."
        World world = Deal("iron_man", "spider_man");
        Card ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        ally.Exhaust();
        Card source = world.CreateCard("01069", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        runner.Act(world, action, [], []);
        PhaseStep step = world.Agenda.Outstanding.Last(item => item.What == Steps.ChooseOption);
        Prompt prompt = runner.Choosing(world, source, 0, step.Index, step.Tier)!;
        Assert.Equal(ally.ObjectId, Assert.Single(prompt.Affordances).AnchorId);
        runner.Chose(world, source, 0, step.Index, Decision.Take(prompt.Affordances[0].Id), step.Tier);
        Assert.True(ally.Ready);
    }

    [Fact]
    [Rule("rr:form-change-form.5")]
    public void ExplosionExcludesAlterEgosButIncludesOtherPlayersAllies()
    {
        World world = Deal("iron_man", "spider_man");
        world.Seats[1].IdentityCard.TurnTo("01001a");
        Card ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Card bomb = world.CreateCard("01109", world.AreaOf(DeckType.SideSchemesArea));
        bomb.PlaceTokens("k_threat", 3);
        Card source = world.CreateCard("01111", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();
        runner.WhenRevealed(world, source, 0);
        Prompt prompt = runner.Choosing(world, source, 0, 0)!;
        int[] legal = [.. Assert.Single(prompt.Affordances).Targets!.Legal];
        // In alter-ego form, "card abilities that interact with their hero do not interact with their identity."
        Assert.DoesNotContain(world.Seats[0].IdentityCard.ObjectId, legal);
        Assert.Contains(world.Seats[1].IdentityCard.ObjectId, legal);
        Assert.Contains(ally.ObjectId, legal);
    }
    [Fact]
    [Rule("rr:form-change-form.5")]
    public void ExplosionWithNoHeroesOrAlliesResolvesWithoutADamageChoice()
    {
        // In alter-ego form, "card abilities that interact with their hero do not interact with their identity."
        World world = Deal("iron_man", "spider_man");
        Card bomb = world.CreateCard("01109", world.AreaOf(DeckType.SideSchemesArea));
        bomb.PlaceTokens("k_threat", 3);
        Card source = world.CreateCard("01111", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        runner.WhenRevealed(world, source, 0);
        Assert.Null(Sequence.Work(world, Cards, runner, []));
        Assert.All(world.Seats, seat => Assert.Equal(0, seat.IdentityCard.Damage));
    }

    [Fact]
    [Rule("rr:form-change-form.5")]
    [Rule("rr:each-player.1")]
    public void UnderAttackRequiresThreatForEachAlterEgo()
    {
        // "The first player decides the order." In alter-ego form, "card abilities
        // that interact with their hero do not interact with their identity."
        World world = Deal("iron_man", "spider_man");
        Card source = world.CreateCard("01151", world.AreaOf(DeckType.SideSchemesArea));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        runner.WhenRevealed(world, source, 0);
        var events = new List<Marvel.Rules.Events.GameEvent>();
        for (int count = 0; count < 6; count++)
        {
            Prompt? prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) break;
            var offer = Assert.Single(prompt.Affordances);
            Sequence.Answer(world, Cards, runner, prompt, Decision.Take(offer.Id, prompt.Asking == Question.Order ? offer.Targets!.Legal : [], []), events);
        }
        Assert.Null(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(4, source.Tokens["k_threat"]);
        Assert.All(world.Seats, seat => Assert.Equal(0, seat.IdentityCard.Damage));
    }

    [Fact]
    [Rule("rr:friendly")]
    public void FirstAidStillTreatsAnAlterEgoAsAFriendlyCharacter()
    {
        // "Friendly is a blanket term that refers to cards the players control."
        World world = Deal();
        world.Seats[0].IdentityCard.TakeDamage(2);
        Card source = world.CreateCard("01086", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        var action = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        runner.Act(world, action, [world.Seats[0].Hand.Cards.First(card => card != source).ObjectId], []);
        PhaseStep step = world.Agenda.Outstanding.Last(item => item.What == Steps.ChooseOption);
        Prompt prompt = runner.Choosing(world, source, 0, step.Index, step.Tier)!;
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, Assert.Single(prompt.Affordances).AnchorId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:form-change-form.3")]
    [Rule("rr:choose-option.1")]
    public void AffairsMayFlipBeforeCheckingTheRemovalPrerequisite(bool flip)
    {
        World world = Deal("black_panther");
        world.Seats[0].IdentityCard.TurnTo("01040a");
        Card source = world.CreateCard("01155", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)));
        var runner = AuthoredCards.Runner();
        runner.WhenRevealed(world, source, 0);
        Prompt prompt = runner.Choosing(world, source, 0, 1, Marvel.Rules.Timing.AbilityType.WhenRevealed)!;
        Assert.Equal(2, prompt.Affordances.Count);
        // "If a card ability causes a player to change forms, it does not count
        // against the one voluntary form change"; the choice precedes its cost.
        // An option cannot be chosen "if there are no valid targets for that option."
        runner.Chose(world, source, 0, 1, Decision.Take(flip ? 0 : 1), Marvel.Rules.Timing.AbilityType.WhenRevealed);
        if (flip)
        {
            Prompt removal = runner.Choosing(world, source, 0, 2, Marvel.Rules.Timing.AbilityType.WhenRevealed)!;
            Assert.Equal(0, Assert.Single(removal.Affordances).Id);
            runner.Chose(world, source, 0, 2, Decision.Take(0), Marvel.Rules.Timing.AbilityType.WhenRevealed);
            Assert.Equal(DeckType.RemovedArea, source.Area.Type);
        }
        else
        {
            Assert.Equal("01040a", world.Seats[0].IdentityCard.FaceId);
            Assert.NotEqual(DeckType.RemovedArea, source.Area.Type);
        }
    }

}
