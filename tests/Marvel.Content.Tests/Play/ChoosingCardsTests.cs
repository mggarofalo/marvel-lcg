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

/// <summary>
/// An ability that stops and asks — <c>rr:choose-option</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the shape the interpreter did not have.</b> Everything a card
/// could do until now was something the engine could finish: give a status,
/// deal damage, schedule an activation. "Choose to either take 2 damage or
/// place 1 threat on the main scheme" is none of those — the ability has to
/// stop, a player has to answer, and only then does anything happen.
/// </para>
/// <para>
/// The mechanism is the agenda, the same one an attack uses. <c>choose</c>
/// suspends the ability and puts a <c>ChooseOption</c> step behind the step
/// that is running; the step asks; the answer runs the option. What the step
/// carries is the <i>card</i>, not the effect tree — a step is a small value
/// on the board — so the node is found again from the card, and a card holding
/// two choices is found again by the tier the step carries.
/// </para>
/// </remarks>
public sealed class ChoosingCardsTests
{
    private const string Campaign = "rhino";
    private const uint Seed = 12345;

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:choose-option")]
    [Fact]
    public void HydraBomberAsksRatherThanDeciding()
    {
        // Nothing has happened yet when the ability returns. Both of its
        // options change the board, and neither has: what the reveal produced
        // is a question.
        var world = Deal();
        var identity = world.Seats[0].IdentityCard;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;

        var (card, events) = Reveal(world, AuthoredCards.HydraBomber);

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
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
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
        Assert.Equal(
            ["placeThreat", "dealDamage"], asked.Affordances.Select(option => option.Label));
    }

    [Rule("rr:choose-option")]
    [Fact]
    public void TwoAbilitiesAtOneTierWithAChoiceInEachAreRefused()
    {
        // The tier is as fine as the step gets, so this is the next thing it
        // would have to carry rather than something to guess at. No printed card
        // needs it; the refusal names what is missing.
        var world = Deal();
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
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

        var refused = Assert.Throws<RulesNotImplementedException>(() => runner.Choosing(
            world, card, 0, 1, AbilityType.WhenRevealed));

        Assert.Contains(
            "choice in more than one 'WhenRevealed' ability",
            refused.Message,
            StringComparison.Ordinal);
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
        var (card, _) = Reveal(world, AuthoredCards.HydraBomber, player: 1);

        var asked = AuthoredCards.Runner().Choosing(world, card, player: 1, stoppedAt: 1)!;

        Assert.Equal(1, asked.Player);
        Assert.Equal(Question.Option, asked.Asking);

        // Two options, and no way out of them: `rr:choose-option` offers a
        // choice between things that happen, not a chance to decline.
        Assert.Equal(2, asked.Affordances.Count);
        Assert.False(asked.Cancellable);
        Assert.Equal(["dealDamage", "placeThreat"], asked.Affordances.Select(a => a.Label));
    }

    [Rule("rr:you-your.2")]
    [Fact]
    public void TakingTheDamageDamagesTheResolvingPlayersIdentity()
    {
        // `rr:you-your.2`: "if a card deals damage to 'you' [...] the player
        // resolving that damage applies it to the hit point dial of their
        // identity." Second player again, and the first is left alone.
        var world = Deal("spider_man", "she_hulk");
        var (card, _) = Reveal(world, AuthoredCards.HydraBomber, player: 1);

        AuthoredCards.Runner().Chose(world, card, 1, 1, Decision.Take(0));

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(2, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(
            0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void TakingTheThreatPlacesItOnTheMainScheme()
    {
        // The other branch, and the assertion that matters is the one about the
        // branch *not* taken: an interpreter that ran both would pass every
        // test above.
        var world = Deal();
        var (card, _) = Reveal(world, AuthoredCards.HydraBomber);

        AuthoredCards.Runner().Chose(world, card, 0, 1, Decision.Take(1));

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(
            1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void AnAnswerThatNamesNoOptionIsRefusedByName()
    {
        // The prompt is not cancellable and the options are numbered, so both
        // a decline and a number outside the list are errors rather than a
        // silently skipped ability.
        var world = Deal();
        var (card, _) = Reveal(world, AuthoredCards.HydraBomber);
        var runner = AuthoredCards.Runner();

        Assert.Throws<RulesNotImplementedException>(
            () => runner.Chose(world, card, 0, 1, Decision.Decline));
        Assert.Throws<RulesNotImplementedException>(
            () => runner.Chose(world, card, 0, 1, Decision.Take(2)));
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
        var card = world.CreateCard(
            AuthoredCards.HydraBomber, world.AreaOf(DeckType.RevealingArea));

        world.Agenda.Add(new PhaseStep(
            Steps.RevealEncounterCard, 1, 4, Subject: card.ObjectId, Seat: 0));

        var asked = Sequence.Work(world, Cards, abilities, events);

        Assert.NotNull(asked);
        Assert.Equal(Question.Option, asked.Asking);

        var threat = asked.Affordances.Single(option => option.Label == "placeThreat");
        Sequence.Answer(world, Cards, abilities, asked, Decision.Take(threat.Id), events);
        Sequence.Work(world, Cards, abilities, events);

        Assert.Equal(
            1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
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
        var book = Marvel.Cards.Dsl.AbilityCatalog.Parse(
            """
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
        var card = world.CreateCard(
            AuthoredCards.HydraBomber, world.AreaOf(DeckType.RevealingArea));
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
        var book = Marvel.Cards.Dsl.AbilityCatalog.Parse(
            """
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
        var card = world.CreateCard(
            AuthoredCards.HydraBomber, world.AreaOf(DeckType.RevealingArea));
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

    private static (Card Card, IReadOnlyList<Marvel.Rules.Events.GameEvent> Events) Reveal(
        World world, string faceId, int player = 0)
    {
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
        return (card, AuthoredCards.Runner().WhenRevealed(world, card, player));
    }

    private static World Deal(params string[] heroes)
    {
        string[] playing = heroes.Length > 0 ? heroes : ["spider_man"];
        return WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, playing), Cards),
            [.. playing.Select(hero => Setup.Hero(hero).Name)],
            Seed);
    }
}
