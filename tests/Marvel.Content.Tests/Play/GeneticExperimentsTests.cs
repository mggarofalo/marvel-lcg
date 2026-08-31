using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Genetic Experiments — an attachment whose host is selected by trait rather
/// than by title or role.
/// </summary>
/// <remarks>
/// "Attach to an [[Infinite]] minion. Otherwise, this card gains surge.
/// Attached minion gets +2 hit points. <b>Forced Interrupt</b>: When attached
/// minion is defeated, place 2 threat on Gene Pool. [star] <b>Boost</b>: Attach
/// this card to an [[Infinite]] minion."
/// </remarks>
public sealed class GeneticExperimentsTests
{
    private const string InfiniteSoldier = "45069";
    private const string InfiniteHunter = "45065";
    private const string HydraMercenary = "01101";

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:attach-to")]
    [Rule("rr:choose-game-element.1")]
    [Fact]
    public void RevealOffersEveryInfiniteMinionAndAttachesToTheSelectedOne()
    {
        // `rr:attach-to` requires the specified game element as the card enters
        // play. The card says "an Infinite minion" rather than "choose", so
        // applying `rr:choose-game-element.1` to let the first player select
        // among several eligible hosts is explicitly our reading.
        var world = Empty();
        var first = Minion(world, InfiniteSoldier);
        var second = Minion(world, InfiniteHunter);
        var excluded = Minion(world, HydraMercenary);
        var experiment = Dealt(world);
        var events = new List<GameEvent>();

        var asked = Assert.IsType<Prompt>(
            Sequence.Work(world, Cards, world.Abilities, events));

        Assert.Equal(world.FirstPlayer, asked.Player);
        Assert.Equal(Question.Element, asked.Asking);
        Assert.Equal(
            [first.ObjectId, second.ObjectId],
            asked.Affordances.Select(option => option.Id));
        Assert.DoesNotContain(
            asked.Affordances, option => option.Id == excluded.ObjectId);

        Sequence.Answer(
            world, Cards, world.Abilities, asked,
            Decision.Take(second.ObjectId), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));

