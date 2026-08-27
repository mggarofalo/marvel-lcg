using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>
/// The six steps of an enemy attack — <c>rr:attack-enemy-activation</c>.
/// </summary>
/// <remarks>
/// Synthetic boards, because the recorded game never reaches an attack: its
/// hero never leaves alter-ego form, and <c>rr:activation.1</c> makes a villain
/// facing an alter-ego scheme instead. <c>CardsInWindowsTests</c> runs the same
/// steps on the real Rhino board with real card data; these separate the rules
/// from each other.
/// </remarks>
public sealed class AttackTests
{
    [Rule("rr:attack-enemy-activation.step.1")]
    [Rule("rr:boost-boost-icon")]
    [Fact]
    public void TheBoostCardIsStillFacedownWhenTheDefenderIsDeclared()
    {
        // "During the activation (and after any defenders are declared if the
        // villain is attacking), each boost card on the enemy is turned face
        // up." A defender is chosen without knowing what the boost card is, so
        // flipping it with the same call that gives it would hand the player
        // information the rules withhold.
        var printed = Printed(atk: 2, boost: 3);
        var world = Board(printed);
        var asked = Sequence.Work(world, printed, new NoCardAbilities(), []);

        Assert.NotNull(asked);
        Assert.Equal(Question.Defender, asked.Asking);

        var boost = world.AreaOf(
            DeckType.BoostCardsDeck, PlayArea.Villains,
            host: world.TheCardIn(DeckType.VillainArea)!.ObjectId);
        Assert.Single(boost.Cards);
        Assert.False(boost.Cards[0].FaceUp);
    }

