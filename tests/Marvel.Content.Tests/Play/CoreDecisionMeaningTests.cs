using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Known commitments in canonical Core states, without committing a draft.</summary>
public sealed class CoreDecisionMeaningTests
{
    [Rule("rr:basic-power")]
    [Rule("rr:consequential-damage")]
    [Rule("rr:consequential-damage.1")]
    [Theory]
    [InlineData(BasicPowers.AttackVerb, 0)]
    [InlineData(BasicPowers.ThwartVerb, 1)]
    public void BlackCatPowerNamesExhaustionAndItsOwnConsequentialIcons(string verb, int damage)
    {
        // Basic powers exhaust; "After an ally attacks" or thwarts it takes
        // damage equal to the icons beneath the corresponding printed field.
        Card? ally = null;
        string before = string.Empty;
        var (game, world) = Playing(board =>
        {
            ally = PutBlackCatInPlay(board);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 3);
            before = board.Digest().Canonical();
        }, hero: true);
        Affordance power = Assert.Single(game.Pending!.Affordances,
            option => option.AnchorId == ally!.ObjectId && option.Verb == verb);

        Assert.Contains("Exhaust Black Cat", power.Description);
        Assert.Contains($"{damage} consequential damage", power.Description);
        Assert.Contains("before later effects", power.Description);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.True(ally!.Ready);
    }

    [Rule("rr:stun-stunned.1")]
    [Rule("rr:confuse-confused.1")]
    [Theory]
    [InlineData(BasicPowers.AttackVerb, Statuses.Stunned)]
    [InlineData(BasicPowers.ThwartVerb, Statuses.Confused)]
    public void CanceledAllyAttemptStillExhaustsWithoutPromisingConsequentialDamage(string verb, string status)
    {
        // A stunned/confused attempt discards the status instead of attacking
        // or thwarting; it does not become a completed ally basic power.
        Card? ally = null;
        var (game, _) = Playing(board =>
        {
            ally = PutBlackCatInPlay(board);
            Statuses.Give(board, ally, status);
        }, hero: true);
        Affordance power = Assert.Single(game.Pending!.Affordances,
            option => option.AnchorId == ally!.ObjectId && option.Verb == verb);

        Assert.Contains("Exhaust Black Cat", power.Description);
        Assert.Contains($"{status} cancels this attempt", power.Description);
        Assert.DoesNotContain("consequential damage", power.Description);
    }

    [Rule("rr:recover-recovery")]
    [Fact]
    public void RecoveryDescribesTheModifiedRecoveryPowerAndExhaustion()
    {
        // "To recover, the player exhausts their alter-ego and heals a number
        // of hit points equal to their REC value." The canonical damaged
        // Peter Parker state has an engine-applied +2 REC lasting effect.
        string before = string.Empty;
        var (game, world) = Playing(board =>
        {
            Card identity = board.Seats[0].IdentityCard;
            identity.TakeDamage(8);
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Kind: "recover", Amount: 2, Affects: identity.ObjectId));
            before = board.Digest().Canonical();
        });
        Affordance recover = Assert.Single(game.Pending!.Affordances,
            option => option.Verb == BasicPowers.RecoverVerb);

        Assert.Contains("Exhaust Peter Parker", recover.Description);
        Assert.Contains("recover 5 hit points", recover.Description);
        Assert.Contains("before later effects", recover.Description);
        Assert.Equal(before, world.Digest().Canonical());
    }

    [Rule("rr:defend-defense.2")]
    [Rule("rr:defend-defense.3")]
    [Fact]
    public void DefenderOptionsDistinguishModifiedHeroDefenseFromAllyDamage()
    {
        // Hero damage "is reduced by the hero's DEF value"; an ally exhausts
        // and "Damage from the attack is dealt to that ally."
        Card? ally = null;
        var (_, world) = Playing(board =>
        {
            ally = PutBlackCatInPlay(board);
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Kind: "defense", Amount: 2, Affects: board.Seats[0].IdentityCard.ObjectId));
        }, hero: true);
        Card identity = world.Seats[0].IdentityCard;
        int enemy = world.TheCardIn(DeckType.VillainArea)!.ObjectId;
        world.Attack = new EnemyAttack(enemy, 0, identity.ObjectId);
        world.Activation = new EnemyActivation(Enemy: enemy, Player: 0, Attacking: true, Id: 1);
        var before = world.Digest().Canonical();
        Prompt prompt = Assert.IsType<Prompt>(Attack.DeclareDefender(world, world.Facts, new NoCardAbilities()));
        Affordance hero = Assert.Single(prompt.Affordances, option => option.AnchorId == identity.ObjectId);
        Affordance allyOption = Assert.Single(prompt.Affordances, option => option.AnchorId == ally!.ObjectId);

        Assert.Equal("Leave attack undefended", prompt.DeclineLabel);
        Assert.Contains("Exhaust Spider-Man", hero.Description);
        Assert.Contains("DEF 5", hero.Description);
        Assert.Contains("remaining damage", hero.Description);
        Assert.Contains("Exhaust Black Cat", allyOption.Description);
        Assert.Contains("no basic DEF reduction", allyOption.Description);
        Assert.All(prompt.Affordances, option =>
        {
            Assert.DoesNotContain("Boosts", option.Description);
            Assert.Contains("Later effects", option.Description);
        });
        Assert.Equal(before, world.Digest().Canonical());
    }

    [Rule("rr:end-of-player-phase.step.1")]
    [Rule("rr:end-of-player-phase.step.2")]
    [Rule("rr:end-of-player-phase.step.3")]
    [Fact]
    public void EndTurnAndPhaseDiscardHaveDifferentCommitments()
    {
        // Each player may discard "any number" before drawing to hand size,
        // then "readies all of their cards" and exhausted encounter cards.
        var (game, _) = Playing(_ => { });
        Assert.Equal("End turn", game.Pending!.DeclineLabel);
        game.Resolve(Decision.Decline);
        Prompt phase = Assert.IsType<Prompt>(game.Pending);
        Affordance discard = Assert.Single(phase.Affordances);

        Assert.False(phase.Cancellable);
        Assert.Equal(Game.EndPhaseVerb, discard.Verb);
        Assert.Contains("draw up to hand size", phase.Description);
        Assert.Contains("ready player and encounter cards", phase.Description);
        Assert.All(discard.Targets!.Details!.Values, detail =>
            Assert.Contains("Discard this card", detail));
    }

    [Fact]
    public void SpiderSenseDescriptionNamesTheTriggeringPlayersDraw()
    {
        // Printed Core card 01001a: "When the villain initiates an attack
        // against you, draw 1 card." Description must retain that recipient.
        var (_, world) = Playing(_ => { }, hero: true);
        var abilities = AuthoredCards.Runner();
        Card villain = world.TheCardIn(DeckType.VillainArea)!;
        Card identity = world.Seats[0].IdentityCard;
        var occurrence = Occurrence.ForAttack(1, [Steps.AttackInitiated], world,
            world.Facts, villain.ObjectId, identity.ObjectId, 0);
        PendingAbility ability = Assert.Single(abilities.Waiting(world, occurrence, WindowKind.Interrupt),
            candidate => candidate.Card == identity.ObjectId);

        Assert.Equal("The triggering player draws 1 card", abilities.Describe(world, ability).Description);
    }

    [Fact]
    public void SyntheticRequiredDefenderDoesNotAdvertiseDecliningTheAttack()
    {
        // Synthetic card-owned restriction exercises the generic contract;
        // this is not a claim that a Core card requires this defender.
        var (_, world) = Playing(_ => { }, hero: true);
        int enemy = world.TheCardIn(DeckType.VillainArea)!.ObjectId;
        world.Attack = new EnemyAttack(enemy, 0, world.Seats[0].IdentityCard.ObjectId);
        world.Activation = new EnemyActivation(enemy, 0, Attacking: true);

        Prompt prompt = Assert.IsType<Prompt>(Attack.DeclareDefender(world, world.Facts,
            new RequiredDefender()));

        Assert.False(prompt.Cancellable);
        Assert.DoesNotContain("leave the attack undefended", prompt.Description);
        Assert.Contains("Choose a ready hero or ally to defend.", prompt.Description);
    }

    private sealed class RequiredDefender : NoCardAbilities
    {
        public override DefenderChoice Defenders(World world, EnemyAttack attack,
            IReadOnlyList<Card> candidates) => new(candidates, Required: true);
    }

    private static Card PutBlackCatInPlay(World world)
    {
        // Move the dealt Core copy into the canonical state after playing her.
        Card ally = Assert.Single(world.Cards, card => card.FaceId == AuthoredCards.BlackCat);
        World.MoveToTop(ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        return ally;
    }
}