        Assert.Equal(DeckType.UpgradesArea, experiment.Area.Type);
        Assert.Equal(second.ObjectId, experiment.Area.Host);
        Assert.Equal(3, Damage.Health(world, Cards, first));
        Assert.Equal(6, Damage.Health(world, Cards, second));
    }

    [Rule("rr:surge.1")]
    [Rule("rr:attach-to.3")]
    [Fact]
    public void RevealWithNoInfiniteMinionGainsSurgeAndRemainsWhereItWas()
    {
        // The legality check does not pass, so `rr:attach-to.3` leaves the card
        // in its revealing area and the printed "otherwise" applies.
        // `rr:surge.1`: "When Revealed: Deal yourself 1 facedown encounter
        // card." The original attachment finishes before that dealt card is
        // later revealed.
        var world = Empty();
        Minion(world, HydraMercenary);
        var encounterDeck = world.AreaOf(DeckType.EncounterDeck);
        var extra = world.CreateCard("01090", encounterDeck);
        World.MoveToTop(extra, encounterDeck);
        var experiment = Dealt(world);

        Assert.Null(Sequence.Work(world, Cards, world.Abilities, []));

        Assert.Equal(DeckType.RevealingArea, experiment.Area.Type);
        Assert.Equal(
            DeckType.DealtEncounterCardsDeck,
            extra.Area.Type);
        Assert.Equal(PlayArea.Of(0), extra.Area.PlayArea);
    }

    [Rule("rr:maximum-hit-points")]
    [Fact]
    public void AttachedMinionGetsTwoMaximumHitPointsOnlyWhileAttached()
    {
        // "A character's maximum hit points is their base hit points plus or
        // minus all 'gets' hit point modifiers that are active on that
        // character." Infinite Soldier prints three and this card grants two.
        var world = Empty();
        var minion = Minion(world, InfiniteSoldier);
        var experiment = world.CreateCard(
            AuthoredCards.GeneticExperiments,
            world.AreaOf(
                DeckType.UpgradesArea, minion.Area.PlayArea,
                minion.ObjectId, minion.Area.CardOwner));

        Assert.Equal(5, Damage.Health(world, Cards, minion));

        Discard.Card(world, experiment, "test", []);

        Assert.Equal(3, Damage.Health(world, Cards, minion));
    }

    [Rule("rr:damage.step.7")]
    [Fact]
    public void HostDefeatPlacesThreatBeforeTheAttachmentLeavesPlay()
    {
        // Step 7 resolves abilities triggered when the character is defeated;
        // the host and everything attached to it leave in step 8. The order is
        // observable because Genetic Experiments must still be in play when it
        // places two threat on Gene Pool.
        var world = Empty();
        var pool = world.CreateCard(
            AuthoredCards.GenePool, world.AreaOf(DeckType.SideSchemesArea));
        var minion = Minion(world, InfiniteSoldier);
        var experiment = world.CreateCard(
            AuthoredCards.GeneticExperiments,
            world.AreaOf(
                DeckType.UpgradesArea, minion.Area.PlayArea,
                minion.ObjectId, minion.Area.CardOwner));
        long before = pool.Tokens.GetValueOrDefault("k_threat");
        var events = new List<GameEvent>();

        Agendas.Happening(world);
        Defeat.Character(world, Cards, minion, "test", events);
        events.AddRange(Agendas.Finish(world, Cards, world.Abilities));

        Assert.Equal(before + 2, pool.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, experiment.Area.Type);
        Assert.True(
            events.FindIndex(happened => happened.Verb == "Place_Threat")
                < events.FindIndex(happened => happened is CardsMoved moved
                    && moved.Cards.Any(landing => landing.Card == minion.ObjectId)));
    }

    [Rule("rr:attach-to.3.1")]
    [Rule("rr:choose-game-element.1")]
    [Fact]
    public void BoostAbilityChoosesAnInfiniteMinionAndAttachesTheBoostCard()
    {
        // `rr:attach-to.3.1`: the printed attach-to phrase is not resolved when
        // another ability attaches the card. The Boost ability is that other
        // ability, and its singular "an Infinite minion" uses the same stated
        // engine reading for the player's choice.
        var world = Empty(players: 2);
        world.FirstPlayer = 0;
        var chosen = Minion(world, InfiniteSoldier);
        var spared = Minion(world, InfiniteHunter);
        var experiment = world.CreateCard(
            AuthoredCards.GeneticExperiments,
            world.AreaOf(DeckType.BoostingArea));
        var runner = Assert.IsType<AbilityRunner>(world.Abilities);

        // The activating enemy is attacking seat 1, while seat 0 holds the
        // first-player token. `rr:first-player.2`: "the first player makes all
        // necessary decisions for encounter card effects that do not specify
        // which player makes the decision."
        runner.Boost(world, experiment, 1);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        var asked = Assert.IsType<Prompt>(runner.Choosing(
            world, experiment, 1, waiting.Index, waiting.Tier));

        Assert.Equal(world.FirstPlayer, asked.Player);
        Assert.Equal(
            [chosen.ObjectId, spared.ObjectId],
            asked.Affordances.Select(option => option.Id));
        runner.Chose(
            world, experiment, 1, waiting.Index,
            Decision.Take(chosen.ObjectId), waiting.Tier);

        Assert.Equal(DeckType.UpgradesArea, experiment.Area.Type);
        Assert.Equal(chosen.ObjectId, experiment.Area.Host);
        Assert.Equal(5, Damage.Health(world, Cards, chosen));
        Assert.Equal(4, Damage.Health(world, Cards, spared));
    }

    private static World Empty(int players = 1)
    {
        var world = new World(Cards, players);
        for (int player = 0; player < players; player++)
        {
            world.CreateSeat($"p{player}");
        }
        world.Abilities = AuthoredCards.Runner();
        return world;
    }

    private static Card Minion(World world, string faceId) => world.CreateCard(
        faceId, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

    private static Card Dealt(World world)
    {
        var card = world.CreateCard(
            AuthoredCards.GeneticExperiments,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        world.Agenda.Add(new PhaseStep(
            Steps.RevealEncounterCard, 1, 4, Subject: card.ObjectId, Seat: 0));
        return card;
    }
}
