using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// The Rhino scenario, played with the cards actually doing what they say.
/// </summary>
/// <remarks>
/// <para>
/// <c>WholeGameTests</c> plays this board with <c>NoCardAbilities</c>, so what
/// it proves is that the <i>rules</i> reach an ending. It could not prove more:
/// until recently every encounter card resolved to silence, and a game where
/// nothing a card says happens is not the game.
/// </para>
/// <para>
/// This plays the same board with the real interpreter, and states the gap as a
/// <b>list</b> rather than a number. Forty seeds either run to an ending or stop
/// on a card nobody has written — and which cards those are is asserted, so
/// authoring one is a visible change here and a <i>new</i> blocker is a failure.
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
    /// <summary>
    /// The Rhino cards nobody has written yet, and why each is hard.
    /// </summary>
    /// <remarks>
    /// <c>01098</c> Armored Rhino Suit is a replacement effect — "when any
    /// amount of damage would be dealt to Rhino, place it here instead" — and
    /// damage is not an occurrence with a window, so nothing can replace it
    /// yet. <c>01111</c> Explosion assigns damage among several characters,
    /// which is a distribution the player chooses.
    /// <para>
    /// <c>01165</c> Eviction Notice used to be here. It asks twice — "you may
    /// flip to alter-ego form" and then "choose:" — and a suspended ability had
    /// nowhere to remember where it stopped.
    /// </para>
    /// </remarks>
    private static readonly string[] Unwritten = ["01098", "01111"];

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void EverySeedEitherFinishesOrStopsOnACardNobodyHasWritten()
    {
        var blocked = new SortedSet<string>(StringComparer.Ordinal);
        int finished = 0;

        for (uint seed = 1; seed <= 40; seed++)
        {
            string? stopped = Play(seed);
            if (stopped is null)
            {
                finished++;
                continue;
            }

            Assert.True(
                Unwritten.Any(card => stopped.Contains(card, StringComparison.Ordinal)),
                $"seed {seed} stopped on something that is not an unwritten card: {stopped}");
            blocked.UnionWith(Unwritten.Where(
                card => stopped.Contains(card, StringComparison.Ordinal)));
        }

        // Most of them get all the way, which is the claim worth making: the
        // engine plays this scenario, and what is left is three cards rather
        // than a rule.
        Assert.True(finished >= 25, $"only {finished} of 40 seeds reached an ending");

        // And every card in the list is still blocking something. One that has
        // been written should come out of the list rather than sit in it
        // claiming a gap that is closed.
        Assert.Equal(Unwritten.Order(StringComparer.Ordinal), blocked);
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
