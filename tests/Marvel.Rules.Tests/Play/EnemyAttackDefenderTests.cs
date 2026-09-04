using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>Defender replacement and attacks whose printed target is an ally.</summary>
public sealed class EnemyAttackDefenderTests
{
    [Rule("rr:attack-enemy-activation.2.1")]
    [Rule("rr:attack-enemy-activation.3.1")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheDeclaredHeroOrAllyIsTheCharacterConsideredAttacked(bool allyDefends)
    {
        // "The defending hero is considered to have been attacked" and "The
        // defending ally is considered to have been attacked." The target in
        // the attack-ending window is the character that after-attack abilities
        // inspect, so it must be the declared defender rather than the attack's
        // original hero target.
        var (world, facts, villain) = Board();
        var defender = allyDefends
            ? world.CreateCard(
                "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0))
            : world.Seats[0].IdentityCard;
        var observer = new AttackTargetObserver();
        world.Agenda.Add(AttackStep(villain));

        var asked = Sequence.Work(world, facts, observer, []);
        Sequence.Answer(world, facts, observer, asked!, Decision.Take(defender.ObjectId), []);
        Sequence.Finish(world, facts, observer, []);

        Assert.Equal(defender.ObjectId, observer.AttackedAtEnd);
    }

    [Rule("rr:attack-enemy-activation.2.2")]
    [Fact]
    public void DefenseReducesDamageBeforeToughAndZeroDamageKeepsTheStatus()
    {
        // "The damage is first reduced by that hero's DEF value. If the damage
        // is reduced to 0, the hero keeps their tough status." ATK and DEF are
        // both three, so spending tough would prove the order was reversed.
        var (world, facts, villain) = Board(attack: 3, defense: 3);
        var hero = world.Seats[0].IdentityCard;
        Statuses.Give(world, hero, Statuses.Tough);
        world.Agenda.Add(AttackStep(villain));

        var asked = Sequence.Work(world, facts, new NoCardAbilities(), []);
        Sequence.Answer(
            world, facts, new NoCardAbilities(), asked!, Decision.Take(hero.ObjectId), []);
        Sequence.Finish(world, facts, new NoCardAbilities(), []);

        Assert.Equal(0, hero.Damage);
        Assert.True(Statuses.Has(world, hero, Statuses.Tough));
    }

    [Rule("rr:attack-enemy-activation.3.2")]
    [Rule("rr:defend-defense.6")]
    [Fact]
    public void ADefendingAllyThatLeavesBeforeDamageMakesTheAttackUndefended()
    {
        // If the defending ally leaves play before damage, "the attack is
        // considered to have no character defending and the identity of that
        // ally's controller becomes the target." Control is captured while the
        // ally is in player zero's area; its owner-one discard cannot redirect
        // the attack to player one.
        var (world, facts, villain) = Board(players: 2);
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 1));
        world.Agenda.Add(AttackStep(villain));
        var abilities = new NoCardAbilities();

        var asked = Sequence.Work(world, facts, abilities, []);
        Sequence.Answer(world, facts, abilities, asked!, Decision.Take(ally.ObjectId), []);
        World.MoveToTop(
            ally, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
        Sequence.Finish(world, facts, abilities, []);

        Assert.Equal(5, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.False(world.FinishedAttack!.IsDefended);
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, world.FinishedAttack.Target);
    }

    [Rule("rr:defend-defense.2.1")]
    [Rule("rr:defend-defense.2.2")]
    [Fact]
    public void ACardCanDeclareAnExhaustedHeroAsABasicDefender()
    {
        // A card-declared hero "is considered to be making a basic defense",
        // and an ability that declares without exhausting "can be used on an
        // exhausted hero." The hero stays exhausted and its DEF still applies.
        var (world, facts, villain) = Board(attack: 5, defense: 2);
        var hero = world.Seats[0].IdentityCard;
        hero.Exhaust();
        Begin(world, villain, hero);

        Attack.DeclareByAbility(world, facts, hero);

        Assert.False(hero.Ready);
        Assert.True(world.Attack!.BasicDefense);
        Assert.Equal(3, Attack.Amount(world, facts, world.Attack));
    }

    [Rule("rr:defend-defense.3.2")]
    [Rule("rr:defend-defense.3.3")]
    [Fact]
    public void ACardCanDeclareAnExhaustedAllyWithoutUsingDefense()
    {
        // A card instruction makes the ally the defender, and a declaration
        // without exhausting can name an exhausted ally. Ally defense never
        // subtracts a DEF value, even when the synthetic ally prints one.
        var (world, facts, villain) = Board(attack: 5, defense: 2);
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.Exhaust();
        Begin(world, villain, world.Seats[0].IdentityCard);

        Attack.DeclareByAbility(world, facts, ally);

        Assert.False(ally.Ready);
        Assert.False(world.Attack!.BasicDefense);
        Assert.Equal(5, Attack.Amount(world, facts, world.Attack));
    }

    [Rule("rr:defend-defense.1")]
    [Rule("rr:defend-defense.2")]
    [Rule("rr:defend-defense.3")]
    [Fact]
    public void ACardDeclarationCannotReplaceAnotherCharacterAlreadyDefending()
    {
        // "Only one player at a time can defend," and both the hero and ally
        // clauses prevent other friendly characters from defending once one is
        // in that role. Refusal happens before the existing attack changes.
        var (world, facts, villain) = Board();
        var first = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var replacement = world.Seats[0].IdentityCard;
        Begin(world, villain, replacement);
        Attack.DeclareByAbility(world, facts, first);

        Assert.Throws<RulesNotImplementedException>(
            () => Attack.DeclareByAbility(world, facts, replacement));
        Assert.Equal(first.ObjectId, world.Attack!.Defender);
        Assert.Equal(first.ObjectId, world.Attack.Target);
    }

    [Rule("rr:defend-defense.4.3")]
    [Fact]
    public void ANonBasicDefenseHeroCanStillMakeABasicDefenseAtStepTwo()
    {
        // A defense-labeled ability is not a basic defense, but the same hero
        // "can still be declared the defender" during the ordinary step. No
        // other character is offered once that player's identity is defending.
        var (world, facts, villain) = Board(defense: 2);
        var hero = world.Seats[0].IdentityCard;
        Begin(world, villain, hero);
        Attack.BeginDefenseAbility(world, 0, hero);

        var asked = Attack.DeclareDefender(world, facts, new NoCardAbilities());

        Assert.Equal(hero.ObjectId, Assert.Single(asked!.Affordances).AnchorId);
        Assert.Contains("attacking", asked.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(villain.FaceId, asked.Description, StringComparison.Ordinal);
        Attack.Defend(
            world, facts, new NoCardAbilities(), Decision.Take(hero.ObjectId), []);
        Assert.True(world.Attack!.BasicDefense);
        Assert.False(hero.Ready);
    }

    [Rule("rr:defend-defense.4")]
    [Rule("rr:attack-enemy-activation.3.2")]
    [Fact]
    public void ADefenseAbilityIdentityReturnsIfItsDeclaredAllyLeavesBeforeDamage()
    {
        // The defense label first makes the identity a non-basic defender. The
        // Mutant Protectors ruling retains that role behind the explicitly
        // declared ally, so the identity returns rather than the attack becoming
        // undefended if that ally leaves before damage.
        var (world, facts, villain) = Board();
        var hero = world.Seats[0].IdentityCard;
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Begin(world, villain, hero);
        Attack.BeginDefenseAbility(world, 0, hero);
        Attack.DeclareByAbility(world, facts, ally, replaceableDefender: hero.ObjectId);

        World.MoveToTop(
            ally, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        Attack.RefreshDefender(world, facts);

        Assert.Equal(hero.ObjectId, world.Attack!.Defender);
        Assert.Equal(hero.ObjectId, world.Attack.Target);
        Assert.False(world.Attack.BasicDefense);
    }

    [Rule("rr:attack-enemy-activation.3.2")]
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void AnAllyLeavingAfterDamageDoesNotRewriteTheCompletedAttack(long damage)
    {
        // The defender-departure rule is expressly limited to before attack
        // damage. Once step 5 has applied, either its own damage or later
        // response text may move the ally, but the completed attack still
        // remembers the character that defended and was attacked.
        var (world, facts, villain) = Board();
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        Begin(world, villain, ally);
        world.Attack = world.Attack! with
        {
            Defender = ally.ObjectId,
            CalculatedDamage = damage,
        };
        world.Agenda.Add(new PhaseStep(
            Steps.DealAttackDamage, Round: 1, Number: 5,
            Subject: villain.ObjectId, Seat: 0));
        var occurrence = world.Agenda.Begin(world, facts);
        world.Agenda.Advance(occurrence);

        Attack.DealDamage(world, facts, []);
        if (DeckTypes.IsInPlay(ally.Area.Type))
        {
            World.MoveToTop(
                ally, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        }
        Attack.RefreshDefender(world, facts);

        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(ally.ObjectId, world.Attack!.Defender);
        Assert.Equal(ally.ObjectId, world.Attack.Target);
        Assert.True(world.Attack.IsDefended);
    }

    [Rule("rr:attack-enemy-activation.3.2")]
    [Fact]
    public void DamageAgainstOneAdditionalTargetDoesNotMarkTheNextTargetsDamageResolved()
    {
        // One attack can resolve against several heroes. Finishing step 5 for
        // the first target does not put the later target past its own step 5;
        // an ally leaving before that later damage must still expose the hero.
        var (world, facts, villain) = Board(players: 2);
        var first = world.Seats[0].IdentityCard;
        Begin(world, villain, first);
        world.Attack = world.Attack! with
        {
            CalculatedDamage = 0,
            AdditionalPlayers = [1],
        };
        world.Agenda.Add(new PhaseStep(
            Steps.DealAttackDamage, Round: 1, Number: 5,
            Subject: villain.ObjectId, Seat: 0));
        var occurrence = world.Agenda.Begin(world, facts);
        world.Agenda.Advance(occurrence);
        Attack.DealDamage(world, facts, []);

        Attack.NextTarget(world, 1);
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        world.Attack = world.Attack! with
        {
            Defender = ally.ObjectId,
            Target = ally.ObjectId,
        };
        World.MoveToTop(
            ally, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
        Attack.RefreshDefender(world, facts);

        Assert.False(world.Attack!.IsDefended);
        Assert.Equal(world.Seats[1].IdentityCard.ObjectId, world.Attack.Target);
    }

    [Rule("rr:attacks-against-allies.2")]
    [Fact]
    public void AHeroCanDefendAnAttackThatWasInitiatedAgainstAnAlly()
    {
        // Players may defend an attack against an ally "as normal by declaring
        // a hero or an ally as the defender." The hero becomes the target and
        // reduces the damage with its DEF.
        var (world, facts, villain) = Board(attack: 5, defense: 2);
        var attackedAlly = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.Agenda.Add(AttackStep(villain, attackedAlly));
        var abilities = new NoCardAbilities();

        var asked = Sequence.Work(world, facts, abilities, []);
        Assert.Contains(
            asked!.Affordances,
            option => option.AnchorId == world.Seats[0].IdentityCard.ObjectId);
        Sequence.Answer(
            world, facts, abilities, asked,
            Decision.Take(world.Seats[0].IdentityCard.ObjectId), []);
        Sequence.Finish(world, facts, abilities, []);

        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, attackedAlly.Damage);
    }

    [Rule("rr:attacks-against-allies.3")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OverkillSpillsFromEitherAnAttackedOrDefendingAlly(bool allyDefends)
    {
        // If overkill defeats an ally, excess reaches that ally's controller's
        // identity "whether that ally was the attacked ally or a defending
        // ally." Five damage defeats the three-hit-point ally and spills two.
        var (world, facts, villain) = Board();
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("filler", world.Seats[0].Deck);
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: Keywords.Overkill,
            Amount: 1,
            Affects: villain.ObjectId));
        world.Agenda.Add(AttackStep(villain, allyDefends ? null : ally));
        var abilities = new NoCardAbilities();

        var asked = Sequence.Work(world, facts, abilities, []);
        Sequence.Answer(
            world, facts, abilities, asked!,
            allyDefends ? Decision.Take(ally.ObjectId) : Decision.Decline, []);
        Sequence.Finish(world, facts, abilities, []);

        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
    }

    private static void Begin(World world, Card villain, Card target)
    {
        world.Attack = new EnemyAttack(villain.ObjectId, 0, target.ObjectId);
        world.Activation = new EnemyActivation(villain.ObjectId, 0, Attacking: true);
    }

    private static PhaseStep AttackStep(Card villain, Card? target = null) => new(
        Steps.Attack, Round: 1, Number: 2, Index: 0,
        Subject: villain.ObjectId, Seat: 0, Character: target?.ObjectId ?? -1);

    private static (World World, Facts Facts, Card Villain) Board(
        int players = 1, int attack = 5, int defense = 0)
    {
        var facts = new Facts(attack, defense);
        var world = new World(facts, players);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("hero", seat.Hero);
        }

        var villain = world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("filler", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        return (world, facts, villain);
    }

    private sealed class AttackTargetObserver : NoCardAbilities
    {
        public int AttackedAtEnd { get; private set; } = -1;

        public override IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window)
        {
            if (window == WindowKind.Response && occurrence.Is(Steps.AttackEnds))
            {
                AttackedAtEnd = occurrence.Target;
            }
            return [];
        }
    }

    private sealed class Facts(int attack, int defense) : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes =
            new(StringComparer.Ordinal)
            {
                ["villain"] = new(StringComparer.Ordinal)
                {
                    ["ATK"] = attack.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["HP"] = "16",
                },
                ["hero"] = new(StringComparer.Ordinal)
                {
                    ["DEF"] = defense.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["HP"] = "10",
                },
                ["ally"] = new(StringComparer.Ordinal)
                {
                    ["DEF"] = defense.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["HP"] = "3",
                },
                ["boost"] = new(StringComparer.Ordinal) { ["Boost"] = "0" },
            };

        public CardKind Kind(string faceId) => faceId switch
        {
            "villain" => CardKind.EncounterVillain,
            "hero" => CardKind.Hero,
            "ally" => CardKind.Ally,
            "tough" or "stunned" or "confused" => CardKind.Status,
            _ => CardKind.Treachery,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            attributes.TryGetValue(faceId, out var found)
                ? found
                : new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out var printed)
            && long.TryParse(printed, out long value) ? value : fallback;
    }
}
