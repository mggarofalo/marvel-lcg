using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>
/// Allies attacking, thwarting and defending — <c>rr:ally</c>.
/// </summary>
/// <remarks>
/// An ally is a character a player controls that is <b>not</b> their identity,
/// and <c>rr:ally.5</c> says so in as many words: attacks and thwarts "that
/// resolve from allies in play under a player's control are not considered to
/// be performed by that player's identity". Almost everything here follows from
/// that sentence.
/// </remarks>
public sealed class AllyTests
{
    [Rule("rr:ally.2")]
    [Rule("rr:player-turn.4")]
    [Fact]
    public void AnAllyExhaustsToAttackAndDealsItsAttackValue()
    {
        // "During a player's turn, they may use any number of allies they
        // control to attack or thwart. An ally **must exhaust** to attack."
        var printed = Cards();
        var world = Board(printed);
        var ally = Ally(world, "ally");
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        BasicPowers.AllyPower(world, printed, ally, villain, BasicPowers.AttackVerb, []);
        Agendas.Finish(world, printed);

        Assert.False(ally.Ready);
        Assert.Equal(2, villain.Damage);
    }

    [Rule("rr:ally.5")]
    [Fact]
    public void AnAllyCanActWhileItsControllerIsInAlterEgoForm()
    {
        // "Attacks [...] that resolve from allies in play under a player's
        // control are **not** considered to be performed by that player's
        // identity." So `rr:player-turn.3`'s form gate is the identity's and
        // does not reach an ally -- and an exhausted identity does not stop one
        // either.
        var printed = Cards();
        var world = Board(printed, hero: false);
        var ally = Ally(world, "ally");
        world.Seats[0].IdentityCard.Exhaust();
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        BasicPowers.AllyPower(world, printed, ally, villain, BasicPowers.AttackVerb, []);
        Agendas.Finish(world, printed);

        Assert.Equal(2, villain.Damage);
    }

    [Rule("rr:ally.3")]
    [Rule("rr:consequential-damage")]
    [Theory]
    // "After an ally is used to attack or thwart, deal consequential damage to
    // that ally equal to the number of consequential damage icons beneath the
    // ally's ATK or THW field." The icons are per field, so an ally with one
    // under ATK and none under THW takes damage attacking and none thwarting.
    [InlineData(BasicPowers.AttackVerb, 1)]
    [InlineData(BasicPowers.ThwartVerb, 0)]
    public void ConsequentialDamageComesFromTheFieldThatWasUsed(string verb, int expected)
    {
        var printed = Cards();
        var world = Board(printed);
        var ally = Ally(world, "ally");
        var target = verb == BasicPowers.AttackVerb
            ? world.TheCardIn(DeckType.VillainArea)!
            : Threatened(world);

        BasicPowers.AllyPower(world, printed, ally, target, verb, []);
        Agendas.Finish(world, printed);

        Assert.Equal(expected, ally.Damage);
    }

    [Rule("rr:ally.1")]
    [Fact]
    public void AnAllyItsOwnConsequentialDamageDefeatsIsDiscarded()
    {
        // "If an ally's remaining hit points are reduced to zero, it is
        // defeated and discarded from play." One hit point and one icon, so
        // attacking once is the end of it -- and `rr:consequential-damage` is
        // dealt to the ally by the same `Damage.Deal` an enemy uses.
        var printed = Cards();
        var world = Board(printed);
        var ally = Ally(world, "fragile");
        world.CreateCard("ally", world.Seats[0].Deck);

        var events = new List<GameEvent>();
        BasicPowers.AllyPower(
            world, printed, ally, world.TheCardIn(DeckType.VillainArea)!,
            BasicPowers.AttackVerb, events);
        events.AddRange(Agendas.Finish(world, printed));

        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Contains(events.OfType<CardsMoved>(), moved =>
            moved.Verb == "Defeat"
            && moved.Cards.Any(landing => landing.Card == ally.ObjectId));
    }

    [Rule("rr:exhausted.1")]
    [Rule("rr:exhausted.2")]
    [Fact]
    public void AnExhaustedAllyCannotActAndIsNotOffered()
    {
        // "An exhausted card cannot be exhausted again until it is ready."
        // An ally must exhaust to attack or thwart, so neither action is legal.
        var printed = Cards();
        var world = Board(printed);
        var ally = Ally(world, "ally");
        ally.Exhaust();

        Assert.Empty(BasicPowers.Allies(world, 0));

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => BasicPowers.AllyPower(
                world, printed, ally, world.TheCardIn(DeckType.VillainArea)!,
                BasicPowers.AttackVerb, []));

