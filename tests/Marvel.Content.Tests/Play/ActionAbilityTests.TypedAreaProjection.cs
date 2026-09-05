using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Rule("rr:guard.1")]
    [Fact]
    public void ProjectedAttackableSelectionExcludesTheVillainWhileGuardRemains()
    {
        // Guard says: “The engaged player cannot attack any villain.” The
        // attackable selector therefore names only the engaged Mercenary. Its
        // Tough prevents defeat; the villain and its attachment stay in play.
        var (runner, _) = MutableAreaSequenceRunner("""
            [{"giveStatus":{"card":{"titled":"Hydra Mercenary"},"status":"tough"} },
             {"dealDamage":{"cards":{"query":"attackableEnemies"},"amount":100} },
             {"removeFromGame":{"cardsIn":{"area":"encounterDiscardPile","title":"Armored Rhino Suit"}}}]
            """, 0);
        var (world, source, minion, _) = AreaProjectionBoard();
        long damageBefore = minion.Damage;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        villain.TakeDamage(Damage.Health(world, world.Facts, villain) - 1);
        long villainDamage = villain.Damage;
        // A different-title next stage makes projected defeat discard the
        // attachment, which would make the final singular selector ambiguous.
        world.CreateCard("01136", world.AreaOf(DeckType.VillainDeck));
        var attachment = world.CreateCard("01098",
            world.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));
        var discarded = world.CreateCard("01098", world.AreaOf(DeckType.EncounterDiscardPile));

        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        runner.Act(world, action, [], []);

        Assert.Equal(damageBefore, minion.Damage);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(villainDamage, villain.Damage);
        Assert.Equal(villain.ObjectId, attachment.Area.Host);
        Assert.Equal(DeckType.UpgradesArea, attachment.Area.Type);
        Assert.Equal(DeckType.RemovedArea, discarded.Area.Type);
        Assert.False(source.Ready);
    }

    [Theory]
    [InlineData("and")]
    [InlineData("or")]
    [InlineData("not")]
    public void AreaProjectionDoesNotTreatAnUnknownConditionAsFalse(string wrapper)
    {
        const string status = """{"hasStatus":{"card":{"titled":"Hydra Mercenary"},"status":"stunned"}}""";
        const string hero = """{"inForm":{"player":"you","form":"hero"}}""";
        string condition = wrapper == "not"
            ? """{"not":{"inForm":{"player":"you","form":"alter-ego"}}}"""
            : $$"""{"{{wrapper}}":[{{status}},{{hero}}]}""";
        var (runner, _) = MutableAreaSequenceRunner($$"""
            [{"if":{"test":{{condition}},
               "then":{"dealDamage":{"cards":{"titled":"Hydra Mercenary"},"amount":1} },
               "else":{"draw":{"player":"you","count":1} }
             } },{{RemoveDiscardedMercenary}}]
            """, 0);
        var (world, source, minion, discarded) = AreaProjectionBoard();
        if (wrapper == "and") Statuses.Give(world, minion, Statuses.Stunned);
        long before = minion.Damage;

        // Area projection does not evaluate form predicates. Its unknown
        // result must retain the branch that can invalidate the singular query.
        Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));

        Assert.True(source.Ready);
        Assert.Equal(before, minion.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, discarded.Area.Type);
    }

    [Theory]
    [InlineData("direct", false)]
    [InlineData("and", false)]
    [InlineData("or", true)]
    [InlineData("not", true)]
    [InlineData("and-known", true)]
    [InlineData("or-known", false)]
    public void AreaProjectionKeepsTheCompiledStatusCondition(string wrapper, bool drawOnTrue)
    {
        const string tough = """{"hasStatus":{"card":{"titled":"Hydra Mercenary"},"status":"tough"}}""";
        const string form = """{"inForm":{"player":"you","form":"hero"}}""";
        string condition = wrapper switch
        {
            "direct" => tough,
            "and" => $$"""{"and":[{{tough}},{{form}}]}""",
            "or" => $$"""{"or":[{"not":{{tough}} },{{form}}]}""",
            "and-known" => $$"""{"and":[{"not":{{tough}} },{"not":{{tough}} }]}""",
            "or-known" => $$"""{"or":[{{tough}},{{tough}}]}""",
            _ => $$"""{"not":{{tough}} }""",
        };
        const string draw = """{"draw":{"player":"you","count":1}}""";
        const string damage = """{"dealDamage":{"cards":{"titled":"Hydra Mercenary"},"amount":1}}""";
        var (runner, fields) = MutableAreaSequenceRunner($$"""
            [{{damage}},
             {"if":{"test":{{condition}},"then":{{(drawOnTrue ? draw : damage)}},
               "else":{{(drawOnTrue ? damage : draw)}} } },
             {{RemoveDiscardedMercenary}}]
            """, 1);
        var (world, source, minion, discarded) = AreaProjectionBoard(tough: true);
        long damageBefore = minion.Damage;
        int held = world.Seats[0].Hand.Cards.Count;
        // The first damage removes Tough. Negating the caller's condition must
        // not change which branch the compiled program projects or executes.
        fields["test"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
        {
            ["not"] = fields["test"],
        });

        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        runner.Act(world, action, [], []);

        Assert.Equal(damageBefore, minion.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.RemovedArea, discarded.Area.Type);
        Assert.False(source.Ready);
    }

    [Theory]
    [InlineData("dealDamage")]
    [InlineData("dealAttackDamage")]
    public void AreaProjectionKeepsTheCompiledDamageAmount(string operation)
    {
        var (runner, fields) = MutableAreaSequenceRunner($$"""
            [{"{{operation}}":{"cards":{"titled":"Hydra Mercenary"},"amount":0} },
             {{RemoveDiscardedMercenary}}]
            """, 0);
        var (world, source, minion, discarded) = AreaProjectionBoard();
        long damageBefore = minion.Damage;
        fields["amount"] = new AbilityValue.Number(1);

        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        runner.Act(world, action, [], []);

        Assert.Equal(damageBefore, minion.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(DeckType.RemovedArea, discarded.Area.Type);
        Assert.False(source.Ready);
    }

    [Fact]
    public void AreaProjectionKeepsTheCompiledHealingBeforeOtherwise()
    {
        var (runner, fields) = MutableAreaSequenceRunner($$"""
            [{"heal":{"card":"you","amount":1} },
             {"otherwise":{
               "effect":{"heal":{"card":"you","amount":1} },
               "otherwise":{"dealDamage":{"cards":{"titled":"Hydra Mercenary"},"amount":1} }
             } },
             {{RemoveDiscardedMercenary}}]
            """, 0);
        var (world, source, minion, discarded) = AreaProjectionBoard();
        world.Seats[0].IdentityCard.TakeDamage(2);
        fields["amount"] = new AbilityValue.Number(2);

        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        runner.Act(world, action, [], []);

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(DeckType.RemovedArea, discarded.Area.Type);
        Assert.False(source.Ready);
    }

    [Fact]
    public void AreaProjectionKeepsTheCompiledToughGrant()
    {
        var (runner, fields) = MutableAreaSequenceRunner($$"""
            [{"giveStatus":{"card":{"titled":"Hydra Mercenary"},"status":"tough"} },
             {"dealDamage":{"cards":{"titled":"Hydra Mercenary"},"amount":1} },
             {{RemoveDiscardedMercenary}}]
            """, 0);
        var (world, source, minion, discarded) = AreaProjectionBoard();
        long damageBefore = minion.Damage;
        fields["status"] = new AbilityValue.Word("stunned");

        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        runner.Act(world, action, [], []);

        Assert.Equal(damageBefore, minion.Damage);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
        Assert.False(Statuses.Has(world, minion, Statuses.Stunned));
        Assert.Equal(DeckType.RemovedArea, discarded.Area.Type);
        Assert.False(source.Ready);
    }

    [Fact]
    public void AreaProjectionKeepsTheCompiledHealthGrant()
    {
        var (runner, fields) = MutableAreaSequenceRunner($$"""
            [{"grantUntil":{"card":{"titled":"Hydra Mercenary"},"keyword":"health",
               "amount":1,"until":"EndOfRound"} },
             {"dealDamage":{"cards":{"titled":"Hydra Mercenary"},"amount":1} },
             {{RemoveDiscardedMercenary}}]
            """, 0);
        var (world, source, minion, discarded) = AreaProjectionBoard();
        long damageBefore = minion.Damage;
        fields["amount"] = new AbilityValue.Number(0);

        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        runner.Act(world, action, [], []);

        Assert.Equal(damageBefore + 1, minion.Damage);
        Assert.Equal(1, Damage.Health(world, world.Facts, minion) - minion.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(DeckType.RemovedArea, discarded.Area.Type);
        Assert.False(source.Ready);
    }

    private const string RemoveDiscardedMercenary = """
        {"removeFromGame":{"cardsIn":{"area":"encounterDiscardPile","title":"Hydra Mercenary"}}}
        """;

    private static (World World, Card Source, Card Minion, Card Discarded) AreaProjectionBoard(bool tough = false)
    {
        Card? source = null;
        Card? minion = null;
        Card? discarded = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            minion.TakeDamage(Damage.Health(board, board.Facts, minion) - 1);
            discarded = board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
        }, hero: true, abilities: AuthoredCards.Runner());
        if (tough) Statuses.Give(world, minion!, Statuses.Tough);
        return (world, source!, minion!, discarded!);
    }

    private static (AbilityRunner Runner, Dictionary<string, AbilityValue> Fields) MutableAreaSequenceRunner(
        string sequence, int index)
    {
        var parsed = AbilityCatalog.Parse($$"""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},"effect":{"seq":{{sequence}} }
            }]}]}
            """);
        var steps = ((AbilityValue.List)parsed.Abilities[0].Effect.Argument).Values.ToList();
        var step = AbilityNode.Of(steps[index]);
        var fields = ((AbilityValue.Map)step.Argument).Entries
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
        steps[index] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
        {
            [step.Kind] = new AbilityValue.Map(fields),
        });
        return (new AbilityRunner(new AbilityBook(
            [parsed.Abilities[0] with { Effect = new AbilityNode("seq", new AbilityValue.List(steps)) }],
            parsed.Authored)), fields);
    }
}
