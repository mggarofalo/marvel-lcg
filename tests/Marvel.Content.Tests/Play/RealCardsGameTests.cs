using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// The Rhino scenario, played with every card doing what it says.
/// </summary>
/// <remarks>
/// <para>
/// <c>WholeGameTests</c> plays this board with <c>NoCardAbilities</c>, so what
/// it proves is that the <i>rules</i> reach an ending. It could not prove more:
/// until recently every encounter card resolved to silence, and a game where
/// nothing a card says happens is not the game.
/// </para>
/// <para>
/// This plays the same board with the real interpreter, and <b>all forty seeds
/// reach an ending</b>. Every one of the scenario's twenty-four cards is
/// written, and so is every card of the nemesis set that Shadow of the Past
/// brings in.
/// </para>
/// <para>
/// It carried a list of the cards that blocked it while there were any, which
/// is how it earned its keep: authoring Eviction Notice let the seeds get
/// further, they reached Shadow of the Past, and Highway Robbery appeared as a
/// blocker nobody had a reason to look for.
/// </para>
/// <para>
/// The policy declines what it can, which is what makes the coverage broad
/// rather than lucky: a hero who never acts meets more of the encounter deck
/// than one who wins. It answers only the questions that cannot be declined —
/// <c>rr:choose-option</c> offers a choice between things that happen, not a
/// chance to refuse.
/// </para>
/// </remarks>
public sealed class RealCardsGameTests
{
    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void EverySeedPlaysToAnEnding()
    {
        // **Every card in this scenario does what it says.** Forty seeds, the
        // real interpreter, and not one of them meets a card nobody has
        // written. `UnusScenarioTests` is the same assertion for a board where
        // the list of unwritten cards is not yet empty.
        for (uint seed = 1; seed <= 40; seed++)
        {
            string? stopped = Play("rhino", seed);
            Assert.True(stopped is null, $"seed {seed} stopped: {stopped}");
        }
    }

    [Fact]
    public void EverySeedPlaysTheExpertScenarioToAnEnding()
    {
        // The same board dealt for expert mode, which `01097a`'s contents line
        // describes as "Rhino (II) and Rhino (III) instead" -- so the villain
        // deck opens on the second stage and the encounter set gains
        // Exhaustion, Masterplan and Under Fire.
        //
        // **What this does not reach is stage III.** The policy below declines
        // everything it can, so it never attacks, so the villain deck never
        // advances and every seed here ends in a villain win. Rhino III is
        // covered by `RhinoThreeTests` instead, which defeats a stage outright
        // rather than hoping a declining player gets there. Said out loud
        // because a green test over a board it never explores is the exact
        // shape of the sweep probe this suite deleted for lying.
        for (uint seed = 1; seed <= 40; seed++)
        {
            string? stopped = Play("rhino_expert", seed);
            Assert.True(stopped is null, $"seed {seed} stopped: {stopped}");
        }
    }

    [Fact]
    public void EverySeedPlaysNeedForSpeedToAnEnding()
    {
        // The Rhino board with the **Sinister Syndicate** instead of Bomb
        // Scare, and the first scenario the engine can play that is not the
        // one it was built on. Its thirty cards are all read, and the seven
        // that were not are what MARVEL-232 through MARVEL-238 were about.
        //
        // Same caveat as the expert deal above: the policy declines what it
        // can, so this walks the encounter deck rather than the endgame.
        for (uint seed = 1; seed <= 40; seed++)
        {
            string? stopped = Play("2410_need_for_speed", seed);
            Assert.True(stopped is null, $"seed {seed} stopped: {stopped}");
        }
    }

    /// <summary>Plays one seed out; answers with the message it stopped on.</summary>
    private static string? Play(string campaign, uint seed)
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, campaign, ["spider_man"]), Cards),
            ["Spider-Man"],
            seed,
            // `rr:appendix-ii-setup.step.12` is part of the deal, so the
            // interpreter has to be there before `Game.Begin` sees the board.
            AuthoredCards.Runner());
        var game = Game.Begin(world, Cards, AuthoredCards.Runner());

        try
        {
            for (int decisions = 0; game.Pending is not null; decisions++)
            {
                Assert.True(decisions < 600, $"seed {seed} is still playing");
                game.Resolve(Answer(game.Pending));
            }

            return null;
        }
        catch (RulesNotImplementedException stopped)
        {
            return stopped.Message;
        }
    }

    /// <summary>
    /// Declines everything that can be declined, and answers what cannot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Prompt.Cancellable</c> is the whole rule, rather than a list of
    /// question kinds. A card resolving a <c>chooseCard</c> is not cancellable
    /// -- <c>rr:choose-game-element</c> gives no way out, the ability is
    /// resolving and one of the things it offers is going to happen -- and an
    /// earlier version of this policy declined it because <c>Question.Element</c>
    /// was not on its list. Crime Pays found that on seed 6.
    /// </para>
    /// <para>
    /// The mulligan is the exception, and it is one because the engine has a
    /// hole rather than because the rules do: the prompt is offered
    /// non-cancellable and taking it is unimplemented, so declining is the
    /// only answer it accepts. MARVEL-229.
    /// </para>
    /// </remarks>
    private static Decision Answer(Prompt asked)
    {
        if (asked.Cancellable
            || string.Equals(
                asked.Affordances[0].Verb, Game.ResolveMulligans, StringComparison.Ordinal))
        {
            return Decision.Decline;
        }

        var taken = asked.Affordances[0];
        return Decision.Take(
            taken.Id,
            taken.Targets is { } wanted ? [.. wanted.Legal.Take(Math.Max(wanted.Min, 1))] : [],
            []);
    }
}
