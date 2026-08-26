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
        // **Every card in this scenario now does what it says.** Forty seeds,
        // the real interpreter, and not one of them meets a card nobody has
        // written.
        //
        // This test used to carry a list of the cards that blocked it, so that
        // authoring one was a visible change here. The list is empty, and what
        // is left of it is this assertion.
        for (uint seed = 1; seed <= 40; seed++)
        {
            string? stopped = Play(seed);
            Assert.True(stopped is null, $"seed {seed} stopped: {stopped}");
        }
    }

    /// <summary>Plays one seed out; answers with the message it stopped on.</summary>
    private static string? Play(uint seed)
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            seed);
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
    /// <c>rr:choose-option</c> offers a choice between things that happen, so
    /// a decline is not an answer to it — <c>Question.Option</c> is the only
    /// question here that must be answered. Everything else takes the decline,
    /// including the mulligan, where declining is how a hand is kept.
    /// </remarks>
    private static Decision Answer(Prompt asked) =>
        asked.Asking == Question.Option
            ? Decision.Take(asked.Affordances[0].Id)
            : Decision.Decline;
}
