using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>
/// Stunned and confused, and the two keywords that argue with them.
/// </summary>
/// <remarks>
/// <c>rr:status-cards</c> names three, and Tough was the only one with any
/// effect. These two are the other pair, and <c>rr:stalwart</c> and
/// <c>rr:steady</c> are what make "carrying a status card" and "being that
/// status" two different questions.
/// </remarks>
public sealed class StatusTests
{
    [Rule("rr:stun-stunned.1")]
    [Rule("rr:stun-stunned.5")]
    [Fact]
    public void AStunnedHeroExhaustsAttacksNothingAndLosesTheStun()
    {
        // "**Forced Interrupt**: when this character would attack, remove each
        // stunned status card from it instead", and `.5`: "costs associated
        // with the attack attempt, **including exhausting the character**, must
        // still be paid."
        var printed = Cards();
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, hero, Statuses.Stunned);

        BasicPowers.BasicAttack(world, printed, 0, villain, []);

        Agendas.Finish(world, printed);

        Assert.False(hero.Ready);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(0, Statuses.Count(world, hero, Statuses.Stunned));
    }

    [Rule("rr:attack-player-ability-type.1.1")]
    [Rule("rr:stun-stunned.5.1")]
    [Fact]
    public void AStunnedCharacterCanAttackWithNoLegalTarget()
    {
        // "A character can only initiate a basic attack if there is an enemy
        // that can be attacked **or if that character is stunned**", and
        // `.5.1` says the same from the other side. Attacking nothing is how
        // the stun comes off.
        var printed = Cards().With("minion", ("HP", "3"), ("Guard", "1"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        // A guarding minion makes the villain an illegal target.
        world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.DoesNotContain(
            BasicPowers.Attackable(world, printed, 0), card => card.ObjectId == villain.ObjectId);

        Statuses.Give(world, hero, Statuses.Stunned);
        BasicPowers.BasicAttack(world, printed, 0, villain, []);
        Agendas.Finish(world, printed);

        Assert.Equal(0, villain.Damage);
        Assert.Equal(0, Statuses.Count(world, hero, Statuses.Stunned));
    }

    [Rule("rr:confuse-confused.1")]
    [Rule("rr:confuse-confused.5")]
    [Fact]
    public void AConfusedHeroThwartsNothingAndLosesTheConfusion()
    {
        // "Discard the confused card instead. Costs associated with the thwart
        // attempt, including exhausting the character, must still be paid."
        var printed = Cards();
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 5);
        Statuses.Give(world, hero, Statuses.Confused);

        BasicPowers.BasicThwart(world, printed, 0, scheme, []);

        Assert.False(hero.Ready);
        Assert.Equal(5, scheme.Tokens["k_threat"]);
        Assert.Equal(0, Statuses.Count(world, hero, Statuses.Confused));
    }

    [Rule("rr:stun-stunned.1")]
    [Fact]
    public void AStunReplacesACardAbilityAttackAfterItsCostsArePaid()
    {
        var printed = Cards();
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var eventCard = world.CreateCard(
            "event", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        Statuses.Give(world, hero, Statuses.Stunned);

        BasicPowers.CardAttack(world, printed, 0, eventCard, villain, 8, "event", []);
        Agendas.Finish(world, printed);

        Assert.Equal(0, villain.Damage);
        Assert.True(hero.Ready);
        Assert.False(Statuses.Has(world, hero, Statuses.Stunned));
    }

    [Rule("rr:confuse-confused.1")]
    [Fact]
    public void AConfusionReplacesACardAbilityThwartAfterItsCostsArePaid()
    {
        var printed = Cards();
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var eventCard = world.CreateCard(
            "event", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        scheme.PlaceTokens("k_threat", 5);
        Statuses.Give(world, hero, Statuses.Confused);

        BasicPowers.CardThwart(world, printed, 0, eventCard, scheme, 4, "event", []);
        Agendas.Finish(world, printed);

        Assert.Equal(5, scheme.Tokens["k_threat"]);
        Assert.True(hero.Ready);
        Assert.False(Statuses.Has(world, hero, Statuses.Confused));
    }

    [Rule("rr:stun-stunned.1")]
    [Rule("rr:stun-stunned.6")]
    [Fact]
    public void AStunnedEnemyDoesNotAttackAtAll()
    {
        // "If a stunned villain or minion would attack, discard the stunned
        // status card instead." So none of the attack's six steps happens, and
        // the boost card it would have been given stays on the encounter deck.
        var printed = Cards();
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        Statuses.Give(world, villain, Statuses.Stunned);

        Attack.Initiate(
            world, printed,
            new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0), []);
        Sequence.Finish(world, printed, new NoCardAbilities(), []);

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Single(world.AreaOf(DeckType.EncounterDeck).Cards);
        Assert.Equal(0, Statuses.Count(world, villain, Statuses.Stunned));
    }

    [Rule("rr:confuse-confused.1")]
    [Rule("rr:confuse-confused.6")]
    [Fact]
    public void AConfusedEnemyPlacesNoThreat()
    {
        var printed = Cards();
        var world = Board(printed);

        // "If a confused villain or minion would scheme, discard the confused
        // status card instead." Alter-ego form makes the villain attempt that
        // scheme.
        world.Seats[0].IdentityCard.TurnTo("alterego");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        Statuses.Give(world, villain, Statuses.Confused);

        VillainPhase.Schedule(world.Agenda, round: 1);
        Sequence.Finish(world, printed, new NoCardAbilities(), []);

        // The main scheme's own acceleration only, and the villain's SCH of 2
        // never lands.
        Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
        Assert.Equal(0, Statuses.Count(world, villain, Statuses.Confused));
    }

    [Rule("rr:stalwart")]
    [Rule("rr:stalwart.1")]
    [Rule("rr:confuse-confused.4")]
    [Rule("rr:stun-stunned.4")]
    [Fact]
    public void AStalwartCharacterCannotBeStunnedOrConfused()
    {
        // "If a character has an ability stating that it 'cannot be confused'"
        // or "cannot be stunned", that status cannot be placed. Stalwart is
        // exactly those two constant abilities. Tough is unaffected.
        var printed = Cards().With("minion", ("HP", "3"), ("Stalwart", "1"));
        var world = Board(printed);
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Assert.Null(Statuses.Inflict(world, printed, minion, Statuses.Stunned));
        Assert.Null(Statuses.Inflict(world, printed, minion, Statuses.Confused));
        Assert.NotNull(Statuses.Inflict(world, printed, minion, Statuses.Tough));
    }

    [Rule("rr:status-cards.1")]
    [Fact]
    public void ACharacterCannotHoldTwoOfOneStatus()
    {
        // "A character cannot have more than one status card of each type at a
        // time."
        var printed = Cards().With("minion", ("HP", "3"));
        var world = Board(printed);
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Assert.NotNull(Statuses.Inflict(world, printed, minion, Statuses.Stunned));
        Assert.Null(Statuses.Inflict(world, printed, minion, Statuses.Stunned));
    }

    [Rule("rr:steady")]
    [Rule("rr:steady.1")]
    [Rule("rr:confuse-confused.3.1")]
    [Rule("rr:stun-stunned.3.1")]
    [Theory]
    [InlineData(Statuses.Stunned)]
    [InlineData(Statuses.Confused)]
    public void ASteadyCharacterNeedsTwoStatusCardsToBeAfflicted(string status)
    {
        // A steady character is stunned or confused "only if it has two"
        // corresponding status cards, and `rr:status-cards.1.1` lets it hold
        // that second card.
        var printed = Cards().With("minion", ("HP", "3"), ("Steady", "1"));
        var world = Board(printed);
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Assert.NotNull(Statuses.Inflict(world, printed, minion, status));
        Assert.False(Statuses.Afflicted(world, printed, minion, status));

        Assert.NotNull(Statuses.Inflict(world, printed, minion, status));
        Assert.True(Statuses.Afflicted(world, printed, minion, status));

        // And no third.
        Assert.Null(Statuses.Inflict(world, printed, minion, status));
    }

    [Rule("rr:steady")]
    [Fact]
    public void ASteadyCharacterLosesBothCardsWhenItsAttackIsCancelled()
    {
        // "After that character's attack, scheme, or thwart is canceled by a
        // status card effect, remove **all** status cards of the corresponding
        // type" -- which is `rr:stun-stunned.1`'s "remove **each**", and the
        // opposite of `rr:tough.2.1`'s one at a time.
        var printed = Cards().With("hero", ("ATK", "2"), ("HP", "10"), ("Steady", "1"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, hero, Statuses.Stunned);
        Statuses.Give(world, hero, Statuses.Stunned);

        BasicPowers.BasicAttack(world, printed, 0, villain, []);

        Agendas.Finish(world, printed);

        Assert.Equal(0, villain.Damage);
        Assert.Equal(0, Statuses.Count(world, hero, Statuses.Stunned));
    }

    [Rule("rr:ally.3")]
    [Fact]
    public void AStunnedAllyTakesNoConsequentialDamage()
    {
        // `rr:ally.3`'s parenthesis: "if an ally attempts to attack or thwart
        // while stunned or confused, respectively, that ally will **not** take
        // consequential damage."
        var printed = Cards().With("ally", ("HP", "4"), ("ATK", "2"), ("AtkIcons", "1"));
        var world = Board(printed);
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Statuses.Give(world, ally, Statuses.Stunned);

        BasicPowers.AllyPower(
            world, printed, ally, world.TheCardIn(DeckType.VillainArea)!,
            BasicPowers.AttackVerb, []);

        Assert.False(ally.Ready);
        Assert.Equal(0, ally.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:piercing")]
    [Rule("rr:piercing.1")]
    [Fact]
    public void PiercingDiscardsToughBeforeTheDamageLands()
    {
        // "Before this attack deals damage to a character, discard each tough
        // status card from that character." So the damage lands rather than
        // being eaten -- which is the whole point, and the opposite of what
        // `rr:tough.2` does on its own.
        var printed = Cards().With("minion", ("HP", "9"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Statuses.Give(world, minion, Statuses.Tough);

        Grant(world, hero, Marvel.Rules.Timing.Keywords.Piercing);
        BasicPowers.BasicAttack(world, printed, 0, minion, []);
        Agendas.Finish(world, printed);

        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
        Assert.Equal(2, minion.Damage);
    }

    [Rule("rr:piercing.2")]
    [Fact]
    public void PiercingDiscardsNothingWhenTheAttackDealsNoDamage()
    {
        // "If an attack with the piercing keyword would deal no damage to the
        // attacked character, it does not discard tough status cards."
        var printed = Cards().With("hero", ("ATK", "0"), ("HP", "10"))
            .With("minion", ("HP", "9"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Statuses.Give(world, minion, Statuses.Tough);

        Grant(world, hero, Marvel.Rules.Timing.Keywords.Piercing);
        BasicPowers.BasicAttack(world, printed, 0, minion, []);
        Agendas.Finish(world, printed);

        Assert.True(Statuses.Has(world, minion, Statuses.Tough));
    }

    [Rule("rr:piercing.1")]
    [Fact]
    public void PiercingDiscardsEveryToughCard()
    {
        // "Discard **each** tough status card from that character" -- all of
        // them, which is the opposite of `rr:tough.2.1`'s one at a time. Two
        // cards would otherwise eat two attacks.
        var printed = Cards().With("minion", ("HP", "9"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Statuses.Give(world, minion, Statuses.Tough);
        Statuses.Give(world, minion, Statuses.Tough);

        Grant(world, hero, Marvel.Rules.Timing.Keywords.Piercing);
        BasicPowers.BasicAttack(world, printed, 0, minion, []);
        Agendas.Finish(world, printed);

        Assert.Equal(0, Statuses.Count(world, minion, Statuses.Tough));
        Assert.Equal(2, minion.Damage);
    }

    [Rule("rr:overkill")]
    [Fact]
    public void AnAttackWithoutOverkillSpillsNothing()
    {
        // The excess simply goes away. Six damage against two hit points and
        // the villain is untouched.
        var printed = Cards().With("hero", ("ATK", "6"), ("HP", "10"))
            .With("minion", ("HP", "2"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        BasicPowers.BasicAttack(world, printed, 0, minion, []);

        Agendas.Finish(world, printed);

        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:ranged")]
    [Fact]
    public void RangedIgnoresRetaliate()
    {
        var printed = Cards().With("minion", ("HP", "9"), ("Retaliate", "3"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Grant(world, hero, Marvel.Rules.Timing.Keywords.Ranged);
        BasicPowers.BasicAttack(world, printed, 0, minion, []);
        Agendas.Finish(world, printed);

        Assert.Equal(2, minion.Damage);
        Assert.Equal(0, hero.Damage);
    }

    [Rule("rr:overkill")]
    [Rule("rr:overkill.1")]
    [Rule("rr:excess-damage")]
    [Fact]
    public void OverkillCarriesTheExcessFromADefeatedMinionToTheVillain()
    {
        // "Excess damage is any amount of damage [...] beyond that character's
        // remaining hit points." If overkill defeats a minion, that excess is
        // dealt to the villain: six against two is four beyond.
        var printed = Cards().With("hero", ("ATK", "6"), ("HP", "10"))
            .With("minion", ("HP", "2"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Grant(world, hero, Marvel.Rules.Timing.Keywords.Overkill);
        BasicPowers.BasicAttack(world, printed, 0, minion, []);
        Agendas.Finish(world, printed);

        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(4, villain.Damage);
    }

    [Rule("rr:overkill.1")]
    [Fact]
    public void OverkillCarriesTheExcessFromADefeatedAllyToItsController()
    {
        // The other destination: "deal any damage on that ally beyond its hit
        // points to **the identity of the player who controls the ally**".
        var printed = Cards().With("villain", ("ATK", "7"), ("SCH", "2"), ("HP", "20"))
            .With("ally", ("HP", "3"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("encounter", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));

        Grant(world, villain, Marvel.Rules.Timing.Keywords.Overkill);
        Attack.Initiate(
            world, printed,
            new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0), []);
        var asked = Sequence.Work(world, printed, new NoCardAbilities(), []);
        Sequence.Answer(
            world, printed, new NoCardAbilities(), asked!, Decision.Take(ally.ObjectId), []);

        Sequence.Finish(world, printed, new NoCardAbilities(), []);

        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(4, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:overkill.1")]
    [Rule("rr:ownership-and-control.5")]
    [Fact]
    public void OverkillUsesAnAllysControllerRatherThanItsOwner()
    {
        // Overkill names "the identity of the player who controls the ally".
        // A card put into another player's play area is controlled there even
        // though defeat sends it to its owner's discard pile. The move caused
        // by defeat must not change the destination already named by overkill.
        var printed = Cards().With("villain", ("ATK", "7"), ("SCH", "2"), ("HP", "20"))
            .With("ally", ("HP", "3"));
        var world = Board(printed, players: 2);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 0));
        world.CreateCard("encounter", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));

        Grant(world, villain, Marvel.Rules.Timing.Keywords.Overkill);
        Attack.Initiate(
            world, printed,
            new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 1), []);
        var asked = Sequence.Work(world, printed, new NoCardAbilities(), []);
        Sequence.Answer(
            world, printed, new NoCardAbilities(), asked!, Decision.Take(ally.ObjectId), []);

        // The ally's controller becomes the target player; ownership only
        // decides which discard pile receives the ally after defeat.
        Assert.Equal(1, world.Attack!.Player);
        Sequence.Finish(world, printed, new NoCardAbilities(), []);

        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(4, world.Seats[1].IdentityCard.Damage);
    }

    [Rule("rr:overkill.4")]
    [Fact]
    public void OverkillCarriesNothingWhenAToughCardAteTheDamage()
    {
        // "If excess damage from an attack with overkill is prevented, that
        // damage is **not** dealt to the identity or villain." A tough status
        // card prevents all of it (`rr:tough.2`), so nothing spills.
        var printed = Cards().With("hero", ("ATK", "6"), ("HP", "10"))
            .With("minion", ("HP", "2"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Statuses.Give(world, minion, Statuses.Tough);

        Grant(world, hero, Marvel.Rules.Timing.Keywords.Overkill);
        BasicPowers.BasicAttack(world, printed, 0, minion, []);
        Agendas.Finish(world, printed);

        Assert.Equal(0, minion.Damage);
        Assert.Equal(0, villain.Damage);
    }

    /// <summary>Grants a keyword the way a card ability does.</summary>
    private static void Grant(World world, Card card, string keyword) =>
        world.Effects.Register(new Marvel.Rules.Timing.ContinuousEffect(
            Marvel.Rules.Timing.EffectSource.LastingEffect,
            Kind: keyword, Card: card.ObjectId, Affects: card.ObjectId));

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
        world.CreateCard("filler", world.Seats[0].Deck);
        return world;
    }

    private static Printed Cards() => new Printed()
        .With("hero", ("ATK", "2"), ("THW", "2"), ("HP", "10"))
        .With("villain", ("ATK", "3"), ("SCH", "2"), ("HP", "20"))
        .With("scheme", ("EscalationThreat", "1"), ("TargetThreat", "99"))
        .With("boost", ("Boost", "0"));

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
            "minion" => CardKind.Minion,
            "ally" => CardKind.Ally,
            "tough" or "stunned" or "confused" => CardKind.Status,
            _ => CardKind.Treachery,
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

        public long ConsequentialDamage(string faceId, string attribute) =>
            attribute == "ATK" ? PrintedValue(faceId, "AtkIcons", 1) : 0;
    }
}
