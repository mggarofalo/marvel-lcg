using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Constant abilities exercised only with Core Set cards.</summary>
public sealed class ConstantAbilityTests
{
    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void CompiledConstantBranchesKeepTheirSyntaxSnapshotButReadTheCurrentBoard()
    {
        var parsed = AbilityCatalog.Parse("""
            {"cards":[{"card":"01094","abilities":[{
              "trigger":{"timing":"Constant"},
              "effect":{"if":{
                "test":{"atLeast":{"value":{"damageOn":"this"},"count":1}},
                "then":{"seq":[
                  {"grant":{"card":"this","keyword":"attack","amount":{"damageOn":"this"}}},
                  {"grantEach":{"cards":{"query":"allies"},"trait":"AVENGER"}}
                ]},
                "else":{"grant":{"card":"this","keyword":"stalwart"}}
              }}
            }]}]}
            """);
        var fields = ((AbilityValue.Map)parsed.Abilities[0].Effect.Argument).Entries
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var ability = parsed.Abilities[0] with { Effect = new AbilityNode("if", new AbilityValue.Map(fields)) };
        var runner = new AbilityRunner(new AbilityBook([ability], parsed.Authored));
        var world = Bare();
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var allyArea = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        Card[] allies = [world.CreateCard("01002", allyArea), world.CreateCard("01020", allyArea)];
        world.Abilities = runner;

        var uninjured = Assert.Single(world.Effects.Active());
        Assert.Equal("stalwart", uninjured.Kind);
        Assert.Equal(1, uninjured.Amount);

        // Engine choice: compiling freezes authored syntax, not game state.
        // Replacing the caller's branch cannot change the running program.
        fields["then"] = fields["else"];
        villain.TakeDamage(2);
        var injured = world.Effects.Active();
        Assert.Equal(3, injured.Count);
        var attack = Assert.Single(injured, effect => effect.Kind == "attack");
        Assert.Equal(villain.ObjectId, attack.Affects);
        Assert.Equal(2, attack.Amount);
        Assert.Equal(allies.Select(card => (int?)card.ObjectId), injured
            .Where(effect => effect.Kind == Traits.Granted + "AVENGER").Select(effect => effect.Affects));

        villain.TakeDamage(1);
        Assert.Equal(3, Assert.Single(world.Effects.Active(), effect => effect.Kind == "attack").Amount);
    }

