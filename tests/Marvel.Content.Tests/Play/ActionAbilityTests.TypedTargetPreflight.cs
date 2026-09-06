using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Rule("rr:in-play-and-out-of-play.5")]
    [Fact]
    public void AFacedownDroneCannotBlockThreatRemovalDuringActionAdmission()
    {
        // Card abilities can only "affect the game while they are in play."
        // The Drone retains its underlying face id, but admission must not
        // apply that hidden card's constant prohibition when deciding whether
        // to offer the threat-removal action.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[
              {"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
               "cost":{"exhaust":"this"},
               "effect":{"removeThreat":{"scheme":{"query":"mainScheme"},"amount":1}}}
            ]},{"card":"01091","abilities":[
              {"trigger":{"timing":"Constant","subject":"this"},
               "effect":{"preventThreatRemoval":{"query":"mainScheme"}}}
            ]}]}
            """));
        Card? source = null;
        Card? drone = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            drone = board.CreateCard("01091", board.Seats[0].Deck);
            FacedownDrones.EngageTop(board, 0, "test", "Create_Drone", []);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 1);
        }, abilities: runner);

        Assert.True(FacedownDrones.Is(drone!));
        Assert.True(runner.CanRemoveThreat(world, world.TheCardIn(DeckType.MainSchemesArea)!));
        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
    }

    [Theory]
    [InlineData("heal", false)]
    [InlineData("heal", true)]
    [InlineData("dealDamage", false)]
    [InlineData("dealDamage", true)]
    [InlineData("dealAttackDamage", false)]
    [InlineData("dealAttackDamage", true)]
    [InlineData("placeThreat", false)]
    [InlineData("placeThreat", true)]
    [InlineData("removeThreat", false)]
    [InlineData("removeThreat", true)]
    [InlineData("giveStatus", false)]
    [InlineData("giveStatus", true)]
    [InlineData("grantUntil", false)]
    [InlineData("grantUntil", true)]
    [InlineData("discard", false)]
    [InlineData("discard", true)]
    public void TargetEligibilityAndExecutionUseTheCompiledSelection(string operation, bool choice)
    {
        string arguments = operation switch
        {
            "heal" => """{"card":"you","amount":1}""",
            "dealDamage" or "dealAttackDamage" => """{"cards":{"query":"villain"},"amount":1}""",
            "placeThreat" or "removeThreat" => """{"scheme":{"query":"mainScheme"},"amount":1}""",
            "giveStatus" => """{"card":"you","status":"stunned"}""",
            "grantUntil" => """{"card":"you","keyword":"health","amount":1,"until":"EndOfRound"}""",
            _ => """{"card":"this"}""",
        };
        string field = operation switch
        {
            "dealDamage" or "dealAttackDamage" => "cards",
            "placeThreat" or "removeThreat" => "scheme",
            _ => "card",
        };
        var (runner, fields) = MutableNumericRunner(operation, arguments, choice);
        // Engine contract: input maps are not a live source of target bindings.
        // In this alter-ego scene, the replacement names no card at all.
        fields[field] = new AbilityValue.Word("yourHero");
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(2);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 3);
        }, abilities: runner);
        var identity = world.Seats[0].IdentityCard;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long beforeHealth = Damage.Health(world, world.Facts, identity);
        long beforeThreat = scheme.Tokens.GetValueOrDefault("k_threat");
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        if (choice)
        {
            Assert.Equal(Question.Option, game.Pending!.Asking);
            Assert.Equal(2, game.Pending.Affordances.Count);
            game.Resolve(Decision.Take(0));
        }

        Assert.Equal(operation == "heal" ? 1 : 2, identity.Damage);
        Assert.Equal(operation is "dealDamage" or "dealAttackDamage" ? 1 : 0,
            world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(beforeThreat + (operation == "placeThreat" ? 1 : operation == "removeThreat" ? -1 : 0),
            scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(operation == "giveStatus" ? 1 : 0, Statuses.Count(world, identity, Statuses.Stunned));
        Assert.Equal(beforeHealth + (operation == "grantUntil" ? 1 : 0),
            Damage.Health(world, world.Facts, identity));
        if (operation == "discard") Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        else Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThreatRemovalEligibilityKeepsItsCompiledCrisisException(bool choice)
    {
        var (runner, fields) = MutableNumericRunner("removeThreat",
            """{"scheme":{"query":"mainScheme"},"amount":1,"ignoresCrisis":"true"}""", choice);
        fields["ignoresCrisis"] = new AbilityValue.Word("false");
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea)).PlaceTokens("k_threat", 2);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 3);
        }, hero: true, abilities: runner);
        Assert.True(MainScheme.Crisis(world, world.Facts));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = scheme.Tokens.GetValueOrDefault("k_threat");
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        if (choice)
        {
            Assert.Equal(2, game.Pending!.Affordances.Count);
            game.Resolve(Decision.Take(0));
        }

        Assert.Equal(before - 1, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.True(MainScheme.Crisis(world, world.Facts));
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ThreatRemovalPreflightOverridesOnlyItsCompiledSource(bool choice, bool anotherProhibition)
    {
        var parsed = AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[
              {"trigger":{"timing":"Constant","subject":"this"},
               "effect":{"preventThreatRemoval":{"query":"mainScheme"}}},
              {"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
               "cost":{"exhaust":"this"},
               "effect":{"removeThreat":{"scheme":{"query":"mainScheme"},"amount":1,"overridesCannotFrom":"this"}}}
            ]},{"card":"01091","abilities":[
              {"trigger":{"timing":"Constant","subject":"this"},
               "effect":{"preventThreatRemoval":{"query":"mainScheme"}}}
            ]}]}
            """);
        var action = parsed.Abilities[1];
        var fields = ((AbilityValue.Map)action.Effect.Argument).Entries.ToDictionary(pair => pair.Key, pair => pair.Value);
        var effect = new AbilityNode("removeThreat", new AbilityValue.Map(fields));
        if (choice)
        {
            var fallback = AbilityCatalog.Parse("""
                {"cards":[{"card":"01006","abilities":[{
                  "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                  "effect":{"draw":{"player":"you","count":1}}
                }]}]}
                """).Abilities[0].Effect;
            effect = new AbilityNode("choose", new AbilityValue.Map(new Dictionary<string, AbilityValue>
            {
                ["options"] = new AbilityValue.List([
                    new AbilityValue.Map(new Dictionary<string, AbilityValue> { [effect.Kind] = effect.Argument }),
                    new AbilityValue.Map(new Dictionary<string, AbilityValue> { [fallback.Kind] = fallback.Argument }),
                ]),
            }));
        }
        var abilities = parsed.Abilities.ToList();
        abilities[1] = action with { Effect = effect };
        var runner = new AbilityRunner(new AbilityBook(abilities, parsed.Authored));
        fields["overridesCannotFrom"] = new AbilityValue.Word("you");
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            if (anotherProhibition) InPlay(board, "01091");
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 3);
        }, abilities: runner);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long threat = scheme.Tokens.GetValueOrDefault("k_threat");
        if (anotherProhibition && !choice)
        {
            Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
            Assert.True(source!.Ready);
        }
        else
        {
            var offered = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
            game.Resolve(Decision.Take(offered.Id));
            if (choice)
            {
                int[] expected = anotherProhibition ? [1] : [0, 1];
                Assert.Equal(expected, game.Pending!.Affordances.Select(option => option.Id));
                game.Resolve(Decision.Take(anotherProhibition ? 1 : 0));
            }
            Assert.False(source!.Ready);
        }
        Assert.Equal(threat - (anotherProhibition ? 0 : 1), scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(runner.CanRemoveThreat(world, scheme));
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Theory]
    [InlineData("heal", false)]
    [InlineData("heal", true)]
    [InlineData("giveStatus", false)]
    [InlineData("grantUntil", false)]
    public void PowerProjectionUsesCompiledRecipientsBeforePredictingElimination(string operation, bool dependent)
    {
        string arguments = operation switch
        {
            "heal" => """{"card":{"query":"villain"},"amount":1}""",
            "giveStatus" => """{"card":"you","status":"tough"}""",
            _ => """{"card":"you","keyword":"health","amount":1,"until":"EndOfRound"}""",
        };
        var parsed = AbilityCatalog.Parse($$$$$$"""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},
              "effect":{"attack":{"target":{"query":"villain"},"effect":{"seq":[
                {"{{{{{{operation}}}}}}":{{{{{{arguments}}}}}}},
                {"moveDamage":{"from":{"query":"villain"},"to":"you","amount":1}},
                {"if":{"test":{"inForm":{"player":"firstPlayer","form":"hero"}},
                  "then":{"dealAttackDamage":{"cards":{"query":"villain"},"amount":1}},
                  "else":{"enemyAttacks":{"enemies":{"query":"villain"}}}}}
              ]}}}
            }]}]}
            """);
        var power = parsed.Abilities[0].Effect;
        var powerFields = ((AbilityValue.Map)power.Argument).Entries.ToDictionary(pair => pair.Key, pair => pair.Value);
        var sequence = AbilityNode.Of(powerFields["effect"]);
        var steps = ((AbilityValue.List)sequence.Argument).Values.ToList();
        var first = AbilityNode.Of(steps[0]);
        var fields = ((AbilityValue.Map)first.Argument).Entries.ToDictionary(pair => pair.Key, pair => pair.Value);
        steps[0] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
        {
            [operation] = new AbilityValue.Map(fields),
        });
        if (dependent)
        {
            steps[0] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
            {
                ["otherwise"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
                {
                    ["effect"] = steps[0],
                    ["otherwise"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
                    {
                        ["dealDamage"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
                        {
                            ["cards"] = new AbilityValue.Word("you"),
                            ["amount"] = new AbilityValue.Number(1),
                        }),
                    }),
                }),
            });
        }
        powerFields["effect"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
        {
            ["seq"] = new AbilityValue.List(steps),
        });
        var runner = new AbilityRunner(new AbilityBook(
            [parsed.Abilities[0] with { Effect = new AbilityNode("attack", new AbilityValue.Map(powerFields)) }],
            parsed.Authored));
        var (world, source) = FixedCountBoard(runner, players: 2);
        world.Seats[0].IdentityCard.TakeDamage(9);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        villain.TakeDamage(1);
        fields["card"] = new AbilityValue.Word("this");

        // Healing the donor, Tough on the recipient, or one extra hit point
        // each prevent elimination. The unsupported later-player branch must
        // remain unreachable without changing the live board during preflight.
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source.ObjectId);

        Assert.True(source.Ready);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(10, Damage.Health(world, world.Facts, world.Seats[0].IdentityCard));
        Assert.Equal(0, Statuses.Count(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.Equal(1, villain.Damage);
        Assert.Equal(0, world.FirstPlayer);
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
    }
}
