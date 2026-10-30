using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.KangObligationFixture;

namespace Marvel.Content.Tests.Play;

// Temporal Shield (11014) and Fear of Kang (11049) against attacks with several or
// later recipients: Shield Toss (03006) declares its recipients, Giant Stomp (12003)
// deals splash damage to each minion and then chooses an enemy. Ruling
// 310af249be272877 treats each recipient as attacked, as
// rr:attack-player-ability-type.5 does.
public sealed class KangAttackRecipientTests : CoreCardTestBase
{
    private const string TemporalShield = "11014";
    private const string FearOfKang = "11049";
    private const string MasterOfTime = "11047";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:attack-player-ability-type.5")]
    [Rule("rr:triggering-condition.2")]
    public void ShieldTossTriggersTheShieldWhereverTheShieldedVillainIsListed(bool villainFirst)
    {
        // Captain America discards two cards for two recipients, each dealt 4. Ruling
        // 310af249be272877: the shield "only prevents the damage that would be dealt
        // to Kang"; the Hydra Soldier (3 hit points) takes its 4 and is defeated. The
        // attacker takes the shield's 1 damage either way.
        var (game, world, villain, minion, shields) = AttackRecipientFixture.ShieldToss(TemporalShield, 1);
        var shield = shields[0];
        var attack = Assert.Single(game.Pending!.Affordances);
        int[] targets = villainFirst ? [villain.ObjectId, minion.ObjectId] : [minion.ObjectId, villain.ObjectId];

        game.Resolve(new Decision(attack.Id, targets));

        Assert.Equal(0, villain.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, shield.Area.Type);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [Rule("rr:attack-player-ability-type.5")]
    [Rule("rr:interrupt.3")]
    [Rule("rr:max-maximum")]
    public void GiantStompChoosingAShieldedVillainTriggersTheShield(int shields)
    {
        // The 8 is chosen after the attack begins. Ruling 310af249be272877: the enemy
        // the second sentence hits is attacked then, so the shield answers before the
        // damage: Rhino takes none and Giant Ant-Man takes 1. "(Max 1 per attack.)"
        // A second shield stays attached.
        var (world, f, stomp, pay) = AttackRecipientFixture.AntMan();
        var attached = Enumerable.Range(0, shields).Select(_ => f.Reveal(TemporalShield, 0)).ToArray();
        f.Finish();
        AttackRecipientFixture.Execute(f, stomp, pay);

        f.Finish(prompt => Decision.Take(prompt.Affordances.Any(option => option.AnchorId == f.Villain.ObjectId)
            ? f.Villain.ObjectId : prompt.Affordances[0].Id));

        Assert.Equal(0, f.Villain.Damage);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Single(attached, shield => shield.Area.Type == DeckType.EncounterDiscardPile);
        Assert.Equal(shields - 1, attached.Count(shield => shield.Area.Host == f.Villain.ObjectId));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.5")]
    public void GiantStompChoosingAnUnshieldedVillainDealsItsEight()
    {
        var (world, f, stomp, pay) = AttackRecipientFixture.AntMan();
        AttackRecipientFixture.Execute(f, stomp, pay);

        f.Finish(prompt => Decision.Take(f.Villain.ObjectId));

        Assert.Equal(8, f.Villain.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cannot")]
    [Rule("rr:attack-player-ability-type.5")]
    public void GiantStompsSplashSkipsAKangItsHolderCannotAttack(bool fear)
    {
        // "Deal 1 damage to each minion": every minion is attacked (ruling
        // 310af249be272877), and the holder of Fear of Kang cannot attack the Master
        // of Time. The Hydra Soldier is damaged either way; the 8 goes to Rhino.
        var (world, f, stomp, pay) = AttackRecipientFixture.AntMan();
        var kang = KangTemporalFixture.Engaged(world, MasterOfTime, 0);
        var soldier = KangTemporalFixture.Engaged(world, "01102", 0);
        if (fear) Obligation(world, FearOfKang, 0);
        AttackRecipientFixture.Execute(f, stomp, pay);
        var offered = new List<int>();

        f.Finish(prompt =>
        {
            offered.AddRange(prompt.Affordances.Select(option => option.AnchorId));
            return Decision.Take(f.Villain.ObjectId);
        });

        Assert.Equal(fear ? 0 : 1, kang.Damage);
        Assert.Equal(1, soldier.Damage);
        Assert.Equal(8, f.Villain.Damage);
        Assert.Equal(!fear, offered.Contains(kang.ObjectId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cannot")]
    public void AnEnergyChannelReleaseCannotChooseKangForTheHolder(bool fear)
    {
        var (world, f) = KangTemporalFixture.Board(players: 1);
        var kang = KangTemporalFixture.Engaged(world, MasterOfTime, 0);
        if (fear) Obligation(world, FearOfKang, 0);
        var channel = world.CreateCard("01018", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        channel.PlaceTokens("c_energy", 2);
        var release = f.Runner.Actions(world, 0).Last(action => action.Card == channel.ObjectId);
        f.Runner.ExecuteAction(world, new PlayerAction(release, [], []));
        var offered = new List<int>();

        f.Finish(prompt =>
        {
            offered.AddRange(prompt.Affordances.Select(option => option.AnchorId));
            return Decision.Take(f.Villain.ObjectId);
        });

        Assert.Equal(!fear, offered.Contains(kang.ObjectId));
        Assert.Contains(f.Villain.ObjectId, offered);
        Assert.Equal(4, f.Villain.Damage);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:guard.1")]
    [Rule("rr:cannot")]
    public void AnAttackAfterTheGuardLeavesOffersKangOnlyWithoutFear(bool repeated, bool fear)
    {
        // A synthetic upgrade action defeats the guarding Hydra Mercenary, then attacks
        // an attackable enemy. Its admission projects the board the defeat leaves, and
        // the attack's offer follows it: Rhino always, the Master of Time only without
        // Fear of Kang. `eachPlayer` admits the same body through the repeated trace.
        string body = """
            { "seq": [
              { "dealDamage": { "cards": { "titled": "Hydra Mercenary" }, "amount": 5 } },
              { "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": { "attack": {
                "target": "chosen", "effect": { "dealAttackDamage": { "cards": "chosen", "amount": 1 } } } } } }
            ] }
            """;
        var runner = ActionAbilitySupportFixtures.Runner("01016", "Action",
            repeated ? "{ \"eachPlayer\": { \"effect\": " + body + " } }" : body,
            cost: "{ \"exhaust\": \"this\" }", includeAuthored: true);
        var (world, _) = KangTemporalFixture.Board(players: 1);
        world.Abilities = runner;
        var f = new RedSkullFixture(world, runner);
        var hero = world.Seats[0].IdentityCard;
        var source = world.CreateCard("01016", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), hero.ObjectId, 0));
        var mercenary = KangTemporalFixture.Engaged(world, "01101", 0);
        var kang = KangTemporalFixture.Engaged(world, MasterOfTime, 0);
        if (fear) Obligation(world, FearOfKang, 0);
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var offered = new List<int>();

        f.Finish(prompt =>
        {
            offered.AddRange(prompt.Affordances.Select(option => option.AnchorId));
            return Decision.Take(f.Villain.ObjectId);
        });

        Assert.Equal(DeckType.EncounterDiscardPile, mercenary.Area.Type);
        Assert.Equal(fear ? [f.Villain.ObjectId] : new[] { f.Villain.ObjectId, kang.ObjectId }.Order().ToArray(),
            offered.Order().ToArray());
        Assert.Equal(1, f.Villain.Damage);
    }

    [Fact]
    [Rule("rr:stun-stunned.5.1")]
    [Rule("rr:stun-stunned.7")]
    public void AStunnedHoldersAttemptNamingKangOnlyRemovesTheStun()
    {
        // The stunned attempt is "not considered to have attacked" (rr:stun-stunned.7),
        // so naming the Master of Time attacks nobody: the stun comes off, nothing is
        // damaged, and the hero exhausts as the attempt's cost.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        var kang = KangTemporalFixture.Engaged(world, MasterOfTime, 0);
        Obligation(world, FearOfKang, 0);
        var hero = world.Seats[0].IdentityCard;
        Statuses.Give(world, hero, Statuses.Stunned);

        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, kang, []);
        f.Finish();

        Assert.False(Statuses.Has(world, hero, Statuses.Stunned));
        Assert.False(hero.Ready);
        Assert.Equal(0, kang.Damage);
        Assert.Equal(0, f.Villain.Damage);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:max-maximum")]
    [Rule("rr:attack-player-ability-type.5")]
    public void ShieldTossResolvesOneOfTwoShieldsWhereverTheVillainIsListed(bool villainFirst)
    {
        // "(Max 1 per attack.)" The second shield stays attached, and the villain is a
        // recipient the attack declared, so its damage resolves without raising.
        var (game, world, villain, minion, shields) = AttackRecipientFixture.ShieldToss(TemporalShield, 2);
        var attack = Assert.Single(game.Pending!.Affordances);

        game.Resolve(new Decision(attack.Id,
            villainFirst ? [villain.ObjectId, minion.ObjectId] : [minion.ObjectId, villain.ObjectId]));
        while (game.Pending!.Asking != Question.TurnOption)
            game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));

        Assert.Equal(0, villain.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Single(shields, shield => shield.Area.Type == DeckType.EncounterDiscardPile);
        Assert.Single(shields, shield => shield.Area.Host == villain.ObjectId);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:max-maximum")]
    public void AnAttackEventOnATwiceShieldedVillainResolvesOneShield()
    {
        // Uppercut's target is the attack's enemy, offered at initiation; the shield
        // left attached does not make its damage raise.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        var shields = new[] { f.Reveal(TemporalShield, 0), f.Reveal(TemporalShield, 0) };
        f.Finish();
        var hand = world.Seats[0].Hand;
        var uppercut = world.CreateCard("01054", hand);
        int[] pay = [world.CreateCard("01089", hand).ObjectId, world.CreateCard("01088", hand).ObjectId];
        var action = Assert.Single(f.Runner.Actions(world, 0), ability => ability.Card == uppercut.ObjectId);

        f.Runner.ExecuteAction(world, new PlayerAction(action, pay, []));
        f.Finish(prompt => Decision.Take(prompt.Affordances.Any(option => option.AnchorId == f.Villain.ObjectId)
            ? f.Villain.ObjectId : prompt.Affordances[0].Id));

        Assert.Equal(0, f.Villain.Damage);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Single(shields, shield => shield.Area.Host == f.Villain.ObjectId);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.5")]
    public void GiantStompMayAvoidAShieldedVillain()
    {
        // The splash and the 8 both go to the Hydra Soldier; the shielded villain is
        // never attacked, so nothing raises and the shield stays.
        var (world, f, stomp, pay) = AttackRecipientFixture.AntMan();
        var shield = f.Reveal(TemporalShield, 0);
        f.Finish();
        var soldier = KangTemporalFixture.Engaged(world, "01102", 0);
        AttackRecipientFixture.Execute(f, stomp, pay);

        f.Finish(prompt => Decision.Take(soldier.ObjectId));

        Assert.Equal(DeckType.EncounterDiscardPile, soldier.Area.Type);
        Assert.Equal(0, f.Villain.Damage);
        Assert.Equal(f.Villain.ObjectId, shield.Area.Host);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.5")]
    public void AnAttachmentAnsweringItsHostsDamageDoesNotRaise()
    {
        // Vibranium Armor answers damage to its host, not the host being attacked, so
        // Giant Stomp's later 8 reaches the armoured villain.
        var (world, f, stomp, pay) = AttackRecipientFixture.AntMan();
        world.CreateCard("01152", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, f.Villain.ObjectId));
        AttackRecipientFixture.Execute(f, stomp, pay);

        f.Finish(prompt => Decision.Take(prompt.Affordances.Any(option => option.AnchorId == f.Villain.ObjectId)
            ? f.Villain.ObjectId : prompt.Affordances[0].Id));

        Assert.True(f.Villain.Damage > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:attack-player-ability-type.1.1")]
    [Rule("rr:stun-stunned.5.1")]
    public void WithOnlyKangToAttackTheHolderMayAttemptOnlyWhileStunned(bool stunned)
    {
        // The Kang villain is the only enemy, and the holder cannot attack him. Only a
        // stunned holder may make the targetless attempt, which removes the stun.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        World.MoveToTop(f.Villain, world.AreaOf(DeckType.RemovedArea));
        var kang = world.CreateCard("11001", world.AreaOf(DeckType.VillainArea));
        Obligation(world, FearOfKang, 0);
        var hero = world.Seats[0].IdentityCard;
        if (stunned) Statuses.Give(world, hero, Statuses.Stunned);

        if (!stunned)
        {
            Assert.Throws<RulesNotImplementedException>(() =>
                BasicPowerInitiation.BasicAttack(world, world.Facts, 0, kang, []));
            Assert.True(hero.Ready);
            return;
        }
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, kang, []);
        f.Finish();

        Assert.False(Statuses.Has(world, hero, Statuses.Stunned));
        Assert.Equal(0, kang.Damage);
    }

    [Fact]
    [Rule("rr:triggering-condition")]
    public void PreventionNeedsAPlayerAttackInitiation()
    {
        // An occurrence that initiates no player attack carries nothing to prevent.
        var (world, f) = KangTemporalFixture.Board(players: 1);
        var occurrence = new Marvel.Rules.Timing.Occurrence(0, [Steps.AttackInitiated], Target: f.Villain.ObjectId);

        Assert.False(PlayerAttackPrevention.CanInstall(world, occurrence, f.Villain));
        Assert.Throws<RulesNotImplementedException>(() => PlayerAttackPrevention.Install(world, occurrence, f.Villain));
    }
}
