using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Rule("rr:confuse-confused.5.1")]
    [Fact]
    public void ATargetlessConfusedThwartIsOfferedAndResolvesThroughTheGame()
    {
        // With no threat anywhere, the target list is empty. The status rule
        // still permits the attempt, so the client receives a zero-target
        // affordance that exhausts Spider-Man and removes Confused.
        var (game, world) = Playing(
            board =>
            {
                foreach (var scheme in board.Cards.Where(card =>
                    card.Area.Type is DeckType.MainSchemesArea or DeckType.SideSchemesArea))
                {
                    scheme.PlaceTokens(
                        "k_threat", -scheme.Tokens.GetValueOrDefault("k_threat"));
                }
                Statuses.Give(
                    board, board.Seats[0].IdentityCard, Statuses.Confused);
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        var thwart = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == BasicPowers.ThwartVerb
                && option.AnchorId == world.Seats[0].IdentityCard.ObjectId);
        Assert.Empty(thwart.Targets!.Legal);
        Assert.Equal(0, thwart.Targets.Min);
        Assert.Equal(0, thwart.Targets.Max);

        Assert.Throws<RulesNotImplementedException>(() => game.Resolve(
            Decision.Take(
                thwart.Id,
                [world.TheCardIn(DeckType.VillainArea)!.ObjectId],
                [])));
        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.True(Statuses.Has(
            world, world.Seats[0].IdentityCard, Statuses.Confused));

        game.Resolve(Decision.Take(thwart.Id));

        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.False(Statuses.Has(
            world, world.Seats[0].IdentityCard, Statuses.Confused));
    }

    [Rule("rr:hit-points.3.1")]
    [Rule("rr:ability.11")]
    [Fact]
    public void AnOptionalHealthLossInterruptPausesAndThenResumesTheAbility()
    {
        // Discarding Genetically Enhanced removes the minion's +3 HP and
        // makes defeat imminent. The optional interrupt must be answered
        // before the next effect places threat, and declining it resumes that
        // effect once rather than rediscovering the same imminent defeat.
        var abilities = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [
              { "card": "01006", "abilities": [{
                "trigger": {
                  "event": "WhenActionTriggered", "timing": "Action",
                  "subject": "game"
                },
                "effect": { "seq": [
                  { "discard": { "titled": "Genetically Enhanced" } },
                  { "placeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } }
                ] }
              }] },
              { "card": "01163", "abilities": [{
                "trigger": { "timing": "Constant", "subject": "this" },
                "effect": { "grant": {
                  "card": "attachedTo", "keyword": "health", "amount": 3
                } }
              }] },
              { "card": "01185", "abilities": [{
                "trigger": {
                  "event": "WhenCardWouldBeDefeated", "timing": "Interrupt",
                  "subject": "attachedTo"
                },
                "effect": { "heal": {
                  "card": "attachedTo", "amount": { "damageOn": "attachedTo" }
                } }
              }] }
            ] }
            """));
        Card? source = null;
        Card? minion = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01163",
                    board.AreaOf(
                        DeckType.UpgradesArea, minion.Area.PlayArea,
                        minion.ObjectId));
                board.CreateCard(
                    "01185",
                    board.AreaOf(
                        DeckType.UpgradesArea, minion.Area.PlayArea,
                        minion.ObjectId));
                minion.TakeDamage(3);
            },
            hero: true,
            abilities: abilities);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long threatBefore = scheme.Tokens.GetValueOrDefault("k_threat");
        Assert.Equal(6, Damage.Health(world, Cards, minion!));

        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId
                && option.Verb == Game.ActionVerb);
        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(threatBefore, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);

        game.Resolve(Decision.Decline);

        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(
            threatBefore + 1,
            scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }


    [Rule("rr:hit-points.1")]
    [Rule("rr:hit-points.2.3")]
    [Fact]
    public void StartingHealthExcludesAHitPointModifier()
    {
        // "Starting hit points" is the identity's printed hit point value.
        // Spider-Man prints 10 and currently has 16; dealing startingHealth
        // therefore leaves six rather than treating the modifier as printed.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "dealDamage": {
              "cards": { "titled": "Spider-Man" },
              "amount": { "startingHealth": { "titled": "Spider-Man" } }
            } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var hero = board.Seats[0].IdentityCard;
                board.Effects.Register(new ContinuousEffect(
                    EffectSource.LastingEffect,
                    "health",
                    Amount: 6,
                    Card: source.ObjectId,
                    Affects: hero.ObjectId));
            },
            hero: true,
            abilities: runner);

        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId
                && option.Verb == Game.ActionVerb);
        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(10, world.Seats[0].IdentityCard.Damage);
        Assert.False(world.Seats[0].Eliminated);
        Assert.Equal(
            6,
            Damage.Health(world, Cards, world.Seats[0].IdentityCard)
                - world.Seats[0].IdentityCard.Damage);
    }
}