    [Theory]
    [InlineData("preventReady")]
    [InlineData("preventThreatRemoval")]
    [InlineData("preventDamageWhile")]
    public void CompiledProhibitionsKeepTheirConditionSnapshotButFollowLiveDamage(string instruction)
    {
        string target = instruction == "preventThreatRemoval" ? """{"query":"mainScheme"}""" : "\"this\"";
        string body = instruction == "preventDamageWhile"
            ? """{"preventDamageWhile":{"card":"this","condition":{"atLeast":{"value":{"damageOn":"this"},"count":1}}}}"""
            : $$$$"""{"if":{"test":{"atLeast":{"value":{"damageOn":"this"},"count":1}},"then":{"{{{{instruction}}}}":{{{{target}}}}}}} """;
        var parsed = AbilityCatalog.Parse($$"""
            {"cards":[{"card":"01094","abilities":[{
              "trigger":{"timing":"Constant"},"effect":{{body}}
            }]}]}
            """);
        var fields = ((AbilityValue.Map)parsed.Abilities[0].Effect.Argument).Entries
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var ability = parsed.Abilities[0] with
        {
            Effect = new AbilityNode(parsed.Abilities[0].Effect.Kind, new AbilityValue.Map(fields)),
        };
        var runner = new AbilityRunner(new AbilityBook([ability], parsed.Authored));
        var world = Bare();
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.Abilities = runner;
        var scheme = world.CreateCard("01097", world.AreaOf(DeckType.MainSchemesArea));
        bool Allowed() => instruction switch
        {
            "preventReady" => runner.CanReady(world, villain, villain),
            "preventThreatRemoval" => runner.CanRemoveThreat(world, scheme),
            _ => runner.CanTakeDamage(world, villain, world.Seats[0].IdentityCard),
        };

        Assert.True(Allowed());
        // Engine choice: even a valid replacement condition belongs to the
        // caller's syntax, not to this already-compiled program.
        fields[instruction == "preventDamageWhile" ? "condition" : "test"] =
            new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
            {
                ["inExpertMode"] = new AbilityValue.Word("expert"),
            });
        villain.TakeDamage(1);
        Assert.False(Allowed());
        Assert.Empty(world.Effects.Active());
        villain.TakeDamage(-1);
        Assert.True(Allowed());
    }

    [Rule("rr:in-play-and-out-of-play.5")]
    [Fact]
    public void AFacedownUltronDroneDoesNotUseItsPrintedPlayerCardAbility()
    {
        var world = Bare();
        var inspired = world.CreateCard("01074", world.Seats[0].Deck);
        world.Abilities = AuthoredCards.Runner();

        FacedownDrones.EngageTop(world, 0, "test", "Drone", []);

        Assert.Same(inspired, Assert.Single(FacedownDrones.InPlay(world)));
        Assert.Empty(world.Effects.Active());
    }

    [Rule("rr:ally-limit")]
    [Fact]
    public void TheTriskelionRaisesItsControllersAllyLimit()
    {
        var world = Bare();
        world.CreateCard(
            "01073",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Abilities = AuthoredCards.Runner();

        Assert.Equal(4, Modified(world, world.Seats[0].IdentityCard, "ally_limit"));
    }

    [Rule("rr:attach-to.1")]
    [Rule("rr:in-play-and-out-of-play.12")]
    [Fact]
    public void ReturningHellcatToHandDiscardsInspired()
    {
        var world = Bare();
        var hellcat = world.CreateCard(
            "01020",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var inspired = world.CreateCard(
            "01074",
            world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(0), hellcat.ObjectId, cardOwner: 0));
        world.CreateCard("01080", world.Seats[0].Deck);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        var action = Assert.Single(
            runner.Actions(world, 0), candidate => candidate.Card == hellcat.ObjectId);
        var events = runner.Act(world, action, [], []);

        Assert.Equal(DeckType.HandsArea, hellcat.Area.Type);
        Assert.Equal(DeckType.DiscardPile, inspired.Area.Type);
        Assert.Contains(events.OfType<Marvel.Rules.Events.CardDetached>(), detached =>
            detached.Card == inspired.ObjectId && detached.Host == hellcat.ObjectId);
    }

    [Rule("rr:ability.5")]
    [Fact]
    public void AConstantAbilityCarryingATriggeringConditionIsRefused()
    {
        var refused = Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "01094", "abilities": [ {
                "trigger": { "event": "WhenCardRevealed", "timing": "Constant" },
                "effect": { "grant": { "card": "this", "keyword": "stalwart" } }
            } ] } ] }
            """));

        Assert.Contains("is 'Constant' and triggers on", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ATriggeredAbilityWithNoTriggeringConditionIsRefused()
    {
        var refused = Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "01094", "abilities": [ {
                "trigger": { "timing": "WhenRevealed" },
                "effect": { "grant": { "card": "this", "keyword": "stalwart" } }
            } ] } ] }
            """));

        Assert.Contains("has no 'event'", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AConstantGrantingSomethingTheEngineDoesNotReadThrowsNamingIt()
    {
        var world = Bare();
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var refused = Assert.Throws<AbilityException>(() => Runner(
            """{ "grant": { "card": "this", "keyword": "stallwart" } }"""));

        Assert.Contains("effect/grant/keyword", refused.Message, StringComparison.Ordinal);
        Assert.Contains("'stallwart'", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AConstantAbilityThatDoesSomethingRatherThanGrantingThrows()
    {
        var world = Bare();
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.Abilities = Runner(
            """{ "dealDamage": { "cards": "this", "amount": 1 } }""");

        var refused = Assert.Throws<RulesNotImplementedException>(() => world.Effects.Active());

        Assert.Contains("'01094'", refused.Message, StringComparison.Ordinal);
        Assert.Contains("Damage", refused.Message, StringComparison.Ordinal);
        Assert.Contains("constant ability", refused.Message, StringComparison.Ordinal);
    }

    [Rule("rr:ability.5")]
    [Fact]
    public void OneCardsConstantAbilityIsReadWithoutItsTriggeredOnes()
    {
        var world = Bare();
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "01094", "abilities": [
                {
                  "trigger": { "timing": "Constant" },
                  "effect": { "grant": { "card": "this", "keyword": "stalwart" } }
                },
                {
                  "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed" },
                  "effect": { "dealDamage": { "cards": "this", "amount": 1 } }
                }
            ] } ] }
            """));

        var granted = Assert.Single(world.Effects.Active());

        Assert.Equal("stalwart", granted.Kind);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:ownership-and-control.2.1")]
    [Fact]
    public void AConstantOnAnUpgradeUsesItsCurrentController()
    {
        var world = TwoPlayers();
        var controlled = world.CreateCard(
            "01057",
            world.AreaOf(
                DeckType.UpgradesArea,
                PlayArea.Of(1),
                host: world.Seats[1].IdentityCard.ObjectId,
                cardOwner: 0));
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "01057", "abilities": [ {
                "trigger": { "timing": "Constant" },
                "effect": { "grant": { "card": "yourHero", "keyword": "attack",
                                         "amount": 1 } }
            } ] } ] }
            """));

        Assert.Equal(0, controlled.Owner);
        Assert.Equal(2, Modified(world, world.Seats[0].IdentityCard, "attack"));
        Assert.Equal(3, Modified(world, world.Seats[1].IdentityCard, "attack"));
    }

    [Rule("rr:ownership-and-control.2.1")]
    [Fact]
    public void CombatTrainingCanBePlayedUnderAnotherPlayersControl()
    {
        var world = TwoPlayers();
        for (int player = 0; player < 2; player++)
        {
            world.CreateCard("01003", world.Seats[player].Deck);
        }
        var training = world.CreateCard("01057", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var otherHero = world.Seats[1].IdentityCard;

        Assert.Contains(otherHero.ObjectId, runner.AttachmentTargets(world, training)!);
        CardPlay.Play(
            world, Cards, runner, world.Seats[0], training, [energy.ObjectId], [],
            [otherHero.ObjectId]);

        Assert.Equal(0, training.Owner);
        Assert.Equal(1, training.Area.PlayArea.Player);
        Assert.Equal(otherHero.ObjectId, training.Area.Host);
    }

    [Rule("rr:ownership-and-control.2.1")]
    [Rule("rr:upgrade.3.1")]
    [Fact]
    public void InspiredCanBePlayedOnAnotherPlayersAlly()
    {
        // "A player controls any upgrades attached to characters they
        // control, including upgrades owned by another player." Inspired says
        // only "Attach to an ally," so another player's ally is a legal host.
        var world = TwoPlayers();
        var ally = world.CreateCard(
            "01002",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var inspired = world.CreateCard("01074", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        Assert.Contains(ally.ObjectId, runner.AttachmentTargets(world, inspired)!);
        CardPlay.Play(
            world, Cards, runner, world.Seats[0], inspired, [energy.ObjectId], [],
            [ally.ObjectId]);

        Assert.Equal(0, inspired.Owner);
        Assert.Equal(PlayArea.Of(1), inspired.Area.PlayArea);
        Assert.Equal(ally.ObjectId, inspired.Area.Host);
    }

    private static AbilityRunner Runner(string effect) =>
        new(AbilityCatalog.Parse(
            $$"""
            { "cards": [ { "card": "01094", "abilities": [ {
                "trigger": { "timing": "Constant" }, "effect": {{effect}}
            } ] } ] }
            """));

    private static long Modified(World world, Card card, string field) =>
        StateFields.Modified(world, card, field, Cards, world.Players);

    private static World Bare()
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard(AuthoredCards.SpiderMan, seat.Hero);
        return world;
    }

    private static World TwoPlayers()
    {
        var world = new World(Cards, players: 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(AuthoredCards.SpiderMan, seat.Hero);
        }
        return world;
    }
}
