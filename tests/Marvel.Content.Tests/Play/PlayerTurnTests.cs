using Marvel.Content.Setup;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// The player phase: every player takes a turn, then the phase ends.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:player-phase</c>: "during the player phase, <b>each player</b>
/// <i>(in player order)</i> takes one turn." One turn <i>each</i>, so the phase
/// is over when the last player has had theirs. A single-player board cannot
/// tell that apart from one turn per round, which is why these deal two.
/// </para>
/// </remarks>
public sealed class PlayerTurnTests
{
    private const string Campaign = "rhino";
    private const uint Seed = 12345;

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:player-phase")]
    [Fact]
    public void EveryPlayerTakesATurnBeforeThePhaseEnds()
    {
        var (game, _) = Begin("spider_man", "she_hulk");
        ResolveMulligans(game);

        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.Equal(0, game.Pending!.Player);

        // Declining ends *this player's* turn, not the phase.
        game.Resolve(Decision.Decline);

        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.Equal(1, game.Pending!.Player);

        // And only when the last of them has finished does the phase end.
        game.Resolve(Decision.Decline);

        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }

    [Rule("rr:in-player-order")]
    [Fact]
    public void TurnsGoRoundFromTheFirstPlayer()
    {
        // "The first player performs their part of the sequence first, followed
        // by the other players in clockwise order." At three players starting
        // from seat 2 that is 2, 0, 1 -- so this is a wrap and not a sort.
        var (game, _) = BeginFrom(2, "spider_man", "she_hulk", "captain_marvel");
        ResolveMulligans(game);

        var asked = new List<int>();
        while (game.Phase == GamePhase.PlayerTurn)
        {
            asked.Add(game.Pending!.Player);
            game.Resolve(Decision.Decline);
        }

        Assert.Equal([2, 0, 1], asked);
    }

    [Rule("rr:end-of-player-phase.step.1")]
    [Fact]
    public void EveryPlayerIsAskedToDiscardBeforeThePhaseMovesOn()
    {
        // Step 1 is "in player order", unlike steps 2 and 3, so it is a
        // question per seat rather than one for the table.
        var (game, _) = Begin("spider_man", "she_hulk");
        ResolveMulligans(game);
        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);

        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.Equal(0, game.Pending!.Player);
        Assert.Equal(Question.TurnOption, game.Pending.Asking);

        game.Resolve(Decision.Decline);

        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.Equal(1, game.Pending!.Player);
    }

    [Rule("rr:form-change-form.1")]
    [Fact]
    public void EachPlayerGetsTheirOwnFormChangeInARound()
    {
        // "Once each round, **each player** is permitted to change form." One
        // player using theirs does not spend anybody else's.
        var (game, world) = Begin("spider_man", "she_hulk");
        ResolveMulligans(game);

        var change = game.Pending!.Affordances.Single(a => a.Verb == Game.ChangeForm);
        game.Resolve(Decision.Take(change.Id));

        // The change is gone and the rest of the turn is not: a hero in hero
        // form can attack and thwart, which is `rr:player-turn.3`.
        Assert.DoesNotContain(game.Pending!.Affordances, a => a.Verb == Game.ChangeForm);
        game.Resolve(Decision.Decline);

        Assert.Equal(1, game.Pending!.Player);
        Assert.Contains(game.Pending.Affordances, a => a.Verb == Game.ChangeForm);
        Assert.True(Forms.In(world, world.Seats[0], Cards, Forms.Hero));
        Assert.True(Forms.In(world, world.Seats[1], Cards, Forms.AlterEgo));
    }

    [Rule("rr:attack-player-ability-type.1")]
    [Rule("rr:damage.step.5")]
    [Fact]
    public void BasicAttackTargetsProjectTheirResultBeforeCommit()
    {
        var (game, world) = Begin("spider_man");
        ResolveMulligans(game);
        var change = game.Pending!.Affordances.Single(a => a.Verb == Game.ChangeForm);
        game.Resolve(Decision.Take(change.Id));

        Affordance attack = game.Pending!.Affordances.Single(option =>
            option.Verb == BasicPowers.AttackVerb
            && option.AnchorId == world.Seats[0].IdentityCard.ObjectId);
        var target = world.TheCardIn(DeckType.VillainArea)!;

        Assert.Equal(
            "14/14 → 12/14 HP",
            attack.Targets!.Details![target.ObjectId]);
    }

    [Rule("rr:stun-stunned.1")]
    [Rule("rr:confuse-confused.1")]
    [Theory]
    [InlineData(BasicPowers.AttackVerb, Statuses.Stunned, "no damage will be dealt")]
    [InlineData(BasicPowers.ThwartVerb, Statuses.Confused, "no threat will be removed")]
    public void CancellingStatusReplacesABasicPowerTargetPreview(
        string verb, string status, string expected)
    {
        var (game, world) = Begin("spider_man");
        ResolveMulligans(game);
        Statuses.Give(world, world.Seats[0].IdentityCard, status);
        var change = game.Pending!.Affordances.Single(a => a.Verb == Game.ChangeForm);
        game.Resolve(Decision.Take(change.Id));

        Affordance power = game.Pending!.Affordances.Single(option =>
            option.Verb == verb
            && option.AnchorId == world.Seats[0].IdentityCard.ObjectId);

        Assert.All(power.Targets!.Details!.Values, detail =>
            Assert.Contains(expected, detail));
    }

    private static (Game Game, World World) Begin(params string[] heroes)
        => BeginFrom(0, heroes);

    private static (Game Game, World World) BeginFrom(int firstPlayer, params string[] heroes)
    {
        var world = WorldSetup.DealWithoutCardAbilities(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, heroes), Cards),
            [.. heroes.Select(hero => Setup.Hero(hero).Name)],
            Seed);
        world.FirstPlayer = firstPlayer;
        return (Game.BeginWithoutCardAbilities(world, Cards), world);
    }

    private static void ResolveMulligans(Game game)
    {
        while (game.Phase == GamePhase.Mulligan)
        {
            game.Resolve(Decision.Decline);
        }
    }
}
