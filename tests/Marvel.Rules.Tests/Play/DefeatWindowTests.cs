using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>
/// The window around a defeat — <c>rr:triggering-condition.2</c>.
/// </summary>
/// <remarks>
/// <para>
/// A defeat has to be reachable by cards other than the one being defeated:
/// "after an ally is defeated" is a trigger a great many cards print, and the
/// defeated card's own <c>WhenDefeated</c> ability does not serve it.
/// </para>
/// <para>
/// It is served by <b>not</b> giving a defeat windows of its own.
/// <c>rr:triggering-condition.2</c> uses this exact case as its example — "a
/// single attack causing a character to both take damage and be defeated" is
/// handled "with a single interrupt window and a single response window" — so
/// the defeat joins the occurrence that caused it, and the window that was
/// already going to open covers both.
/// </para>
/// </remarks>
public sealed class DefeatWindowTests
{
    [Rule("rr:triggering-condition.2")]
    [Fact]
    public void ADefeatJoinsTheOccurrenceThatCausedItRatherThanGettingItsOwn()
    {
        // Both conditions on one occurrence, and no second step. An engine that
        // scheduled the defeat would show two: an ability answering both would
        // fire twice against what the rules call one moment.
        var world = Board(out var printed, out var minion);
        BasicPowers.BasicAttack(world, printed, 0, minion, []);
        Assert.Equal(1, world.Agenda.Count);

        Sequence.Work(world, printed, new Responder(Steps.CardDefeated), []);

        // The minion died and the agenda did not grow. The window that is now
        // open is the attack's, and it is the only one there will be.
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(1, world.Agenda.Count);
        Assert.Equal(Steps.CharacterAttacks, world.Agenda.Current!.Value.What);
    }

    [Rule("rr:triggering-condition.2")]
    [Rule("rr:response")]
    [Fact]
    public void TheDefeatIsAnsweredInTheCausingOccurrencesResponseWindow()
    {
        // The whole point, stated once: a card that has nothing to do with the
        // attack, and answers only "after a card is defeated", is offered — and
        // it is offered in the response window of the step that killed it.
        var world = Board(out var printed, out var minion);
        var cards = new Responder(Steps.CardDefeated);

        BasicPowers.BasicAttack(world, printed, 0, minion, []);
        var asked = Sequence.Work(world, printed, cards, []);

        Assert.NotNull(asked);
        Assert.Equal(Stage.Responses, world.Agenda.Stage);

        var occurrence = world.Agenda.Occurrence!;
        Assert.Contains(Steps.AttackInitiated, occurrence.Conditions);
        Assert.Contains(Steps.AttackEnds, occurrence.Conditions);
        Assert.Contains(Steps.DamageDealt, occurrence.Conditions);
        Assert.Contains(Steps.CardDefeated, occurrence.Conditions);
    }

    [Rule("rr:attack-player-ability-type.step.7")]
    [Rule("rr:damage.step.9")]
    [Fact]
    public void ACompletedAttackReportsItsEndWithoutInventingDamage()
    {
        // The end condition is learned after the attack applies, so it is
        // visible to responses and was absent from the interrupt window. The
        // damage condition is different: an attack for zero dealt no damage
        // and must not trigger "attacks and damages" text.
        var world = Board(out var printed, out var minion);
        printed.With("hero", ("ATK", "0"));
        var cards = new Responder(Steps.AttackEnds);

        BasicPowers.BasicAttack(world, printed, 0, minion, []);
        var asked = Sequence.Work(world, printed, cards, []);

        Assert.NotNull(asked);
        Assert.Equal(Stage.Responses, world.Agenda.Stage);
        Assert.Contains(Steps.AttackEnds, world.Agenda.Occurrence!.Conditions);
        Assert.DoesNotContain(Steps.DamageDealt, world.Agenda.Occurrence.Conditions);
    }

    [Rule("rr:interrupt.1")]
    [Fact]
    public void AttackCompletionFactsAreAbsentBeforeTheAttackApplies()
    {
        // An interrupt resolves before its triggering condition. End, damage,
        // and defeat facts are learned during Apply and must not be visible in
        // the occurrence's opening window as predictions about the future.
        var world = Board(out var printed, out var minion);

        BasicPowers.BasicAttack(world, printed, 0, minion, []);
        var occurrence = world.Agenda.Begin(world, printed);

        Assert.Contains(Steps.AttackInitiated, occurrence.Conditions);
        Assert.DoesNotContain(Steps.AttackEnds, occurrence.Conditions);
        Assert.DoesNotContain(Steps.DamageDealt, occurrence.Conditions);
        Assert.DoesNotContain(Steps.CardDefeated, occurrence.Conditions);
    }

    [Rule("rr:triggering-condition.2")]
    [Fact]
    public void TheProvenanceOutlastsTheCallThatMadeIt()
    {
        // A response is asked after the defeat and after everything else the
        // occurrence did, so who was defeated and how has to still be there. It
        // is on the occurrence rather than on the board precisely because the
        // occurrence is the thing whose life is the right length.
        var world = Board(out var printed, out var minion);

        BasicPowers.BasicAttack(world, printed, 0, minion, []);
        Sequence.Work(world, printed, new Responder(Steps.CardDefeated), []);

        var defeat = world.Agenda.Occurrence!.Defeat;
        Assert.NotNull(defeat);
        Assert.Equal(minion.ObjectId, defeat.Card);
        Assert.Equal(BasicPowers.AttackVerb, defeat.How);
        Assert.Equal(0, defeat.By);
    }