    [Rule("rr:attack-enemy-activation.step.3.c")]
    [Rule("rr:lasting-effects.5")]
    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 3)]
    [InlineData(3, 5)]
    public void BoostIconsAddToTheAttackValueForThatAttackOnly(int boost, int expected)
    {
        // "Increase the attacking enemy's ATK value by one for each boost icon
        // on the card" -- for this attack. A modifier with a stated duration is
        // a lasting effect, so it goes on the same list as everything else
        // continuously in force and comes off when the attack ends.
        var printed = Printed(atk: 2, boost: boost);
        var world = Board(printed);
        Finish(world, printed);

        Assert.Equal(expected, world.Seats[0].IdentityCard.Damage);
        Assert.Empty(world.Effects.Active());
    }

    [Rule("rr:attack-enemy-activation.step.4")]
    [Rule("rr:attack-enemy-activation.step.5")]
    [Fact]
    public void DamageIsFixedBeforeTheSeparateStepThatDealsIt()
    {
        // Step 4 calculates the damage. Step 5 then deals "the amount of
        // damage calculated in the previous step". Changing the attacker's
        // ATK between those occurrences therefore does not rewrite the number
        // that step 5 was told to deal.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Attack.Initiate(
            world, printed,
            new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0), []);

        Attack.CalculateDamage(world, printed);
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: "attack",
            Amount: 5,
            Affects: villain.ObjectId));
        Attack.DealDamage(world, printed, []);

        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:attack-enemy-activation.step.3.d")]
    [Rule("rr:boost-boost-icon.5")]
    [Fact]
    public void TheBoostCardIsDiscardedAfterItIsApplied()
    {
        // "After applying a boost card to an activation, discard it." A card
        // left on the enemy would be given again next activation and counted
        // twice -- `rr:boost-boost-icon.4` makes them cumulative.
        var printed = Printed(atk: 1, boost: 2);
        var world = Board(printed);
        Finish(world, printed);

        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        Assert.Equal(["boost"], discard.Cards.Select(card => card.FaceId));
    }

    [Rule("rr:defend-defense.2")]
    [Fact]
    public void DecliningToDefendLeavesTheAttackUndefended()
    {
        // `rr:attack-enemy-activation.4` -- "if no character was declared the
        // defender of the attack, the attack is considered undefended", and it
        // still resolves. The hero stays ready, which is the visible half of
        // not having exhausted to defend.
        var printed = Printed(atk: 2, boost: 0, def: 1);
        var world = Board(printed);
        Finish(world, printed);

        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.True(world.Seats[0].IdentityCard.Ready);
    }

    [Rule("rr:defend-defense.3")]
    [Fact]
    public void AReadyAllyIsOfferedAsADefenderBesideTheHero()
    {
        // "An ally can exhaust to defend against an enemy attack." Both
        // characters can, so both are offered -- `rr:defend-defense.1` limits
        // the *player*, not the number of candidates.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed);
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));

        var asked = Sequence.Work(world, printed, new NoCardAbilities(), []);

        Assert.NotNull(asked);
        Assert.Equal(Question.Defender, asked.Asking);
        Assert.Equal(
            [world.Seats[0].IdentityCard.ObjectId, ally.ObjectId],
            asked.Affordances.Select(option => option.AnchorId));
    }

    [Rule("rr:defend-defense.3")]
    [Rule("rr:exhausted.2")]
    [Fact]
    public void AnExhaustedAllyIsNotOfferedAsADefender()
    {
        // "An ally can **exhaust** to defend", and `rr:exhausted.2`: a card
        // that must exhaust to pay for an ability "cannot be used until the
        // card is ready". Offering one would be an option that could not be
        // taken.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed);
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.Exhaust();

        var asked = Sequence.Work(world, printed, new NoCardAbilities(), []);

        Assert.NotNull(asked);
        Assert.Equal(
            [world.Seats[0].IdentityCard.ObjectId],
            asked.Affordances.Select(option => option.AnchorId));
    }

    [Rule("rr:defend-defense.3")]
    [Rule("rr:defend-defense.3.1")]
    [Fact]
    public void AnAllyDefendingTakesTheDamageAndTheHeroTakesNone()
    {
        // "Damage from the attack is dealt to that ally", and `.3.1`: "that
        // ally becomes the **target character** for that attack, and its
        // controller becomes the target player."
        //
        // **And its DEF does not apply.** `rr:defend-defense.2`'s reduction is
        // the hero's basic defense power; an ally exhausting to defend is a
        // different clause with no reduction in it. Printed DEF 3 here against
        // an attack of 2, so a hero defending would take nothing -- the ally
        // takes all of it.
        var printed = Printed(atk: 2, boost: 0, def: 3);
        var world = Board(printed);
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var events = new List<GameEvent>();

        var asked = Sequence.Work(world, printed, new NoCardAbilities(), events);
        Sequence.Answer(
            world, printed, new NoCardAbilities(), asked!, Decision.Take(ally.ObjectId), events);
        Sequence.Finish(world, printed, new NoCardAbilities(), events);

        Assert.Equal(2, ally.Damage);
        Assert.False(ally.Ready);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.True(world.Seats[0].IdentityCard.Ready);
    }

    [Rule("rr:ally.1")]
    [Fact]
    public void AnAllyDefeatedByTheAttackIsDiscarded()
    {
        // "If an ally's remaining hit points are reduced to zero, it is
        // defeated and discarded from play." Three hit points against an attack
        // of four, and no DEF to reduce it.
        var printed = Printed(atk: 4, boost: 0);
        var world = Board(printed);
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));

        // A deck with a card in it. An empty one beside an empty discard pile
        // is `rr:player-deck.4`, and the defeated ally landing in the discard
        // resets the deck and takes the ally straight back into it -- correct,
        // and not what this is about.
        world.CreateCard("ally", world.Seats[0].Deck);
        var events = new List<GameEvent>();

        var asked = Sequence.Work(world, printed, new NoCardAbilities(), events);
        Sequence.Answer(
            world, printed, new NoCardAbilities(), asked!, Decision.Take(ally.ObjectId), events);
        Sequence.Finish(world, printed, new NoCardAbilities(), events);

        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
    }

    [Rule("rr:defend-defense.1")]
    [Fact]
    public void EveryPlayersCharactersAreOfferedAsDefenders()
    {
        // `rr:defend-defense.5` lets somebody else's hero defend, so the offer
        // is every player's. "Only one player at a time can defend" (`.1`) is a
        // limit on the answer rather than on the offer -- one prompt, and each
        // affordance says whose character it is.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed, players: 2);

        var asked = Sequence.Work(world, printed, new NoCardAbilities(), []);

        Assert.NotNull(asked);
        Assert.Equal(0, asked.Player);
        Assert.Equal(
            [world.Seats[0].IdentityCard.ObjectId, world.Seats[1].IdentityCard.ObjectId],
            asked.Affordances.Select(option => option.AnchorId));
        Assert.Equal([0, 1], asked.Affordances.Select(option => option.AnchorPlayer));
    }

    [Rule("rr:defend-defense.3")]
    [Fact]
    public void ACardCanRequireOnePlayersAllyToDefendIfAble()
    {
        // "Must defend ... with an ally they control, if able" narrows both
        // halves of the ordinary defense question: the other legal characters
        // are absent and declining is no longer an answer.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed, players: 2);
        var required = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var abilities = new RequiresAlly(0);

        var asked = Sequence.Work(world, printed, abilities, []);

        Assert.NotNull(asked);
        Assert.False(asked.Cancellable);
        Assert.Equal(required.ObjectId, Assert.Single(asked.Affordances).AnchorId);
        Assert.Throws<RulesNotImplementedException>(() => Attack.Defend(
            world, printed, abilities, Decision.Decline, []));
    }

    [Rule("rr:attack-enemy-activation.4")]
    [Fact]
    public void TheOrdinaryOptionalDefenseReturnsWhenTheRequiredAllyIsNotAble()
    {
        // "If able" ends the card's requirement when its matching ally is not
        // ready. The normal attack rule then permits any legal defender or no
        // defender, rather than making the whole step impossible.
        var printed = Printed(atk: 2, boost: 0);
        var world = Board(printed, players: 2);
        var exhausted = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        exhausted.Exhaust();
        var abilities = new RequiresAlly(0);

        var asked = Sequence.Work(world, printed, abilities, []);

        Assert.NotNull(asked);
        Assert.True(asked.Cancellable);
        Assert.Equal(
            [world.Seats[0].IdentityCard.ObjectId, world.Seats[1].IdentityCard.ObjectId],
            asked.Affordances.Select(option => option.AnchorId));
    }

    [Rule("rr:defend-defense.5")]
    [Fact]
    public void DefendingForAnotherPlayerMakesYouTheTarget()
    {
        // "If a player defends against an enemy attack that targets a different
        // player [...] the defending player becomes the new target of that
        // attack." **Both** the target character and the target player move --
        // the damage lands on the defender, and the attack is now against them.
        var printed = Printed(atk: 5, boost: 0, def: 2);
        var world = Board(printed, players: 2);
        var rescuer = world.Seats[1].IdentityCard;
        var events = new List<GameEvent>();

        var asked = Sequence.Work(world, printed, new NoCardAbilities(), events);
        Sequence.Answer(
            world, printed, new NoCardAbilities(), asked!,
            Decision.Take(rescuer.ObjectId), events);

        // **The target *player* moved, not just the character.** The attack is
        // now against seat 1, which is what `rr:defend-defense.5` says outright
        // and what `.5.2` needs -- "any constant or boost abilities that refer
        // to 'you' refer to the defending player".
        //
        // Asserted on the state because nothing reads it yet: `.5.1` splits
        // "when [enemy] attacks you" from "after [enemy] attacks you", and the
        // first is the window that already opened. No ability triggers on the
        // second.
        Assert.Equal(1, world.Attack!.Player);
        Assert.Equal(rescuer.ObjectId, world.Attack.Target);

        Sequence.Finish(world, printed, new NoCardAbilities(), events);

        // ATK 5 less the rescuer's own DEF 2. The player it was aimed at takes
        // nothing at all.
        Assert.Equal(3, rescuer.Damage);
        Assert.False(rescuer.Ready);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.True(world.Seats[0].IdentityCard.Ready);
    }

    [Rule("rr:damage.1")]
    [Rule("rr:hit-points.2.1")]
    [Rule("rr:player-elimination")]
    [Fact]
    public void AHeroReducedToZeroIsDefeatedAndTheirPlayerEliminated()
    {
        // "When a character has damage on it equal to or in excess of its hit
        // points, it is defeated", and `rr:hit-points.2.1`: "if a player's hit
        // point dial is reduced to zero, that player is defeated and eliminated
        // from the game."
        //
        // A hero left standing on 0 hit points is the dangerous board:
        // everything else about it is right.
        var printed = Printed(atk: 20, boost: 0);
        var world = Board(printed);
        var identity = world.Seats[0].IdentityCard;

        Finish(world, printed);

        Assert.True(world.Seats[0].Eliminated);

        // `rr:defeat.2` -- an identity is removed from the game, not discarded.
        Assert.Equal(DeckType.RemovedArea, identity.Area.Type);

        // `rr:player-elimination.4` -- "if all players are eliminated, the game
        // ends and the players lose." One player, so this is that.
        Assert.Equal(Outcome.PlayersLose, world.Result);
    }

    [Rule("rr:damage.1")]
    [Rule("rr:hit-points")]
    [Fact]
    public void HealthIsWhatIsLeftAfterDamage()
    {
        // The digest records a character's `health` and no damage key at all,
        // so damage is the subtrahend rather than something counted beside it.
        // Every recorded board has zero damage on everything, which is exactly
        // why the recording cannot tell a subtraction from a printed constant.
        var printed = Printed(atk: 1, boost: 0);
        var world = new World(printed, players: 1);
        world.CreateSeat("p0");
        var alterEgo = world.CreateCard("alter", world.Seats[0].Hero);

        long Health() => StateFields
            .For(alterEgo, printed, 1, inPlay: true, hasHeldPools: true,
                 hasFirstPlayerToken: false, world: world)["health"];

        Assert.Equal(9, Health());
        alterEgo.TakeDamage(4);
        Assert.Equal(5, Health());
    }

    [Rule("rr:attack-enemy-activation.step.6")]
    [Fact]
    public void TheAttackIsOverWhenItsLastStepIsTaken()
    {
        var printed = Printed(atk: 1, boost: 0);
        var world = Board(printed);
        Finish(world, printed);

        Assert.Null(world.Attack);
        Assert.False(world.Agenda.IsBusy);
    }

    /// <summary>Runs the attack, declining every question.</summary>
    /// <remarks>
    /// Bounded, because the failure this is most likely to meet is a step that
    /// asks the same question forever — and a test that hangs says far less
    /// than one that fails.
    /// </remarks>
    private static void Finish(World world, ICardFacts facts)
    {
        var events = new List<GameEvent>();
        var abilities = new NoCardAbilities();
        var asked = Sequence.Work(world, facts, abilities, events);
        for (int answered = 0; asked is not null; answered++)
        {
            Assert.True(answered < 10, $"'{asked.Label}' is still being asked after 10 answers");
            Sequence.Answer(world, facts, abilities, asked, Decision.Decline, events);
            asked = Sequence.Work(world, facts, abilities, events);
        }
    }

    [Rule("rr:attack-enemy-activation.step.6.a")]
    [Rule("rr:tough.3")]
    [Fact]
    public void AnAttackRecordsWhetherItLandedAndOnlyItsOwnWindowSeesIt()
    {
        // `.step.6.a` lists "after [character] attacks **and damages** ... you"
        // as a trigger of its own, so "it attacked" and "it landed" are two
        // facts -- and by the time those abilities run, the attack is over and
        // the damage is on a dial that had damage on it before. The attack
        // carries what it did.
        //
        // `rr:tough.3` is what pulls the two apart in an ordinary game, and it
        // is the second half of this test: a tough card absorbs the attack, and
        // a character who "is not considered to have taken damage" was not
        // damaged by it.
        var facts = Printed(atk: 3, boost: 0);
        var world = Board(facts);
        Finish(world, facts);

        Assert.NotNull(world.FinishedAttack);
        Assert.True(world.FinishedAttack!.Damaged);

        // A second attack, absorbed. The record is the new attack's own -- the
        // first one's `true` must not still be standing when this window opens.
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Tough);
        world.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 2, Number: 2, Index: 0,
            Subject: world.TheCardIn(DeckType.VillainArea)!.ObjectId, Seat: 0));
        Finish(world, facts);

        Assert.False(world.FinishedAttack!.Damaged);
    }

    [Rule("rr:attack-enemy-activation")]
    [Fact]
    public void AnAttackInProgressHasNoFinishedAttackToRead()
    {
        // The record belongs to one attack. While the next one is resolving
        // there is no finished attack, rather than the previous one's facts
        // sitting there looking current -- an interrupt on the second attack
        // that asked what the attack did would otherwise be answered about the
        // first.
        var facts = Printed(atk: 3, boost: 0);
        var world = Board(facts);
        Finish(world, facts);
        Assert.NotNull(world.FinishedAttack);

        Attack.Initiate(
            world,
            facts,
            new PhaseStep(
                Steps.Attack, 2, 2, Subject: world.TheCardIn(DeckType.VillainArea)!.ObjectId,
                Seat: 0),
            []);

        Assert.Null(world.FinishedAttack);
    }

    [Rule("rr:activation.6")]
    [Fact]
    public void AnAttackerThatLeavesPlayMidAttackStopsThere()
    {
        // "If an activating minion **leaves play**, that minion's activation
        // ends immediately and **no further steps of that activation
        // resolve**." An attack is six steps and any of them can be the one
        // that takes the attacker off the table -- an interrupt answering the
        // attack by defeating the thing making it is the ordinary case.
        //
        // Here the attack is begun and the attacker removed between its steps,
        // which is what such an interrupt would do. Nothing after it may
        // happen: no boost card off the encounter deck, and no damage.
        var facts = Printed(atk: 3, boost: 0);
        var world = Board(facts);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        int deck = world.AreaOf(DeckType.EncounterDeck).Cards.Count;

        Attack.Initiate(
            world, facts,
            new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0), []);
        World.MoveToTop(villain, world.AreaOf(DeckType.RemovedArea));
        Finish(world, facts);

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(deck, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
    }

    /// <summary>A villain, one hero-form identity per seat, one boost card.</summary>
    private static World Board(ICardFacts facts, int players = 1)
    {
        var world = new World(facts, players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
            world.Seats[seat].IdentityCard =
                world.CreateCard("hero", world.Seats[seat].Hero);
        }

        var villain = world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));

        world.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 1, Number: 2, Index: 0, Subject: villain.ObjectId, Seat: 0));
        return world;
    }

    private static Facts Printed(int atk, int boost, int def = 0) => new(atk, boost, def);

    private sealed class RequiresAlly(int player) : NoCardAbilities
    {
        public override DefenderChoice Defenders(
            World world, EnemyAttack attack, IReadOnlyList<Card> candidates)
        {
            var allies = candidates.Where(card =>
                card.Owner == player && world.Facts.Kind(card.FaceId) == CardKind.Ally).ToList();
            return allies.Count > 0
                ? new DefenderChoice(allies, Required: true)
                : new DefenderChoice(candidates, Required: false);
        }
    }

    private sealed class Facts(int atk, int boost, int def) : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes =
            new(StringComparer.Ordinal)
            {
                ["villain"] = new(StringComparer.Ordinal)
                {
                    ["ATK"] = atk.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["HP"] = "16",
                },
                ["boost"] = new(StringComparer.Ordinal)
                {
                    ["Boost"] = boost.ToString(System.Globalization.CultureInfo.InvariantCulture),
                },
                ["hero"] = new(StringComparer.Ordinal)
                {
                    ["HP"] = "10",
                    ["DEF"] = def.ToString(System.Globalization.CultureInfo.InvariantCulture),
                },
                ["ally"] = new(StringComparer.Ordinal) { ["HP"] = "3" },
                ["alter"] = new(StringComparer.Ordinal) { ["HP"] = "9" },
            };

        private readonly Dictionary<string, CardKind> kinds = new(StringComparer.Ordinal)
        {
            ["hero"] = CardKind.Hero,
            ["villain"] = CardKind.EncounterVillain,
            ["boost"] = CardKind.Treachery,
            ["ally"] = CardKind.Ally,
            ["alter"] = CardKind.AlterEgo,
        };

        public CardKind Kind(string faceId) =>
            kinds.TryGetValue(faceId, out var kind) ? kind : CardKind.Unknown;

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
