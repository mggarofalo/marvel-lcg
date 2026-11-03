using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Drang (III) (16060) over the production book: "When Revealed: Discard the top 4[per_hero]
// cards of the encounter deck. Each time a minion is discarded this way, put it into play
// engaged with the player who is engaged with the fewest minions." Ruling 82bcc63411002a00:
// Interrupts resolve as each minion engages; "after they've all engaged, resolve all
// Quickstrike and Teamwork abilities on those minions, then any Response abilities."
public sealed class DrangThreeTests : CoreCardTestBase
{
    private const string Vulture = "01167";
    private const string KreePrivate = "16134";
    private const string MurrayReese = "38033";
    private const string WadeCole = "38032";

    [Fact]
    [Rule("rr:quickstrike")]
    [Rule("rr:when-revealed-abilities.2")]
    public void QuickstrikeResolvesBeforeAnEarlierMinionsResponse()
    {
        // The Assassin is discarded first. Its printed "Forced Response: After Badoon Assassin
        // engages your hero, it attacks you with +2 ATK" waits for the Vulture's Quickstrike.
        var world = BadoonBoards.Board(villain: BadoonBoards.DrangThree);
        var vulture = BadoonBoards.OnTop(world, Vulture);
        var assassin = BadoonBoards.OnTop(world, BadoonBoards.Assassin);

        var events = Reveal(world);

        Assert.Equal([vulture.ObjectId, assassin.ObjectId], events.OfType<AttackCompleted>().Select(attack => attack.Enemy));
        Assert.Equal(PlayArea.Of(0), vulture.Area.PlayArea);
        Assert.Equal(PlayArea.Of(0), assassin.Area.PlayArea);
        Assert.Equal(DeckType.EngagedEnemiesArea, assassin.Area.Type);
        Assert.Equal(6, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:engage")]
    public void EachMinionEntersBeforeTheNextCardIsDiscardedAndAttacksOnlyAfterTheLast()
    {
        var world = BadoonBoards.Board(villain: BadoonBoards.DrangThree);
        var vulture = BadoonBoards.OnTop(world, Vulture);
        var assassin = BadoonBoards.OnTop(world, BadoonBoards.Assassin);

        var events = Reveal(world);

        int assassinEnters = Moved(events, assassin, "Put_Into_Play");
        int vultureDiscarded = Moved(events, vulture, "Discard");
        int vultureEnters = Moved(events, vulture, "Put_Into_Play");
        int firstAttack = events.FindIndex(change => change is AttackCompleted);
        Assert.True(assassinEnters < vultureDiscarded);
        Assert.True(vultureEnters < firstAttack);
        Assert.Equal(4, events.OfType<CardsMoved>().Count(move => move.Verb == "Discard"));
    }

    [Fact]
    [Rule("rr:forced.5")]
    public void TheFirstPlayerOrdersTheBatchsQuickstrikes()
    {
        var world = BadoonBoards.Board(villain: BadoonBoards.DrangThree);
        var vulture = BadoonBoards.OnTop(world, Vulture);
        var kree = BadoonBoards.OnTop(world, KreePrivate);
        Prompt? asked = null;

        var events = Reveal(world, prompt =>
        {
            if (prompt.Trigger != MinionEntryBatchOperation.ResolveBatchedEntryKeywords) return null;
            asked ??= prompt;
            return Decision.Take(prompt.Affordances.Single(offer => offer.AnchorId == vulture.ObjectId).Id);
        });

        Assert.NotNull(asked);
        Assert.Equal(Question.Order, asked.Asking);
        Assert.Equal(world.FirstPlayer, asked.Player);
        Assert.Equal(new[] { kree.ObjectId, vulture.ObjectId }.Order(), asked.Affordances.Select(offer => offer.AnchorId).Order());
        Assert.Equal([vulture.ObjectId, kree.ObjectId], events.OfType<AttackCompleted>().Select(attack => attack.Enemy));
    }

    [Fact]
    [Rule("rr:first-player.1")]
    public void EachMinionEngagesThePlayerWithTheFewestAndTheFirstPlayerBreaksTies()
    {
        // Seat 1 already faces a minion. The Lieutenant goes to seat 0; the Warlord then
        // finds both players tied at one, and the first player sends it to seat 1.
        var world = BadoonBoards.Board(players: 2, villain: BadoonBoards.DrangThree);
        world.CreateCard(BadoonBoards.Sentry, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var warlord = BadoonBoards.OnTop(world, BadoonBoards.Warlord);
        var lieutenant = BadoonBoards.OnTop(world, BadoonBoards.Lieutenant);
        Prompt? asked = null;

        Reveal(world, prompt =>
        {
            if (prompt.Trigger != MinionEntryBatchOperation.ChooseEngagedPlayer) return null;
            asked = prompt;
            return Decision.Take(world.Seats[1].IdentityCard.ObjectId);
        });

        Assert.NotNull(asked);
        Assert.Equal(world.FirstPlayer, asked.Player);
        Assert.Equal([world.Seats[0].IdentityCard.ObjectId, world.Seats[1].IdentityCard.ObjectId],
            asked.Affordances.Select(offer => offer.Id));
        Assert.Equal(PlayArea.Of(0), lieutenant.Area.PlayArea);
        Assert.Equal(PlayArea.Of(1), warlord.Area.PlayArea);
        Assert.Equal(DeckType.EngagedEnemiesArea, warlord.Area.Type);
    }

    [Fact]
    [Rule("rr:encounter-deck.2")]
    public void TheDiscardStopsWhenTheEncounterDeckEmpties()
    {
        // Two cards remain of the four. The reset shuffles the discard pile, Vulture included,
        // into a new deck that this instruction does not reach.
        var world = BadoonBoards.Board(villain: BadoonBoards.DrangThree);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        foreach (var card in deck.Cards.Skip(2).ToList()) World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        world.CreateCard(Vulture, world.AreaOf(DeckType.EncounterDiscardPile));

        var events = Reveal(world);

        Assert.Equal(2, events.OfType<CardsMoved>().Count(move => move.Verb == "Discard"));
        Assert.Equal(3, deck.Cards.Count);
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
    }

    [Fact]
    [Rule("rr:teamwork")]
    public void TeamworkReadsTheBoardAfterEveryMinionHasEngaged()
    {
        // Murray Reese and Wade Cole both print Teamwork (REAVER). Murray is alone when he
        // engages; by the time the batch's keywords resolve Wade has joined him, so both activate.
        var world = BadoonBoards.Board(villain: BadoonBoards.DrangThree);
        var second = BadoonBoards.OnTop(world, WadeCole);
        var first = BadoonBoards.OnTop(world, MurrayReese);

        var events = Reveal(world);

        Assert.Equal([first.ObjectId, second.ObjectId], events.OfType<AttackCompleted>().Select(attack => attack.Enemy));
        Assert.Equal(4, world.Seats[0].IdentityCard.Damage);
    }

    private static List<GameEvent> Reveal(World world, Func<Prompt, Decision?>? answer = null)
    {
        GrootBoards.Runner(world).ExecuteReveal(world, GrootBoards.Villain(world), 0);
        return BadoonBoards.Drive(world, answer);
    }

    private static int Moved(List<GameEvent> events, Card card, string verb) => events.FindIndex(change =>
        change is CardsMoved move && move.Verb == verb && move.Cards.Any(landing => landing.Card == card.ObjectId));
}
