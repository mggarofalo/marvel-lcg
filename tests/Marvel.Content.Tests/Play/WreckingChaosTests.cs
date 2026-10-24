using Marvel.Content.Tests.Cards;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingChaosTests
{
    private static readonly string[] Faces = ["07011", "07026", "07056"];
    private static readonly int[] Seats = [0, 1];
    private static readonly int[] Modes = [0, 1, 2, 3];
    public static IEnumerable<object[]> Cases() => Faces.SelectMany((face, index) =>
        Seats.SelectMany(seat => Modes.Select(mode => new object[] { face, index == 2 ? 3 : index, seat, mode })));

    [Theory]
    [MemberData(nameof(Cases))]
    [Rule("rr:surge.1")]
    public void RevealOffersDiscardOrCountedThreatAndSurgesOnlyWithoutUpgrades(string face, int owner, int player, int mode)
    {
        // Printed text: discard a controlled upgrade or place one threat per controlled upgrade;
        // with no upgrades, surge. rr:surge.1 deals "1 facedown encounter card".
        // ruling:de7450cad15c3dbd: "If you discard your last upgrade, the treachery will gain surge."
        var (world, groups, source) = WreckingEncounterCardFixture.Create(face, owner, players: 2);
        world.CreateCard("01088", world.Seats[1].Deck);
        world.EncounterTopology!.SelectActive(groups[2].Villain, "test", []);
        var upgrades = mode == 2 ? Array.Empty<Card>() : Upgrades(world, player, mode == 3 ? 1 : 2);
        var foreign = Upgrades(world, 1 - player);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        runner.ExecuteReveal(world, source, player);
        if (mode != 2) AnswerChoice(world, runner, player, mode == 3 ? 0 : mode, upgrades);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(mode == 1 ? 2 : 0, groups[2].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, groups[0].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.All(foreign, card => Assert.Equal(DeckType.UpgradesArea, card.Area.Type));
        Assert.Equal(mode is 2 or 3 ? 1 : 0, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player)).Cards.Count);
        Assert.Equal(mode is 2 or 3 ? 1 : 2, groups[2].Deck.Cards.Count);
        if (mode is 0 or 3) Assert.Equal(DeckType.DiscardPile, upgrades[^1].Area.Type);
        if (mode == 1) Assert.All(upgrades, card => Assert.Equal(DeckType.UpgradesArea, card.Area.Type));
    }

    public static IEnumerable<object[]> BoostCases() => Faces.SelectMany((face, index) =>
        Seats.SelectMany(seat => Seats.SelectMany(defended => Seats.Select(present =>
            new object[] { face, index == 2 ? 3 : index, seat, defended == 1, present == 1 }))));

    [Theory]
    [MemberData(nameof(BoostCases))]
    public void BoostDiscardsOnlyFromTheUndefendedPlayersControlledUpgrades(
        string face, int owner, int player, bool defended, bool present)
    {
        var (world, groups, source) = WreckingEncounterCardFixture.Create(face, owner, players: 2);
        world.CreateCard("01088", world.Seats[1].Deck);
        var upgrades = present ? Upgrades(world, player) : Array.Empty<Card>();
        var foreign = Upgrades(world, 1 - player);
        var identity = world.Seats[player].IdentityCard;
        world.Attack = new EnemyAttack(groups[0].Villain.ObjectId, player, identity.ObjectId,
            Defender: defended ? identity.ObjectId : -1);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        runner.ExecuteBoost(world, source, player);
        var prompt = Sequence.Work(world, world.Facts, runner, []);
        if (present && !defended)
        {
            Assert.NotNull(prompt);
            Assert.Equal(player, prompt.Player);
            Assert.Equal(upgrades.Select(card => card.ObjectId), prompt.Affordances.Select(option => option.Id));
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(upgrades[0].ObjectId), []);
            Sequence.Finish(world, world.Facts, runner, []);
            Assert.Equal(DeckType.DiscardPile, upgrades[0].Area.Type);
        }
        else Assert.Null(prompt);
        Assert.All(foreign, card => Assert.Equal(DeckType.UpgradesArea, card.Area.Type));
        if (defended) Assert.All(upgrades, card => Assert.Equal(DeckType.UpgradesArea, card.Area.Type));
    }

    private static void AnswerChoice(World world, AbilityRunner runner, int player, int mode, Card[] upgrades)
    {
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(player, choice.Player);
        Assert.Equal(2, choice.Affordances.Count);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(choice.Affordances[mode].Id), []);
        if (mode != 0) return;
        var target = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(upgrades.Select(card => card.ObjectId), target.Affordances.Select(option => option.Id));
        Sequence.Answer(world, world.Facts, runner, target, Decision.Take(upgrades[^1].ObjectId), []);
    }

    private static Card[] Upgrades(World world, int player, int count = 2)
    {
        var first = world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(player), cardOwner: player));
        return count == 1 ? [first] :
            [first, world.CreateCard("01007", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(player), cardOwner: player))];
    }
}
