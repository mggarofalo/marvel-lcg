using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// The same boards, played by somebody who acts.
/// </summary>
/// <remarks>
/// <para>
/// <c>RealCardsGameTests</c> plays these boards with a policy that
/// declines everything it can, and says out loud what that costs: the villain
/// deck never advances, because nobody ever attacks. So eighty
/// green games proved that the <i>encounter deck</i> resolves, and nothing at
/// all about playing a card, paying for it, attacking, thwarting, recovering,
/// or triggering an action. the original investigation.
/// </para>
/// <para>
/// This is the other half of that coverage and it is not a better player —
/// <see cref="ActingPolicy"/> takes a random legal option. What it is, is a
/// player who reaches the parts of the engine a passer cannot, and the two
/// bugs it found on its first run are both of that kind: an end-of-phase
/// prompt that offered an answer the engine then refused (the original investigation), and two
/// options in one turn prompt sharing an id, where taking one silently
/// resolved the other (the original investigation).
/// </para>
/// <para>
/// <b>Two decline rates, and the second earns its runtime.</b> A player who
/// passes one turn option in four walks further into the encounter deck; one
/// who never passes when it can act takes every option the moment it appears,
/// and it was the second that first put an ability and a card play in the same
/// prompt often enough to collide. Neither rate is the interesting one on its
/// own — a policy that only ever passed or only ever acted would be a third
/// narrow thing.
/// </para>
/// <para>
/// <b>No seed wins, and that is not a gap in these assertions.</b> A uniformly
/// random player loses this game — it spends its whole hand on the first card
/// it can afford and attacks whatever it happens to pick. Reaching
/// <see cref="Outcome.PlayersWin"/> through play needs a policy that plans, and
/// that is the original investigation rather than something to assert here. What is asserted
/// instead is that the play actually happened, so this cannot quietly decay
/// into the passing policy it exists to complement.
/// </para>
/// </remarks>
public sealed class ActingGameTests
{
    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData("rhino", 4)]
    [InlineData("rhino_expert", 4)]
    [InlineData("rhino", 1000)]
    [InlineData("rhino_expert", 1000)]
    public void EverySeedPlaysToAnEndingWithAPlayerWhoActs(string campaign, int declineOneIn)
    {
        // Forty seeds each, and every one reaches an ending rather than a card
        // or a step nobody has written. The declining suite says the same of
        // the same boards; what is different here is the half of the engine
        // that a decline never enters.
        for (uint seed = 1; seed <= 40; seed++)
        {
            var played = Play(campaign, seed, declineOneIn);
            Assert.True(played.Stopped is null, $"seed {seed} stopped: {played.Stopped}");
            Assert.True(played.Finished, $"seed {seed} did not finish");
        }
    }

    [Fact]
    public void ThePlayerActuallyPlaysRatherThanPassingWithExtraSteps()
    {
        // The assertion that keeps the test above honest. Each of these is a
        // route the declining policy never takes, so a change that made the
        // acting policy stop acting would leave forty green games behind it —
        // which is the shape of the sweep probe this suite deleted for lying.
        //
        // Counted over the whole run rather than per seed, because a seed whose
        // opening hand is unaffordable is a real game and not a broken one. As
        // The exact totals change as identity cards gain executable abilities;
        // only the presence of each route is asserted here.
        var verbs = new Dictionary<string, long>(StringComparer.Ordinal);
        for (uint seed = 1; seed <= 40; seed++)
        {
            foreach (var (verb, count) in Play("rhino", seed).Verbs)
            {
                verbs[verb] = verbs.GetValueOrDefault(verb) + count;
            }
        }

        Assert.True(verbs.GetValueOrDefault(CardPlay.Verb) > 0, "no card was ever played");
        Assert.True(verbs.GetValueOrDefault("Attack") > 0, "nobody ever attacked");
        Assert.True(verbs.GetValueOrDefault("Thwart") > 0, "nobody ever thwarted");
        Assert.True(verbs.GetValueOrDefault("Recover") > 0, "nobody ever recovered");

        // `rr:attack-enemy-activation.step.2` — a player who never acts is
        // never asked to declare a defender either, because the question is put
        // whatever they do. It is here because it is the one of these that
        // costs the *villain* something, so it is the one most likely to go
        // quiet without anything else changing.
        Assert.True(verbs.GetValueOrDefault("Defense") > 0, "nobody ever defended");

        // Defeat is held directly by DamageTests and the card-specific tests.
        // It is not a player decision, so this random policy is not its gate.
    }

    [Fact]
    public void NoOptionInAPromptCanBeMistakenForAnother()
    {
        // `Game.Resolve` finds the answer with `First(option => option.Id ==
        // input.Affordance)`, so two options sharing an id in one prompt do not
        // fail -- they resolve the wrong one, and the player sees a card played
        // where they asked for an ability. the original investigation was exactly that, between
        // an ability's affordance (numbered by its card's object id) and a card
        // play (numbered from a counter).
        //
        // `ActingPolicy` checks it on every prompt it is given, so this is the
        // same games as above read for a different property: several thousand
        // prompts across two boards, rather than one hand-built collision.
        foreach (string campaign in new[] { "rhino", "rhino_expert" })
        {
            foreach (int declineOneIn in new[] { 4, 1000 })
            {
                for (uint seed = 1; seed <= 40; seed++)
                {
                    var played = Play(campaign, seed, declineOneIn);
                    Assert.Null(played.Ambiguous);
                    Assert.True(played.Prompts > 0, $"seed {seed} was asked nothing");
                }
            }
        }
    }

    private static Played Play(string campaign, uint seed, int declineOneIn = 4)
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, campaign, ["spider_man"]), Cards),
            ["Spider-Man"],
            seed,
            AuthoredCards.Runner());
        var game = Game.Begin(world, Cards, AuthoredCards.Runner());
        var policy = new ActingPolicy((int)seed, declineOneIn);
        var verbs = new Dictionary<string, long>(StringComparer.Ordinal);

        try
        {
            for (int decisions = 0; game.Pending is not null; decisions++)
            {
                Assert.True(decisions < 5000, $"seed {seed} is still playing");
                foreach (var happened in game.Resolve(policy.Answer(game.Pending)).Events)
                {
                    verbs[happened.Verb] = verbs.GetValueOrDefault(happened.Verb) + 1;
                }
            }

            return new Played(null, null, policy.Answered, true, verbs);
        }
        catch (AmbiguousPromptException ambiguous)
        {
            return new Played(null, ambiguous.Message, policy.Answered, false, verbs);
        }
        catch (RulesNotImplementedException stopped)
        {
            return new Played(stopped.Message, null, policy.Answered, false, verbs);
        }
    }

    private sealed record Played(
        string? Stopped,
        string? Ambiguous,
        int Prompts,
        bool Finished,
        IReadOnlyDictionary<string, long> Verbs);
}
