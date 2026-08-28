using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Keyword instances gained while a card is being revealed.</summary>
public sealed class KeywordStackingTests
{
    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:keywords.1")]
    [Rule("rr:surge")]
    [Fact]
    public void GainingSurgeDoesNotRepeatPrintedSurge()
    {
        // "If a card gains multiple instances of a keyword, any additional
        // instances have no effect unless that keyword is followed by a
        // number." Weapons Runner already prints Surge; gaining it during the
        // same reveal does not deal a second encounter card.
        var runner = Runner(
            "01121",
            """{ "gainSurge": 1 }""");
        var (world, card) = Board("01121", runner);

        ResolveReveal(world, card, runner);

        AssertOneAdditionalCard(world);
    }

    [Rule("rr:keywords.1")]
    [Rule("rr:surge")]
    [Fact]
    public void MultipleGainedSurgeInstancesResolveOnce()
    {
        // Surge is not numbered. A value of two and a later second node are
        // three instances, not three additional cards; together they create
        // the keyword's one When Revealed ability.
        var runner = Runner(
            "01110",
            """{ "seq": [ { "gainSurge": 2 }, { "gainSurge": 1 } ] }""");
        var (world, card) = Board("01110", runner);

        ResolveReveal(world, card, runner);

        AssertOneAdditionalCard(world);
    }

