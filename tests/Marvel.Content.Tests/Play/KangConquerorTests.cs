using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual Kang (The Conqueror) stages I and III and the full authored book on a
// nonpublic board; Kang's public admission stays closed.
public sealed class KangConquerorTests : CoreCardTestBase
{
    [Theory]
    [InlineData("11001", 0, 2)]
    [InlineData("11001", 1, 2)]
    [InlineData("11034", 1, 3)]
    [InlineData("11006", 0, 2)]
    [InlineData("11039", 1, 3)]
    [Rule("rr:attack-enemy-activation.1.4")]
    [Rule("rr:interrupt")]
    public void TheAttackedPlayerChoosesThreatOnTheMainSchemeOrTwoMoreAttack(string face, int option, int attack)
    {
        // 11001/11006/11034/11039: "Forced Interrupt: When Kang attacks you,
        // either place 1 threat on the main scheme, or he gets +2 ATK for this
        // attack." rr:attack-enemy-activation.1.4: "When/After [enemy] attacks
        // you" abilities "are resolved when/after a player is attacked"; that
        // player chooses (MARVEL-894 Q3). Player 1 is attacked and is not the
        // first player.
        var (world, f, kang, main) = Board(face);
        long threat = main.Tokens.GetValueOrDefault("k_threat");
        var askers = new List<int>();
        KangTemporalFixture.Attack(world, kang, 1);

        var events = f.Finish(prompt =>
        {
            askers.Add(prompt.Player);
            Assert.Equal(2, prompt.Affordances.Count);
            return Decision.Take(prompt.Affordances[option].Id);
        });

        Assert.Equal([1], askers);
        Assert.Equal(threat + (option == 0 ? 1 : 0), main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(attack + (option == 1 ? 2 : 0), world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Single(events.OfType<AttackCompleted>(), e => e.Enemy == kang.ObjectId);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:attack-enemy-activation.5")]
    [Rule("rr:attack-enemy-activation.step.1")]
    public void TheChoiceIsMadeBeforeTheBoostCardIsDealt()
    {
        // rr:attack-enemy-activation.5: "Interrupts that trigger 'when [enemy
        // name] attacks' have the same timing as interrupts that trigger 'when
        // [the villain/an enemy] initiates an attack.'" They precede step 1,
        // "Give boost card", so the deck is untouched when the player chooses.
        var (world, f, kang, _) = Board("11001");
        var deck = world.AreaOf(DeckType.EncounterDeck);
        int before = deck.Cards.Count;
        int asked = 0;
        KangTemporalFixture.Attack(world, kang, 0);

        f.Finish(prompt =>
        {
            asked++;
            Assert.Equal(before, deck.Cards.Count);
            Assert.All(world.Areas.Where(area => area.Type == DeckType.BoostingArea), area => Assert.Empty(area.Cards));
            return Decision.Take(prompt.Affordances[0].Id);
        });

        Assert.Equal(1, asked);
        Assert.Equal(before - 1, deck.Cards.Count);
    }

    [Fact]
    [Rule("rr:attack-enemy-activation.1.4")]
    public void TheExtraAttackLastsOnlyForTheAttackThatChoseIt()
    {
        // "he gets +2 ATK for this attack": the next attack starts from printed ATK.
        var (world, f, kang, _) = Board("11001");
        KangTemporalFixture.Attack(world, kang, 0);
        f.Finish(prompt => Decision.Take(prompt.Affordances[1].Id));
        KangTemporalFixture.Attack(world, kang, 0);
        f.Finish(prompt => Decision.Take(prompt.Affordances[0].Id));

        Assert.Equal((2 + 2) + 2, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void AnotherEnemysAttackDoesNotTriggerKang()
    {
        // The trigger is "When Kang attacks you", not any attack against you.
        var (world, f, _, main) = Board("11001");
        long threat = main.Tokens.GetValueOrDefault("k_threat");
        var minion = KangTemporalFixture.Engaged(world, "01101", 0);
        KangTemporalFixture.Attack(world, minion, 0);

        f.Finish();

        Assert.Equal(threat, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }

    [Theory]
    [InlineData("11006")]
    [InlineData("11039")]
    [Rule("rr:when-defeated-abilities.1")]
    public void DefeatingKangThreeWinsTheGame(string face)
    {
        // 11006/11039: "When Defeated: The players win the game."
        // rr:when-defeated-abilities.2.1: "A defeated card leaves play after its
        // When Defeated ability is resolved." The game ends inside that ability,
        // so Kang III never leaves play; an empty villain deck would instead win
        // only after he had gone.
        var (world, _, kang, _) = Board(face);
        Agendas.Happening(world);
        Defeat.Character(world, Cards, kang, "prepared defeat", []);
        Sequence.Finish(world, Cards, world.Abilities, []);

        Assert.Equal(Outcome.PlayersWin, world.Result);
        Assert.Equal(DeckType.VillainArea, kang.Area.Type);
    }

    [Theory]
    [InlineData("11001")]
    [InlineData("11034")]
    [Rule("rr:when-defeated-abilities.2.1")]
    public void DefeatingKangOneRetainsTheAdvanceUntilThePhaseEndsWithoutWinning(string face)
    {
        // 11001/11034: "When Defeated: Advance the main scheme to stage 2 at the
        // end of the phase." pack:mc11:setup: Kang I is the only villain in the
        // villain deck, so his defeat neither wins nor promotes a next stage.
        var (world, _, kang, main) = Board(face);
        Agendas.Happening(world);
        Defeat.Character(world, Cards, kang, "prepared defeat", []);
        Sequence.Finish(world, Cards, world.Abilities, []);

        Assert.Equal(kang.ObjectId, world.Agenda.KangSplit.Pending?.Card);
        Assert.Equal(DeckType.RemovedArea, kang.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.VillainArea).Cards);
        Assert.Equal(DeckType.MainSchemesArea, main.Area.Type);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Fact]
    public void ThePhaseEndAdvanceRefusesAtTheUnauthoredStageTwoScheme()
    {
        // Stage 2A's own text belongs to the later stage packet. Reaching it with
        // the authored book raises instead of splitting the table silently.
        var (world, _, kang, _) = Board("11001");
        var aside = world.AreaOf(DeckType.AsideDeck);
        world.CreateCard("11008a,11008b", aside);
        for (int index = 0; index < 4; index++)
        {
            world.CreateCard($"{11009 + index}a,{11009 + index}b", aside);
            world.CreateCard((11002 + index).ToString(System.Globalization.CultureInfo.InvariantCulture), aside);
        }
        Agendas.Happening(world);
        Defeat.Character(world, Cards, kang, "prepared defeat", []);
        Sequence.Finish(world, Cards, world.Abilities, []);
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 1, 4));

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => Sequence.Finish(world, Cards, world.Abilities, []));
        Assert.Contains("11008a", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("no ability data", thrown.Message, StringComparison.Ordinal);
    }

    private static (World World, RedSkullFixture Fixture, Card Kang, Card Main) Board(string face)
    {
        var world = new World(Cards, 2, 0);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == 0 ? "01010a,01010b" : "01019a,01019b", seat.Hero);
            for (int i = 0; i < 12; i++) world.CreateCard("01088", seat.Deck);
        }
        bool final = face is "11006" or "11039";
        var main = world.CreateCard(final ? "11013a,11013b" : "11007a,11007b", world.AreaOf(DeckType.MainSchemesArea));
        main.TurnTo(final ? "11013b" : "11007b");
        var kang = world.CreateCard(face, world.AreaOf(DeckType.VillainArea));
        // Hard to Keep Down prints no boost field, so every boost adds nothing.
        for (int i = 0; i < 6; i++) world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        return (world, new RedSkullFixture(world, runner), kang, main);
    }
}
