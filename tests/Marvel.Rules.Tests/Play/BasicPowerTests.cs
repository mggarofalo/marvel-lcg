using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>
/// The basic powers, and what damage does when it is enough.
/// </summary>
/// <remarks>
/// <c>rr:basic-power</c> lists five. Three are a player's to use on their turn
/// and are here; defence belongs to an enemy's attack and scheme is an
/// enemy's. None of it is reachable by the recorded milestone game, whose
/// sampling policy declines every decision.
/// </remarks>
public sealed class BasicPowerTests
{
    [Rule("rr:attack-player-ability-type.1")]
    [Fact]
    public void ABasicAttackExhaustsAndDealsTheCharactersAttackValue()
    {
        // "A character **must exhaust** to use this power. This deals damage
        // equal to the character's ATK value to the enemy."
        var printed = new Printed().With("hero", ("ATK", "3")).With("villain", ("HP", "10"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var events = new List<GameEvent>();

        BasicPowers.BasicAttack(world, printed, 0, villain, events);

        Agendas.Finish(world, printed);

        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.Equal(3, villain.Damage);
    }

    [Rule("rr:modifiers")]
    [Rule("rr:upgrade.2")]
    [Fact]
    public void ABasicAttackDealsTheModifiedAttackValue()
    {
        // "The character's ATK value", and `rr:modifiers` has the game
        // "constantly check and (if necessary) update the count of any variable
        // quantity that is being modified" -- so it is the value now, not the
        // one printed. An upgrade attached to the hero printing `ATK+ 2` is the
        // ordinary case, and 116 cards in the pool carry one.
        var printed = new Printed()
            .With("hero", ("ATK", "3"))
            .With("upgrade", ("ATK+", "2"))
            .With("villain", ("HP", "10"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var identity = world.Seats[0].IdentityCard;
        world.CreateCard(
            "upgrade",
            world.AreaOf(
                DeckType.UpgradesArea, identity.Area.PlayArea, identity.ObjectId, cardOwner: 0));

        BasicPowers.BasicAttack(world, printed, 0, villain, []);

        Agendas.Finish(world, printed);

        Assert.Equal(5, villain.Damage);
    }

    [Rule("rr:exhausted.2")]
    [Fact]
    public void AnExhaustedCharacterCannotUseABasicPower()
    {
        // "If an exhausted card must exhaust to pay the cost of using its
        // ability, that ability cannot be used until the card is ready."
        var printed = new Printed().With("hero", ("ATK", "3")).With("villain", ("HP", "10"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Seats[0].IdentityCard.Exhaust();

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => BasicPowers.BasicAttack(world, printed, 0, villain, []));

        Assert.Contains("is exhausted", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:player-turn.3")]
    [Fact]
    public void AnAlterEgoCannotAttackAndAHeroCannotRecover()
    {
        // "Use their alter-ego's basic recovery *(if in alter-ego form)* or
        // their hero's basic attack or thwart power *(if in hero form)*."
        var printed = new Printed().With("hero", ("ATK", "3")).With("villain", ("HP", "10"));
        var world = Board(printed, hero: false);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Seats[0].IdentityCard.TakeDamage(2);

        Assert.Throws<RulesNotImplementedException>(
            () => BasicPowers.BasicAttack(world, printed, 0, villain, []));

        world.Seats[0].IdentityCard.TurnTo("hero");

        Assert.Throws<RulesNotImplementedException>(
            () => BasicPowers.BasicRecovery(world, printed, 0, []));
    }

    [Rule("rr:guard.1")]
    [Fact]
    public void AGuardingMinionTakesEveryVillainOffTheList()
    {
        // "The engaged player cannot attack any villain." So the villain leaves
        // the list and the minions stay -- including the one guarding.
        var printed = new Printed()
            .With("hero", ("ATK", "3"))
            .With("villain", ("HP", "10"))
            .With("minion", ("HP", "3"), ("Guard", "1"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        Assert.Equal([villain.ObjectId], Ids(BasicPowers.Attackable(world, printed, 0)));

        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Assert.Equal([minion.ObjectId], Ids(BasicPowers.Attackable(world, printed, 0)));

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => BasicPowers.BasicAttack(world, printed, 0, villain, []));
        Assert.Contains("is not an enemy", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:guard")]
    [Fact]
    public void AMinionGuardingSomebodyElseDoesNotStopYou()
    {
        // "**While a minion with the guard keyword is engaged with a player**,
        // **that** player cannot use cards they control to attack a villain."
        // Guard is engagement-specific, not a board-wide effect.
        var printed = new Printed()
            .With("hero", ("ATK", "3"))
            .With("villain", ("HP", "10"))
            .With("minion", ("HP", "3"), ("Guard", "1"));
        var world = Board(printed, players: 2);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));

        Assert.Contains(villain.ObjectId, Ids(BasicPowers.Attackable(world, printed, 0)));
        Assert.DoesNotContain(villain.ObjectId, Ids(BasicPowers.Attackable(world, printed, 1)));
    }

    [Fact]
    public void EachQueuedCardAttackCarriesItsOwnPayload()
    {
        // The engine chooses to put the complete card attack on its agenda
        // step. A later nested attack may update the board's compatibility
        // snapshot, but it cannot rewrite an earlier occurrence.
        var printed = new Printed()
            .With("hero", ("ATK", "3"))
            .With("villain", ("HP", "10"))
            .With("minion", ("HP", "10"))
            .With("event");
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var source = world.CreateCard("event", world.Seats[0].Hand);
        var abilities = new RecordingCardPowers();
        world.Abilities = abilities;

        Assert.True(BasicPowers.CardAttack(
            world, printed, 0, source, villain, 5, "first", [], abilityIndex: 0));
        Assert.True(BasicPowers.CardAttack(
            world, printed, 0, source, minion, 7, "second", [], abilityIndex: 0));

        Agendas.Finish(world, printed, abilities);

        Assert.Equal([villain.ObjectId, minion.ObjectId], abilities.Targets);
        Assert.Equal([5, 7], abilities.Amounts);
    }

    [Rule("rr:thwart.1")]
    [Rule("rr:thwart.1.1")]
    [Fact]
    public void ABasicThwartExhaustsAndRemovesThreatButNeedsSomeToRemove()
    {
        // "This removes threat equal to the character's THW value from the
        // scheme", and `.1.1`: "a character can only initiate a basic thwart if
        // there is a scheme with **at least one threat** for the character to
        // remove."
        var printed = new Printed().With("hero", ("THW", "2"));
        var world = Board(printed);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;

        Assert.Empty(BasicPowers.Thwartable(world, printed, 0));

        scheme.PlaceTokens("k_threat", 5);
        BasicPowers.BasicThwart(world, printed, 0, scheme, []);
        Agendas.Finish(world, printed);

        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.Equal(3, scheme.Tokens["k_threat"]);
    }

    [Rule("rr:threat")]
    [Fact]
    public void ThwartCannotTakeMoreThreatThanIsThere()
    {
        // Threat is tokens, and a scheme cannot hold a negative number of them.
        var printed = new Printed().With("hero", ("THW", "4"));
        var world = Board(printed);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 1);

        var events = new List<GameEvent>();
        BasicPowers.BasicThwart(world, printed, 0, scheme, events);
        events.AddRange(Agendas.Finish(world, printed));

        Assert.Equal(0, scheme.Tokens["k_threat"]);

        // **And the event says so.** `Card.PlaceTokens` clamps at zero on its
        // own, so the board is right either way and only the wire is wrong: an
        // uncapped thwart reports the scheme going from 1 threat to -3, and a
        // client drawing from the event stream would believe it.
        var reported = events.OfType<FieldSet>().Single(set => set.Field == "k_threat");
        Assert.Equal(1, reported.From);
        Assert.Equal(0, reported.To);
    }

    [Rule("rr:cannot")]
    [Rule("rr:thwart.1.1")]
    [Fact]
    public void ACharacterCannotInitiateAThwartAgainstAProtectedScheme()
    {
        // "Cannot" is absolute. A scheme whose threat cannot be removed is
        // not one with threat "for the character to remove", so it is absent
        // from the legal targets rather than offered as a no-op.
        var printed = new Printed().With("hero", ("THW", "2"));
        var world = Board(printed);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 3);
        world.Abilities = new ProtectedScheme(scheme.ObjectId);

        Assert.Empty(BasicPowers.Thwartable(world, printed, 0));
        Assert.Throws<RulesNotImplementedException>(
            () => BasicPowers.BasicThwart(world, printed, 0, scheme, []));
        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.Equal(3, scheme.Tokens["k_threat"]);
    }

    [Rule("rr:cannot")]
    [Fact]
    public void AProtectedSchemeAlsoRefusesThreatRemovalFromAnEffect()
    {
        // The prohibition is checked by the shared primitive, not only by the
        // basic-power affordance. A card effect therefore cannot walk around
        // the word "cannot" by calling the token mutation directly.
        var printed = new Printed();
        var world = Board(printed);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 3);
        var abilities = new ProtectedScheme(scheme.ObjectId);
        world.Abilities = abilities;
        var events = new List<GameEvent>();

        long removed = Threat.Remove(
            world, printed, abilities, scheme, 2, "test", "Remove_Threat", events);

        Assert.Equal(0, removed);
        Assert.Equal(3, scheme.Tokens["k_threat"]);
        Assert.Empty(events);
    }

    [Rule("rr:recover-recovery")]
    [Rule("rr:recover-recovery.1")]
    [Fact]
    public void ABasicRecoveryExhaustsAndHealsButNeedsDamageToHeal()
    {
        // "The player exhausts their alter-ego and heals a number of hit points
        // equal to their REC value", and `.1`: "an identity that has no damage
        // to heal cannot perform a basic recovery."
        var printed = new Printed().With("alterego", ("REC", "3"), ("HP", "10"));
        var world = Board(printed, hero: false);
        var identity = world.Seats[0].IdentityCard;

        Assert.False(BasicPowers.CanRecover(world, printed, 0));
        Assert.Throws<RulesNotImplementedException>(
            () => BasicPowers.BasicRecovery(world, printed, 0, []));

        identity.TakeDamage(5);
        Assert.True(BasicPowers.CanRecover(world, printed, 0));
        BasicPowers.BasicRecovery(world, printed, 0, []);

        Assert.False(identity.Ready);
        Assert.Equal(2, identity.Damage);
    }

    [Rule("rr:heal.1")]
    [Fact]
    public void HealingCannotGoPastFullHealth()
    {
        // "A heal effect can only bring a character to its maximum hit points."
        var printed = new Printed().With("alterego", ("REC", "9"), ("HP", "10"));
        var world = Board(printed, hero: false);
        var identity = world.Seats[0].IdentityCard;
        identity.TakeDamage(2);

        var events = new List<GameEvent>();
        BasicPowers.BasicRecovery(world, printed, 0, events);

        Assert.Equal(0, identity.Damage);

        // Healed 2 of a possible 9, and the event says 8 -> 10 rather than
        // 8 -> 17. `Card.TakeDamage` clamps, so again the board is right either
        // way and the wire is what a missing cap would spoil.
        var reported = events.OfType<FieldSet>().Single(set => set.Field == "health");
        Assert.Equal(8, reported.From);
        Assert.Equal(10, reported.To);

        // And healing a character with nothing to heal reports nothing at all,
        // rather than a no-op field change.
        Damage.Heal(world, printed, identity, 5, "test", "test", events);
        Assert.Single(events.OfType<FieldSet>(), set => set.Field == "health");
    }

    [Rule("rr:heal.1")]
    [Fact]
    public void HealAnswersWithWhatItActuallyHealed()
    {
        // Not the amount asked for. `rr:heal.1` caps a heal at full health, so
        // a character damaged by one heals one however large the number on the
        // card -- and cards are written against the difference: "Rhino heals 4
        // damage. **If no damage was healed this way**, this card gains surge."
        //
        // The board stays right either way, because `Card.TakeDamage` clamps.
        // It is the answer that a card reads, and an unclamped answer would
        // make a card that healed nothing believe it had.
        var printed = new Printed().With("alterego", ("REC", "3"), ("HP", "10"));
        var world = Board(printed, hero: false);
        var identity = world.Seats[0].IdentityCard;

        Assert.Equal(0, Damage.Heal(world, printed, identity, 4, "test", "test", []));

        identity.TakeDamage(1);
        Assert.Equal(1, Damage.Heal(world, printed, identity, 4, "test", "test", []));

        identity.TakeDamage(6);
        Assert.Equal(4, Damage.Heal(world, printed, identity, 4, "test", "test", []));
        Assert.Equal(2, identity.Damage);
    }

    [Rule("rr:defeat")]
    [Rule("rr:defeat.1")]
    [Fact]
    public void ADefeatedMinionIsDiscarded()
    {
        // "If a character has zero or fewer remaining hit points [...] it is
        // defeated", and "if an ally, minion, or side scheme is defeated, it is
        // discarded". A minion belongs to the scenario, so its pile is the
        // encounter discard.
        var printed = new Printed()
            .With("hero", ("ATK", "3"))
            .With("minion", ("HP", "3"));
        var world = Board(printed);
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        BasicPowers.BasicAttack(world, printed, 0, minion, []);

        Agendas.Finish(world, printed);

        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
    }

    [Rule("rr:defeat")]
    [Fact]
    public void ExactlyZeroRemainingHitPointsIsADefeat()
    {
        // "**Zero or fewer** remaining hit points" -- not "fewer than zero".
        var printed = new Printed().With("hero", ("ATK", "3")).With("minion", ("HP", "3"));
        var world = Board(printed);
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Agendas.Happening(world);

        Assert.True(Damage.Deal(world, printed, minion, minion, 3, "test", "test", []));
    }

    [Rule("rr:villain-defeat")]
    [Rule("rr:hit-points.2.2")]
    [Fact]
    public void DefeatingAVillainStageRemovesItAndRevealsTheNext()
    {
        // "If a villain's hit point dial is reduced to zero, that stage of the
        // villain is defeated." Remove that stage and reveal the next one.
        var printed = new Printed()
            .With("hero", ("ATK", "9"))
            .With("villain", ("HP", "5"))
            .With("villain2", ("HP", "12"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var next = world.CreateCard("villain2", world.AreaOf(DeckType.VillainDeck));

        BasicPowers.BasicAttack(world, printed, 0, villain, []);

        Agendas.Finish(world, printed);

        Assert.Equal(DeckType.RemovedArea, villain.Area.Type);
        Assert.Equal(DeckType.VillainArea, next.Area.Type);
        Assert.True(next.FaceUp);
        Assert.Equal(Outcome.Unfinished, world.Result);

        // `rr:villain-defeat.2` -- "excess damage that is dealt to defeat a
        // villain stage does not carry over to the new stage." Nine damage
        // against five hit points and the new stage starts clean.
        Assert.Equal(0, next.Damage);
    }

    [Rule("rr:villain-defeat.3")]
    [Rule("rr:villain-defeat.3.2")]
    [Fact]
    public void ANewStageWithTheSameTitleKeepsWhatWasOnTheOldOne()
    {
        // "Attachments, upgrades, status cards, counters, and non-damage tokens
        // on a villain **carry over** to the new stage." Rhino's three stages
        // share a title and Charge attaches to Rhino, so this is the ordinary
        // case in the one scenario the engine plays.
        var printed = new Printed()
            .With("hero", ("ATK", "9"))
            .With("villain", ("HP", "5"))
            .With("villain2", ("HP", "12"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var next = world.CreateCard("villain2", world.AreaOf(DeckType.VillainDeck));
        villain.PlaceTokens("k_threat", 3);
        var attached = world.CreateCard(
            "upgrade",
            world.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));
        // Stunned rather than tough on purpose: a tough card would prevent all
        // the damage (`rr:tough.2`) and the stage would never be defeated at
        // all, which is correct and not what this is about.
        Statuses.Give(world, villain, Statuses.Stunned);

        BasicPowers.BasicAttack(world, printed, 0, villain, []);

        Agendas.Finish(world, printed);

        Assert.Equal(next.ObjectId, attached.Area.Host);
        Assert.Equal(3, next.Tokens["k_threat"]);
        Assert.True(Statuses.Has(world, next, Statuses.Stunned));
    }

    [Rule("rr:villain-defeat.4")]
    [Rule("rr:villain-defeat.4.2")]
    [Fact]
    public void ANewStageWithADifferentTitleKeepsNothing()
    {
        // "Attachments, upgrades, status cards, counters, and non-damage tokens
        // do **not** carry over." The title is the whole of the difference.
        var printed = new Printed()
            .With("hero", ("ATK", "9"))
            .With("villain", ("HP", "5"))
            .With("stranger", ("HP", "12"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var next = world.CreateCard("stranger", world.AreaOf(DeckType.VillainDeck));
        villain.PlaceTokens("k_threat", 3);
        var attached = world.CreateCard(
            "upgrade",
            world.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));

        BasicPowers.BasicAttack(world, printed, 0, villain, []);

        Agendas.Finish(world, printed);

        Assert.Equal(DeckType.EncounterDiscardPile, attached.Area.Type);
        Assert.Equal(0, next.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:enters-play")]
    [Rule("rr:toughness")]
    [Rule("rr:toughness.1")]
    [Fact]
    public void ANewStageEntersPlayWithTheKeywordsItPrints()
    {
        // A villain stage comes out of the villain deck, and `rr:enters-play`
        // is "any time when a card transitions from an out-of-play area into
        // play" -- so `rr:toughness`'s "when a character with the toughness
        // keyword enters play, place a tough status card on it" applies to the
        // stage the deck advances to, not only to the one setup dealt.
        //
        // Rhino's third stage is the card that made this visible: it prints
        // toughness and nothing was reading it, because the scenario the engine
        // could play never advanced the villain deck to a stage with any.
        var printed = new Printed()
            .With("hero", ("ATK", "9"))
            .With("villain", ("HP", "5"))
            .With("villain2", ("HP", "12"), ("Toughness", "1"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var next = world.CreateCard("villain2", world.AreaOf(DeckType.VillainDeck));

        BasicPowers.BasicAttack(world, printed, 0, villain, []);

        Agendas.Finish(world, printed);

        Assert.True(Statuses.Has(world, next, Statuses.Tough));
    }

    [Rule("rr:villain-defeat.3.2")]
    [Rule("rr:toughness.1")]
    [Fact]
    public void AToughCardCarriedOverIsNotDoubledByToughness()
    {
        // The two rules meet on the same card. `rr:villain-defeat.3.2` carries
        // a tough status across to a stage of the same title, and `rr:toughness`
        // would give it one for entering play -- but `rr:status-cards.1` caps a
        // character at one tough card, so the keyword finds its work already
        // done. Order is why this is a test: inheriting first is what makes the
        // cap the thing that decides, rather than the sequence.
        //
        // Confused rather than tough on the defeated stage would not do: a tough
        // card would prevent all the damage (`rr:tough.2`) and the stage would
        // never be defeated at all.
        var printed = new Printed()
            .With("hero", ("ATK", "9"))
            .With("villain", ("HP", "5"))
            .With("villain2", ("HP", "12"), ("Toughness", "1"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var next = world.CreateCard("villain2", world.AreaOf(DeckType.VillainDeck));

        BasicPowers.BasicAttack(world, printed, 0, villain, []);

        Agendas.Finish(world, printed);

        Assert.Single(
            world.Areas
                .Where(area => area.Host == next.ObjectId)
                .SelectMany(area => area.Cards),
            card => card.FaceId == Statuses.Tough);
    }

    [Rule("rr:villain-defeat")]
    [Fact]
    public void DefeatingTheFinalStageWinsTheGame()
    {
        // "**If the final stage of the villain deck is defeated, the players
        // win the game.**" The other ending is the villain completing the main
        // scheme, and a boolean could not tell them apart.
        var printed = new Printed().With("hero", ("ATK", "9")).With("villain", ("HP", "5"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var attachment = world.CreateCard(
            "upgrade",
            world.AreaOf(
                DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));

        BasicPowers.BasicAttack(world, printed, 0, villain, []);

        Agendas.Finish(world, printed);

        Assert.Equal(Outcome.PlayersWin, world.Result);
        Assert.True(world.IsOver);
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
    }

    [Fact]
    public void AGameCannotEndTwice()
    {
        // Two endings racing would mean a rule resolved after the game stopped.
        var printed = new Printed();
        var world = Board(printed);
        world.Finish(Outcome.PlayersWin);

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => world.Finish(Outcome.VillainWins));

        Assert.Contains("already ended", thrown.Message, StringComparison.Ordinal);
    }

    private static int[] Ids(IReadOnlyList<Card> cards) =>
        [.. cards.Select(card => card.ObjectId).Order()];

    private sealed class ProtectedScheme(int scheme) : NoCardAbilities
    {
        public override bool CanRemoveThreat(World world, Card candidate) =>
            candidate.ObjectId != scheme;
    }

    private sealed class RecordingCardPowers : NoCardAbilities
    {
        public List<int> Targets { get; } = [];

        public List<long> Amounts { get; } = [];

        public override void ResolveCardAttack(
            World world, CharacterAttack attack, Marvel.Rules.Timing.Occurrence occurrence,
            List<GameEvent> events)
        {
            Targets.Add(attack.Enemy);
            Amounts.Add(attack.Amount);
        }
    }

    /// <summary>A villain, a main scheme, and one identity per seat.</summary>
    private static World Board(Printed printed, int players = 1, bool hero = true)
    {
        var world = new World(printed, players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
            var identity = world.CreateCard("alterego,hero", world.Seats[seat].Hero);
            world.Seats[seat].IdentityCard = identity;
            if (hero)
            {
                identity.TurnTo("hero");
            }
        }

        world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("scheme", world.AreaOf(DeckType.MainSchemesArea));
        return world;
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
            "upgrade" => CardKind.Attachment,
            "tough" or "stunned" => CardKind.Status,
            _ => CardKind.EncounterVillain,
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

        /// <summary>`villain` and `villain2` are two stages of one character.</summary>
        public string Title(string faceId) =>
            faceId.StartsWith("villain", StringComparison.Ordinal) ? "villain" : faceId;
    }
}