    [Rule("rr:ownership-and-control.2")]
    [Fact]
    public void WhoDefeatedItIsTheAttackersSeatAndNotTheFirstPlayer()
    {
        // One seat cannot tell those apart. Two can: the second player attacks,
        // and the defeat names them.
        var world = Board(out var printed, out var minion, players: 2);

        BasicPowers.BasicAttack(world, printed, 1, minion, []);
        Sequence.Work(world, printed, new Responder(Steps.CardDefeated), []);

        Assert.Equal(1, world.Agenda.Occurrence!.Defeat!.By);
    }

    [Rule("rr:triggering-condition.1")]
    [Fact]
    public void TwoDefeatsInOneOccurrenceRefuseToNameTheDefeatedCard()
    {
        // One effect that defeats two characters leaves nothing in the rules to
        // say which of them a response is about: `rr:triggering-condition.1`
        // lets the ability trigger once, and once is the wrong number for two.
        // Refused where the ambiguity bites, so that the multiple defeat itself
        // still resolves.
        var occurrence = new Occurrence(1, [Steps.CardDefeated]);
        occurrence.Also(new Defeated(7, -1, "test"));
        occurrence.Also(new Defeated(8, -1, "test"));

        Assert.Equal(2, occurrence.Defeats.Count);
        var thrown = Assert.Throws<RulesNotImplementedException>(() => occurrence.Defeat);
        Assert.Contains("2 cards were defeated", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:triggering-condition.2")]
    [Fact]
    public void ADefeatWithNothingHappeningIsRefusedRatherThanSilentlyWindowless()
    {
        // If this ever fires in a game, the missing piece is the *cause*: some
        // way of doing damage that this engine still performs as a call rather
        // than as a step, whose own windows are therefore missing too. Silence
        // would hide that inside the cards written to notice it.
        var world = Board(out var printed, out var minion);

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => Damage.Deal(world, printed, minion, minion, 5, "test", "test", []));

        Assert.Contains(
            "nothing is happening on the agenda", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:triggering-condition.2")]
    [Fact]
    public void OneOccurrenceNamesTheDefeatOnlyOnceHoweverManyDied()
    {
        // The rule is about which conditions the occurrence creates, not how
        // many times it created them. Two defeats is one `WhenCardDefeated`,
        // and `rr:triggering-condition.1` then lets each answering ability
        // trigger once.
        var occurrence = new Occurrence(1, ["WhenDamageDealt"]);
        occurrence.Also(new Defeated(7, -1, "test"));
        occurrence.Also(new Defeated(8, -1, "test"));

        Assert.Equal(["WhenDamageDealt", Steps.CardDefeated], occurrence.Conditions);
    }

    /// <summary>A board with a hero who can kill the minion in one hit.</summary>
    private static World Board(
        out Printed printed, out Card minion, int players = 1)
    {
        printed = new Printed().With("hero", ("ATK", "3")).With("minion", ("HP", "3"));
        var world = new World(printed, players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
            var identity = world.CreateCard("alterego,hero", world.Seats[seat].Hero);
            world.Seats[seat].IdentityCard = identity;
            identity.TurnTo("hero");
        }

        world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("scheme", world.AreaOf(DeckType.MainSchemesArea));
        minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(players - 1)));
        return world;
    }

    /// <summary>One optional response, on one named condition.</summary>
    private sealed class Responder(string condition) : NoCardAbilities
    {
        /// <summary>The affordance id it offers its one ability under.</summary>
        public const int Handle = 92;

        /// <summary>The object id of the card carrying it.</summary>
        public const int Card = 5;

        public override IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window) =>
            window == WindowKind.Response && occurrence.Conditions.Contains(condition)
                ? [new PendingAbility(Card, AbilityType.Response, 0)]
                : [];

        public override Affordance Describe(World world, PendingAbility ability) =>
            new(Handle, "Use", ability.Card, ability.Player, "a response");
    }

    /// <summary>Printed data for a handful of made-up cards.</summary>
    private sealed class Printed : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes =
            new(StringComparer.Ordinal);

        public Printed With(string faceId, params (string Key, string Value)[] values)
        {
            var table = attributes.TryGetValue(faceId, out var found)
                ? found
                : attributes[faceId] = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in values)
            {
                table[key] = value;
            }

            return this;
        }

        public CardKind Kind(string faceId) => faceId switch
        {
            "alterego" => CardKind.AlterEgo,
            "hero" => CardKind.Hero,
            "scheme" => CardKind.MainScheme,
            "minion" => CardKind.Minion,
            _ => CardKind.EncounterVillain,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            attributes.TryGetValue(faceId, out var found)
                ? found
                : new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out var printed)
            && long.TryParse(printed, out long value) ? value : fallback;
    }
}