    [Rule("rr:keywords.1")]
    [Rule("rr:surge")]
    [Fact]
    public void SeparateAbilitiesShareTheOneGainedSurge()
    {
        // Each When Revealed ability has its own resolution state, but both
        // belong to this one reveal. The first gained instance creates Surge's
        // ability and the second is inert even though it came from another
        // printed ability on the card.
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "01110", "abilities": [
              { "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
                "effect": { "gainSurge": 1 } },
              { "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
                "effect": { "gainSurge": 1 } }
            ] } ] }
            """));
        var (world, card) = Board("01110", runner);

        ResolveReveal(world, card, runner);

        AssertOneAdditionalCard(world);
    }

    [Rule("rr:keywords.1")]
    [Rule("rr:surge")]
    [Fact]
    public void GainedSurgeSurvivesAChoiceContinuation()
    {
        // The choice only suspends this one reveal ability. It does not create
        // a new instance of that ability, so the later gain is still the inert
        // additional instance described by the keyword rule.
        var runner = Runner(
            "01110",
            """{ "seq": [ { "gainSurge": 1 }, { "choose": { "options": [ { "seq": [] }, { "seq": [] } ] } }, { "gainSurge": 1 } ] }""");
        var (world, card) = Board("01110", runner);

        ResolveReveal(world, card, runner);
        var waiting = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.ChooseOption);
        runner.Chose(world, card, 0, waiting.Index, Decision.Take(0), waiting.Tier);

        AssertOneAdditionalCard(world);
    }

    [Rule("rr:keywords.1")]
    [Rule("rr:surge")]
    [Fact]
    public void GainedSurgeSurvivesAnActivationContinuation()
    {
        // An activation caused in the middle of the ability finishes before
        // its remaining steps. Resuming those steps does not make the later
        // non-numeric keyword instance effective again.
        var runner = Runner(
            "01110",
            """{ "seq": [ { "gainSurge": 1 }, { "enemyAttacks": { "enemies": { "query": "villain" } } }, { "gainSurge": 1 } ] }""");
        var (world, card) = Board("01110", runner);
        var seat = world.Seats[0];
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        var villain = world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));

        ResolveReveal(world, card, runner);
        int activation = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.Attack).ActivationId;
        runner.ActivationCompleted(
            world,
            new EnemyActivation(
                villain.ObjectId, 0, Attacking: true, activation, Made: true));

        AssertOneAdditionalCard(world);
    }

    [Rule("rr:keywords.1")]
    [Rule("rr:surge")]
    [Fact]
    public void LaterAbilityUpdatesAnEarlierChoiceContinuation()
    {
        // The first ability suspends before Surge is gained. The second
        // ability still belongs to the same reveal, so its gain must update
        // the already-scheduled continuation rather than leave a stale copy.
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "01110", "abilities": [
              { "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
                "effect": { "seq": [
                  { "choose": { "options": [ { "seq": [] }, { "seq": [] } ] } },
                  { "gainSurge": 1 } ] } },
              { "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
                "effect": { "gainSurge": 1 } }
            ] } ] }
            """));
        var (world, card) = Board("01110", runner);

        ResolveReveal(world, card, runner);
        var waiting = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.ChooseOption);
        Assert.True(waiting.SurgeGained);
        runner.Chose(world, card, 0, waiting.Index, Decision.Take(0), waiting.Tier);

        AssertOneAdditionalCard(world);
    }

    [Rule("rr:keywords.1")]
    [Rule("rr:surge")]
    [Fact]
    public void LaterAbilityUpdatesAnEarlierActivationContinuation()
    {
        // As with a pending choice, the activation continuation can exist
        // before a sibling ability gains Surge. The shared reveal state must
        // advance that pending cast before it resumes its remaining step.
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "01110", "abilities": [
              { "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
                "effect": { "seq": [
                  { "enemyAttacks": { "enemies": { "query": "villain" } } },
                  { "gainSurge": 1 } ] } },
              { "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                             "subject": "this" },
                "effect": { "gainSurge": 1 } }
            ] } ] }
            """));
        var (world, card) = Board("01110", runner);
        var seat = world.Seats[0];
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        var villain = world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));

        ResolveReveal(world, card, runner);
        int activation = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.Attack).ActivationId;
        runner.ActivationCompleted(
            world,
            new EnemyActivation(
                villain.ObjectId, 0, Attacking: true, activation, Made: true));

        AssertOneAdditionalCard(world);
    }

    [Rule("rr:keywords.1")]
    [Rule("rr:surge")]
    [Fact]
    public void GainedSurgeSurvivesEachPlayerFrames()
    {
        // Each-player work is represented by saveable agenda frames. Those
        // frames and the final return to the outer sequence remain part of the
        // same reveal and therefore share its one effective Surge instance.
        var runner = Runner(
            "01110",
            """{ "seq": [ { "gainSurge": 1 }, { "eachPlayer": { "effect": { "seq": [] } } }, { "gainSurge": 1 } ] }""");
        var (world, card) = Board("01110", runner);

        ResolveReveal(world, card, runner);
        var frame = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.ResolveEachPlayer);
        runner.ResolveEachPlayer(
            world, card, frame.Seat, frame.Index, frame.Tier,
            frame.FinalStep, frame.FinalPlayer);

        AssertOneAdditionalCard(world);
    }

    [Rule("rr:keywords.1")]
    [Rule("rr:surge")]
    [Fact]
    public void EachPlayerFramesShareGainedSurgeAcrossPlayers()
    {
        // The per-player frames are separate saved casts, but the keyword was
        // gained by the card once during one reveal. The first frame advances
        // the remaining frame so the second player does not resolve Surge too.
        var runner = Runner(
            "01110",
            """{ "eachPlayer": { "effect": { "gainSurge": 1 } } }""");
        var world = new World(Cards, players: 2) { Abilities = runner };
        world.CreateSeat("p0");
        world.CreateSeat("p1");
        var card = world.CreateCard("01110", world.AreaOf(DeckType.RevealingArea));
        world.CreateCard("01122", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01123", world.AreaOf(DeckType.EncounterDeck));
        world.Agenda.Add(new PhaseStep(
            Steps.ResolveEachPlayer, 1, 2, Index: 1, Subject: card.ObjectId,
            Seat: 0, Plan: true, Tier: Marvel.Rules.Timing.AbilityType.WhenRevealed,
            EachPlayerFrame: true));
        world.Agenda.Add(new PhaseStep(
            Steps.ResolveEachPlayer, 1, 2, Index: 1, Subject: card.ObjectId,
            Seat: 1, Plan: true, Tier: Marvel.Rules.Timing.AbilityType.WhenRevealed,
            FinalPlayer: true, EachPlayerFrame: true));

        var first = world.Agenda.Current!.Value;
        runner.ResolveEachPlayer(
            world, card, first.Seat, first.Index, first.Tier,
            first.FinalStep, first.FinalPlayer);
        world.Agenda.Advance();
        world.Agenda.Advance();
        world.Agenda.Advance();
        var second = world.Agenda.Current!.Value;
        runner.ResolveEachPlayer(
            world, card, second.Seat, second.Index, second.Tier,
            second.FinalStep, second.FinalPlayer);

        AssertOneAdditionalCard(world);
    }

    private static AbilityRunner Runner(string faceId, string effect) =>
        new(AbilityCatalog.Parse(
            $$"""
            { "cards": [ { "card": "{{faceId}}", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                           "subject": "this" },
              "effect": {{effect}}
            } ] } ] }
            """));

    private static (World World, Card Card) Board(string faceId, AbilityRunner runner)
    {
        var world = new World(Cards, players: 1) { Abilities = runner };
        world.CreateSeat("p0");
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
        world.CreateCard("01122", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01123", world.AreaOf(DeckType.EncounterDeck));
        return (world, card);
    }

    private static void ResolveReveal(World world, Card card, AbilityRunner runner)
    {
        Reveal.Keywords(world, Cards, runner, card, player: 0, []);
        runner.WhenRevealed(world, card, player: 0);
    }

    private static void AssertOneAdditionalCard(World world)
    {
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Single(world.AreaOf(DeckType.EncounterDeck).Cards);
    }
}
