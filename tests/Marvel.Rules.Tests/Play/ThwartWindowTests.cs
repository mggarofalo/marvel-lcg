using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>
/// The windows around a thwart — <c>rr:thwart.1</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:thwart</c> writes out no numbered steps the way
/// <c>rr:attack-player-ability-type</c> does, so the case for a thwart having
/// windows at all is made by <c>rr:consequential-damage.1</c>: an ally's
/// consequential damage is dealt "after resolving abilities that are triggered
/// by the ally attacking <b>or thwarting</b>". A rule that orders something
/// after the abilities triggered by a thwart is a rule that takes for granted
/// there are such abilities and a place they go.
/// </para>
/// <para>
/// Before this, a thwart took its threat off inside the call that started it.
/// Nothing could be triggered by it, so nothing could be ordered around it —
/// which is why the ally's consequential damage was dealt inline and the
/// comment where it happened said so.
/// </para>
/// </remarks>
public sealed class ThwartWindowTests
{
    [Rule("rr:thwart.1")]
    [Rule("rr:response")]
    [Fact]
    public void AThwartOpensAResponseWindowAndTheThreatIsAlreadyOff()
    {
        // A response is "after", so what makes this a response window and not
        // an interrupt one is that the threat has gone by the time the player
        // is asked.
        var printed = new Printed().With("hero", ("THW", "2"));
        var world = Board(printed);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 5);
        var cards = new Responder(Steps.CharacterThwartsScheme);

        BasicPowers.BasicThwart(world, printed, 0, scheme, []);
        var asked = Sequence.Work(world, printed, cards, []);