        Assert.Contains("is exhausted", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:attack-player-ability-type.4")]
    [Fact]
    public void AnAllyCannotAttackSomethingThatIsNotALegalTarget()
    {
        // The same target list a hero gets, guard and all -- the restriction is
        // on the attack, not on who is making it.
        var printed = Cards();
        var world = Board(printed);
        var ally = Ally(world, "ally");
        var scheme = Threatened(world);

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => BasicPowers.AllyPower(
                world, printed, ally, scheme, BasicPowers.AttackVerb, []));

        Assert.Contains("is not something", thrown.Message, StringComparison.Ordinal);
        Assert.True(ally.Ready);
    }

    [Rule("rr:ally.4")]
    [Rule("rr:defend-defense.3")]
    [Fact]
    public void AnAllyDefendingDoesNotReduceDamageByItsDefense()
    {
        // `rr:defend-defense.2` gives the reduction to a **hero** using its
        // basic defense power. `.3` is a different clause with no reduction in
        // it: "an ally can exhaust to defend against an enemy attack. Damage
        // from the attack is dealt to that ally."
        //
        // No printed ally has a DEF, so this ally is given one on purpose --
        // otherwise the two readings cannot be told apart.
        var printed = Cards();
        var world = Board(printed);
        var ally = Ally(world, "tough");
        var events = new List<GameEvent>();

        Attack.Initiate(
            world, printed, new PhaseStep(Steps.Attack, 1, 2, Subject: Villain(world), Seat: 0), []);
        var asked = Sequence.Work(world, printed, new NoCardAbilities(), events);
        Sequence.Answer(
            world, printed, new NoCardAbilities(), asked!, Decision.Take(ally.ObjectId), events);
        Sequence.Finish(world, printed, new NoCardAbilities(), events);

        // ATK 3 against an ally printing DEF 2. All three land.
        Assert.Equal(3, ally.Damage);
    }

    private static int Villain(World world) => world.TheCardIn(DeckType.VillainArea)!.ObjectId;

    private static Card Ally(World world, string faceId) =>
        world.CreateCard(faceId, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));

    /// <summary>The main scheme, with threat on it so it can be thwarted.</summary>
    private static Card Threatened(World world)
    {
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        if (scheme.Tokens.GetValueOrDefault("k_threat") == 0)
        {
            scheme.PlaceTokens("k_threat", 5);
        }

        return scheme;
    }

    private static World Board(Printed printed, bool hero = true)
    {
        var world = new World(printed, players: 1);
        world.CreateSeat("p0");
        var identity = world.CreateCard("alterego,hero", world.Seats[0].Hero);
        world.Seats[0].IdentityCard = identity;
        if (hero)
        {
            identity.TurnTo("hero");
        }

        world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("scheme", world.AreaOf(DeckType.MainSchemesArea));
        world.CreateCard("filler", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        return world;
    }

    private static Printed Cards() => new Printed()
        .With("villain", ("ATK", "3"), ("HP", "20"))
        .With("hero", ("HP", "10"), ("DEF", "9"))
        .With("alterego", ("HP", "10"))
        .With("ally", ("HP", "4"), ("ATK", "2"), ("THW", "2"), ("AtkIcons", "1"))
        .With("fragile", ("HP", "1"), ("ATK", "2"), ("AtkIcons", "1"))
        .With("tough", ("HP", "9"), ("ATK", "1"), ("DEF", "2"));

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
            "villain" => CardKind.EncounterVillain,
            "scheme" => CardKind.MainScheme,
            "boost" => CardKind.Treachery,
            _ => CardKind.Ally,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            attributes.TryGetValue(faceId, out var found)
                ? found
                : new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? value)
            && long.TryParse(value, out long number)
                ? number
                : fallback;

        /// <summary>Stated directly, rather than as stars inside ATK/THW.</summary>
        public long ConsequentialDamage(string faceId, string attribute) =>
            attribute == "ATK" ? PrintedValue(faceId, "AtkIcons", 1) : 0;
    }
}
