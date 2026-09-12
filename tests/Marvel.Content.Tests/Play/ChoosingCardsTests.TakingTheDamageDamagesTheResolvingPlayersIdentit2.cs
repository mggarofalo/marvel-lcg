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
public sealed class ChoosingCardsTakingTheDamageDamagesTheResolvingPlayersIdentitTests : ChoosingCardsTestBase
{
    [Rule("rr:you-your.2")]
    [Fact]
    public void TakingTheDamageDamagesTheResolvingPlayersIdentity()
    {
        // `rr:you-your.2`: "if a card deals damage to 'you' [...] the player
        // resolving that damage applies it to the hit point dial of their
        // identity." Second player again, and the first is left alone.
        var world = Deal("spider_man", "she_hulk");
        var(card, _) = Reveal(world, AuthoredCards.HydraBomber, player: 1);
        AuthoredCards.Runner().Chose(world, card, 1, 1, Decision.Take(0));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(2, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void TakingTheThreatPlacesItOnTheMainScheme()
    {
        // The other branch, and the assertion that matters is the one about the
        // branch *not* taken: an interpreter that ran both would pass every
        // test above.
        var world = Deal();
        Reveal(world, AuthoredCards.HydraBomber);
        var abilities = AuthoredCards.Runner();
        var asked = Sequence.Work(world, Cards, abilities, [])!;
        Sequence.Answer(world, Cards, abilities, asked, Decision.Take(1), []);
        Sequence.Finish(world, Cards, abilities, []);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void AnAnswerThatNamesNoOptionIsRefusedByName()
    {
        // The prompt is not cancellable and the options are numbered, so both
        // a decline and a number outside the list are errors rather than a
        // silently skipped ability.
        var world = Deal();
        var(card, _) = Reveal(world, AuthoredCards.HydraBomber);
        var runner = AuthoredCards.Runner();
        Assert.Throws<RulesNotImplementedException>(() => runner.Chose(world, card, 0, 1, Decision.Decline));
        Assert.Throws<RulesNotImplementedException>(() => runner.Chose(world, card, 0, 1, Decision.Take(2)));
    }

    [Rule("rr:surge.2")]
    [Fact]
    public void TheGameAsksTheQuestionAndTheAnswerResolvesIt()
    {
        // The whole path, through the engine rather than through the runner:
        // the reveal step finishes, the game stops on a prompt nobody wrote
        // into the villain phase, and answering it does the thing.
        //
        // What makes this worth its own test is the *order*. `rr:surge.2` --
        // finish resolving the current card first -- is what `Agenda.Then`
        // gives for free, and an ability that asked inline would have had to
        // stop in the middle of the reveal.
        var world = Deal();
        var abilities = AuthoredCards.Runner();
        var events = new List<Marvel.Rules.Events.GameEvent>();
        var card = world.CreateCard(AuthoredCards.HydraBomber, world.AreaOf(DeckType.RevealingArea));
        world.Agenda.Add(new PhaseStep(Steps.RevealEncounterCard, 1, 4, Subject: card.ObjectId, Seat: 0));
        var asked = Sequence.Work(world, Cards, abilities, events);
        Assert.NotNull(asked);
        Assert.Equal(Question.Option, asked.Asking);
        var threat = asked.Affordances.Single(option => option.Label == "placeThreat");
        Sequence.Answer(world, Cards, abilities, asked, Decision.Take(threat.Id), events);
        Sequence.Work(world, Cards, abilities, events);
        Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        // And the agenda emptied: the choice was answered and nothing is left
        // suspended behind it.
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:choose-option")]
    [Fact]
    public void AnEffectAfterAChoiceWaitsForTheAnswer()
    {
        // **An ability can ask more than once, and what follows a question
        // waits for it.** An effect written after a `choose` has to run after
        // the choice, which means the ability resumes where it stopped rather
        // than restarting or abandoning.
        //
        // A suspended ability remembers where as an index into its top-level
        // sequence -- one number, which is what a `PhaseStep` can carry and
        // what survives a save.
        var book = Marvel.Cards.Dsl.AbilityCatalog.Parse("""
            {"cards":[{"card":"01110","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"seq":[
                {"choose":{"options":[{"draw":{"player":"you","count":1}},
                                      {"draw":{"player":"you","count":2}}]}},
                {"giveStatus":{"card":"you","status":"stunned"}}]}}]}]}
            """);
        var runner = new Marvel.Cards.Run.AbilityRunner(book);
        var world = Deal();
        world.Abilities = runner;
        var identity = world.Seats[0].IdentityCard;
        var card = world.CreateCard(AuthoredCards.HydraBomber, world.AreaOf(DeckType.RevealingArea));
        int held = world.Seats[0].Hand.Cards.Count;
        runner.WhenRevealed(world, card, 0);
        // The question is out and the step after it has not happened.
        var waiting = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.ChooseOption, waiting.What);
        Assert.False(Statuses.Has(world, identity, Statuses.Stunned));
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        runner.Chose(world, card, 0, waiting.Index, Decision.Take(1));
        // The option ran, and then the rest of the sequence did.
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
        Assert.True(Statuses.Has(world, identity, Statuses.Stunned));
    }

    [Fact]
    public void ACardCanAskTwice()
    {
        // Eviction Notice's shape: "you may flip to alter-ego form" and then
        // "choose:". 36 cards in the pool pair a "may" with a listed choice,
        // and every "may" is itself a question.
        var book = Marvel.Cards.Dsl.AbilityCatalog.Parse("""
            {"cards":[{"card":"01110","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"seq":[
                {"choose":{"options":[{"draw":{"player":"you","count":1}},
                                      {"seq":[]}]}},
                {"choose":{"options":[{"giveStatus":{"card":"you","status":"stunned"}},
                                      {"giveStatus":{"card":"you","status":"confused"}}]}}]}}]}]}
            """);
        var runner = new Marvel.Cards.Run.AbilityRunner(book);
        var world = Deal();
        world.Abilities = runner;
        var identity = world.Seats[0].IdentityCard;
        var card = world.CreateCard(AuthoredCards.HydraBomber, world.AreaOf(DeckType.RevealingArea));
        int held = world.Seats[0].Hand.Cards.Count;
        runner.WhenRevealed(world, card, 0);
        var first = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(1, first.Index);
        // Answering the first asks the second, at the next index.
        runner.Chose(world, card, 0, first.Index, Decision.Take(0));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        var second = world.Agenda.Outstanding[^1];
        Assert.Equal(Steps.ChooseOption, second.What);
        Assert.Equal(2, second.Index);
        runner.Chose(world, card, 0, second.Index, Decision.Take(1));
        Assert.True(Statuses.Has(world, identity, Statuses.Confused));
        Assert.False(Statuses.Has(world, identity, Statuses.Stunned));
    }

    [Rule("rr:choose-option")]
    [Fact]
    public void SameTimingChoicesResumeTheirExactAuthoredAbility()
    {
        // The ordinal is save data chosen by the engine. The rule requires
        // each ability's choice to resolve; it does not define how a saved
        // question identifies two abilities at the same timing.
        var book = AbilityCatalog.Parse("""
            {"cards":[{"card":"01110","abilities":[
              {"trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
               "effect":{"choose":{"options":[{"draw":{"player":"you","count":1}},
                                               {"draw":{"player":"you","count":2}}]}}},
              {"trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
               "effect":{"choose":{"options":[{"giveStatus":{"card":"you","status":"stunned"}},
                                               {"giveStatus":{"card":"you","status":"confused"}}]}}}
            ]}]}
            """);
        var runner = new AbilityRunner(book);
        var world = Deal();
        var identity = world.Seats[0].IdentityCard;
        var card = world.CreateCard(AuthoredCards.HydraBomber, world.AreaOf(DeckType.RevealingArea));
        int held = world.Seats[0].Hand.Cards.Count;
        runner.WhenRevealed(world, card, 0);
        Assert.Equal([0, 1], world.Agenda.Outstanding.Select(step => step.AbilityOrdinal));
        var first = world.Agenda.Current!.Value;
        runner.Chose(world, card, 0, first.Index, Decision.Take(1), first.Tier);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
        world.Agenda.Advance();
        world.Agenda.Advance();
        world.Agenda.Advance();
        var second = world.Agenda.Current!.Value;
        runner.Chose(world, card, 0, second.Index, Decision.Take(1), second.Tier);
        Assert.True(Statuses.Has(world, identity, Statuses.Confused));
        Assert.False(Statuses.Has(world, identity, Statuses.Stunned));
    }
}