        Assert.NotNull(asked);
        Assert.Equal(Steps.CharacterThwarts, world.Agenda.Current!.Value.What);
        Assert.Equal(Stage.Responses, world.Agenda.Stage);
        Assert.Equal(3, scheme.Tokens["k_threat"]);
    }

    [Rule("rr:interrupt.1")]
    [Fact]
    public void AnInterruptToAThwartRunsBeforeTheThreatComesOff()
    {
        // "An interrupt ability resolves **before** the triggering condition."
        // The same board and the same card, moved to the other window: the
        // threat is untouched when the player is asked.
        var printed = new Printed().With("hero", ("THW", "2"));
        var world = Board(printed);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 5);
        var cards = new Responder(Steps.CharacterThwartsScheme, WindowKind.Interrupt);

        BasicPowers.BasicThwart(world, printed, 0, scheme, []);
        var asked = Sequence.Work(world, printed, cards, []);

        Assert.NotNull(asked);
        Assert.Equal(Stage.Interrupts, world.Agenda.Stage);
        Assert.Equal(5, scheme.Tokens["k_threat"]);
    }

    [Rule("rr:you-your.6")]
    [Rule("rr:thwart.1")]
    [Fact]
    public void TheOccurrenceNamesTheSchemeAndTheSeatThatThwartedIt()
    {
        // Both ends, because a printed trigger can name either: "after **this
        // scheme** is thwarted" reads the subject, and "after **you** thwart"
        // reads the player. One field could not answer both.
        var printed = new Printed().With("hero", ("THW", "1"));
        var world = Board(printed, players: 2);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 5);

        BasicPowers.BasicThwart(world, printed, 1, scheme, []);
        var occurrence = world.Agenda.Occurrence!;

        Assert.Equal(scheme.ObjectId, occurrence.Subject);
        Assert.Equal(1, occurrence.Player);
    }

    [Rule("rr:consequential-damage.1")]
    [Fact]
    public void AnAllyTakesItsConsequentialDamageAfterTheThwartsWindowAndNotBefore()
    {
        // "Consequential damage is dealt to an ally **after resolving abilities
        // that are triggered by** the ally attacking or thwarting." So at the
        // moment the window opens the ally is undamaged, and it is damaged once
        // the window has closed. Dealt inline — as it was — the first of those
        // two assertions was false.
        var printed = new Printed()
            .With("ally", ("HP", "4"), ("THW", "2"), ("ThwIcons", "1"));
        var world = Board(printed);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 5);
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var cards = new Responder(Steps.CharacterThwartsScheme);

        BasicPowers.AllyPower(world, printed, ally, scheme, BasicPowers.ThwartVerb, []);
        var asked = Sequence.Work(world, printed, cards, []);

        Assert.NotNull(asked);
        Assert.Equal(3, scheme.Tokens["k_threat"]);
        Assert.Equal(0, ally.Damage);

        Agendas.Finish(world, printed, cards);

        Assert.Equal(1, ally.Damage);
    }

    [Rule("rr:assault.2")]
    [Fact]
    public void AnAllyThwartingAnAssaultedSchemeStillTakesTheIconUnderItsAttack()
    {
        // "It takes the consequential damage listed under its **ATK** instead
        // of its THW." Which field was used is read when the damage is dealt
        // rather than when the thwart was scheduled — `rr:ability.9` makes a
        // constant ability true only while its condition holds, and a step is
        // long enough for one to stop holding.
        var printed = new Printed()
            .With("ally", ("HP", "4"), ("ATK", "3"), ("THW", "2"), ("AtkIcons", "2"))
            .With("sideScheme", ("Assault", "1"));
        var world = Board(printed);
        var side = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 9);
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));

        BasicPowers.AllyPower(world, printed, ally, side, BasicPowers.ThwartVerb, []);
        Agendas.Finish(world, printed);

        Assert.Equal(6, side.Tokens["k_threat"]);
        Assert.Equal(2, ally.Damage);
    }

    [Rule("rr:thwart.1")]
    [Fact]
    public void TheEventStreamStillSaysAThwartIsWhatHappened()
    {
        // Scheduling changed when the threat comes off and must not change what
        // a client is told about it. An ally that thwarts takes its
        // consequential damage under the thwart's verb, not the attack's.
        var printed = new Printed()
            .With("ally", ("HP", "4"), ("THW", "2"), ("ThwIcons", "1"));
        var world = Board(printed);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 5);
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));

        BasicPowers.AllyPower(world, printed, ally, scheme, BasicPowers.ThwartVerb, []);
        var happened = Agendas.Finish(world, printed);

        var hurt = happened.OfType<FieldSet>().Single(
            set => set.Card == ally.ObjectId && set.Field == "health");
        Assert.Equal(BasicPowers.ThwartVerb, hurt.Trigger);
        Assert.Equal("Consequential_Damage", hurt.Verb);
    }

    [Rule("rr:thwart.1")]
    [Fact]
    public void AThwartStepWithNoThwartOnTheBoardIsRefusedRatherThanSkipped()
    {
        // The step and the thwart it resolves are two pieces of board state,
        // and they can only disagree if something scheduled one without the
        // other. Silence would leave the threat on the scheme and the game
        // carrying on as though it had come off — so the mismatch is named.
        var printed = new Printed().With("hero", ("THW", "2"));
        var world = Board(printed);

        Assert.Throws<RulesNotImplementedException>(
            () => BasicPowers.ResolveCharacterThwart(world, printed, []));
    }

    private static World Board(Printed printed, int players = 1)
    {
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
        return world;
    }

    /// <summary>One optional ability, in one window, on one named condition.</summary>
    private sealed class Responder(string condition, WindowKind kind = WindowKind.Response)
        : NoCardAbilities
    {
        /// <summary>The affordance id it offers its one ability under.</summary>
        public const int Handle = 91;

        /// <summary>The object id of the card carrying it.</summary>
        public const int Card = 4;

        public override IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window) =>
            window == kind && occurrence.Conditions.Contains(condition)
                ? [new PendingAbility(
                    Card,
                    kind == WindowKind.Interrupt ? AbilityType.Interrupt : AbilityType.Response,
                    0)]
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
            "sideScheme" => CardKind.EncounterSideScheme,
            "ally" => CardKind.Ally,
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

        /// <summary>Stated directly rather than as stars inside ATK/THW.</summary>
        public long ConsequentialDamage(string faceId, string attribute) =>
            PrintedValue(faceId, attribute == "ATK" ? "AtkIcons" : "ThwIcons", 1);
    }
}
