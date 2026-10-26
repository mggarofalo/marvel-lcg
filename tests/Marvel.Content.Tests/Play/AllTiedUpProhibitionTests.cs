using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Nonpublic characteristic coverage; 02048 is not runtime-admitted.</summary>
public sealed class AllTiedUpProhibitionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cannot")]
    [Rule("rr:end-of-player-phase.step.3")]
    public void AttachedIdentityCannotReadyAtPhaseEndInEitherForm(bool hero)
    {
        // 02048: "Attached character cannot ready or change form."
        // rr:cannot: "The word cannot is absolute"; it also restricts the
        // framework instruction to ready "all of their cards."
        Card? ally = null;
        var (_, world, _) = Playing(hero, board =>
        {
            board.Seats[0].IdentityCard.Exhaust();
            board.Seats[1].IdentityCard.Exhaust();
            ally = board.CreateCard("01002", board.AreaOf(DeckType.AlliesArea,
                PlayArea.Of(0), cardOwner: 0));
            ally.Exhaust();
        });
        var events = new List<GameEvent>();
        PhaseEnd.ReadyCards(world, events);
        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.True(world.Seats[1].IdentityCard.Ready);
        Assert.True(ally!.Ready);
    }

    [Fact]
    [Rule("rr:cannot")]
    public void CardReadyingIsUnavailableAloneAndSkippedInsideAnOtherwiseEffectiveAction()
    {
        var (game, world, _) = Playing(true, board => board.Seats[0].IdentityCard.Exhaust(),
            """{"ready":"you"}""");
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == Probe(world).ObjectId);

        (game, world, _) = Playing(true, board =>
        {
            board.Seats[0].IdentityCard.Exhaust();
            board.Seats[0].IdentityCard.TakeDamage(1);
        }, """{"seq":[{"ready":"you"},{"heal":{"card":"you","amount":1}}]}""");
        Act(game, Probe(world));
        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:cannot")]
    [Rule("rr:then.2")]
    public void FormChangesAreUnavailableAndAFailedCardChangeDoesNotResolveItsThen()
    {
        // ruling:6cc2957c6bbe4596 on All Tied Up / Inner Demons: "prevents changing
        // form (or flipping), even if that comes from another card ability."
        // rr:then.1: the pre-then text must "fully resolve" first.
        var (game, world, _) = Playing(true, _ => { },
            """{"changeForm":{"player":"you","to":"alter-ego"}}""");
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.Verb == Game.ChangeForm);
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == Probe(world).ObjectId);
        string digest = world.Digest().Canonical();
        int formChanged = world.Seats[0].FormChangedInRound;
        Assert.Throws<RulesNotImplementedException>(() =>
            Forms.ChangeAndSchedule(world, world.Seats[0], world.Facts, 1));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(formChanged, world.Seats[0].FormChangedInRound);

        (game, world, _) = Playing(true, board => board.Seats[0].IdentityCard.TakeDamage(1), """
            {"seq":[{"then":{"effect":{"changeForm":{"player":"you","to":"alter-ego"}},
              "then":{"placeCounters":{"card":"this","counter":"dependent","count":1}}}},
              {"heal":{"card":"you","amount":1}}]}
            """);
        Act(game, Probe(world));
        Assert.True(Forms.In(world, world.Seats[0], world.Facts, Forms.Hero));
        Assert.False(Probe(world).Tokens.ContainsKey("c_dependent"));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:cannot")]
    [Rule("rr:then.2")]
    public void SourceDepartureControlsReadyAndFormChangeThen(bool ready, bool remove)
    {
        // rr:then.2: "must resolve in full". The prohibition is active only
        // while its constant ability's source remains in play (rr:ability).
        string operation = ready ? "{\"ready\":\"you\"}"
            : "{\"changeForm\":{\"player\":\"you\",\"to\":\"alter-ego\"}}";
        string departure = remove ? "{\"discard\":{\"titled\":\"All Tied Up\"}}," : "";
        var (game, world, attachment) = Playing(true, board =>
        {
            board.Seats[0].IdentityCard.Exhaust();
            board.Seats[0].IdentityCard.TakeDamage(1);
        }, $$$$$"""
            {"seq":[{{{{{departure}}}}}{"then":{"effect":{{{{{operation}}}}},
              "then":{"placeCounters":{"card":"this","counter":"dependent","count":1}}}},
              {"heal":{"card":"you","amount":1}}]}
            """);
        Act(game, Probe(world));
        Assert.Equal(remove ? DeckType.EncounterDiscardPile : DeckType.UpgradesArea, attachment.Area.Type);
        Assert.Equal(remove, ready ? world.Seats[0].IdentityCard.Ready
            : Forms.In(world, world.Seats[0], world.Facts, Forms.AlterEgo));
        Assert.Equal(remove ? 1 : 0, Probe(world).Tokens.GetValueOrDefault("c_dependent"));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:cannot")]
    public void BlockedFormChangeDoesNotReachAnAlterEgoOnlyContinuation()
    {
        var (game, world, _) = Playing(true, board => board.Seats[0].IdentityCard.TakeDamage(1), """
            {"seq":[{"changeForm":{"player":"you","to":"alter-ego"}},
              {"if":{"test":{"inForm":{"player":"you","form":"alter-ego"}},
                "then":{"attack":{"target":{"query":"villain"},
                  "effect":{"enemyAttacks":{"enemies":{"query":"villain"}}}}},
                "else":{"heal":{"card":"you","amount":1}}}}]}
            """);
        Act(game, Probe(world));
        Assert.True(Forms.In(world, world.Seats[0], world.Facts, Forms.Hero));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:cannot")]
    [Rule("rr:then.2")]
    public void LabelledProjectionChecksThenAgainstProjectedPermissions(bool ready, bool remove)
    {
        string operation = ready ? "{\"ready\":\"you\"}"
            : "{\"changeForm\":{\"player\":\"you\",\"to\":\"alter-ego\"}}";
        string departure = remove ? "{\"discard\":{\"titled\":\"All Tied Up\"}}," : "";
        string effect = $$$$$"""
            {"attack":{"target":{"query":"villain"},"effect":{"seq":[{{{{{departure}}}}}
              {"dealAttackDamage":{"cards":{"query":"villain"},"amount":1}},
              {"then":{"effect":{{{{{operation}}}}},"then":{"choose":{"options":[
                {"draw":{"player":"you","count":1}},{"heal":{"card":"you","amount":1}}]}}}}]}}}
            """;
        (Game Game, World World, Card Attachment) Start() => Playing(true,
            board => board.Seats[0].IdentityCard.Exhaust(), effect);
        if (remove)
        {
            var rejected = Assert.Throws<RulesNotImplementedException>(() => Start());
            Assert.Contains("suspends inside a labelled power", rejected.Message, StringComparison.Ordinal);
            return;
        }
        var (game, world, _) = Start();
        long damage = world.TheCardIn(DeckType.VillainArea)!.Damage;
        Act(game, Probe(world));
        Assert.Equal(damage + 1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.True(Forms.In(world, world.Seats[0], world.Facts, Forms.Hero));
    }

    [Fact]
    public void ProhibitionsFollowTheCurrentHostAndEndWithBlankingOrDeparture()
    {
        var (_, world, attachment) = Playing(true, _ => { });
        Card first = world.Seats[0].IdentityCard;
        Card second = world.Seats[1].IdentityCard;
        Assert.False(ReadinessPermissions.CanReady(world, first));
        Assert.False(FormChangePermissions.CanChange(world, first));
        Assert.True(FormChangePermissions.CanChange(world, second));
        World.MoveToTop(attachment, world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(1), host: second.ObjectId));
        Assert.True(ReadinessPermissions.CanReady(world, first));
        Assert.True(FormChangePermissions.CanChange(world, first));
        Assert.False(ReadinessPermissions.CanReady(world, second));
        Assert.False(FormChangePermissions.CanChange(world, second));
        attachment.AssignProfile(new EffectiveCardProfile("test-blank", "Blank attachment",
            CardKind.Attachment, [], ImmutableDictionary<string, long>.Empty));
        Assert.True(ReadinessPermissions.CanReady(world, second));
        Assert.True(FormChangePermissions.CanChange(world, second));

        (_, world, attachment) = Playing(true, _ => { });
        Discard.Card(world, attachment, "test", []);
        Assert.True(ReadinessPermissions.CanReady(world, world.Seats[0].IdentityCard));
        Assert.True(FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:ability.8.1")]
    [Rule("rr:cost.3")]
    public void HostControllerCanPayThePrintedRemovalActionInEitherForm(bool hero)
    {
        // "Attach to your identity card" restricts who may pay under ability.8.1.
        // 02048: "Action: Spend [mental] [physical] resources → discard this card."
        // rr:cost.3: resources are generated by "discarding cards from their hand".
        Card? mental = null;
        Card? physical = null;
        var (game, world, attachment) = Playing(hero, board =>
        {
            mental = board.CreateCard("01089", board.Seats[0].Hand);
            physical = board.CreateCard("01090", board.Seats[0].Hand);
        });
        var runner = (AbilityRunner)world.Abilities;
        Assert.DoesNotContain(runner.Actions(world, 1), action => action.Card == attachment.ObjectId);
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == attachment.ObjectId);
        Assert.Equal("BR", ResourcePayment.RequiredResources(Assert.Single(action.CostOptions[0].ResourceCosts)));
        var prompt = game.Pending;
        string before = world.Digest().Canonical();
        Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(action.Id, [],
            [physical!.ObjectId], Allocations: [])));
        Assert.Same(prompt, game.Pending);
        Assert.Equal(before, world.Digest().Canonical());
        int[] payment = [mental!.ObjectId, physical!.ObjectId];
        game.Resolve(new Decision(action.Id, [], payment,
            Allocations: ResourcePayment.Allocate(action.CostOptions[0], payment)));
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
        Assert.Equal(DeckType.DiscardPile, mental.Area.Type);
        Assert.Equal(DeckType.DiscardPile, physical.Area.Type);
        Assert.Contains(game.Pending!.Affordances, offer => offer.Verb == Game.ChangeForm);
        Assert.True(ReadinessPermissions.CanReady(world, world.Seats[0].IdentityCard));
    }

    private static Card Probe(World world) => world.Cards.Single(card =>
        card.FaceId == "01008" && DeckTypes.IsInPlay(card.Area.Type));

    private static void Act(Game game, Card source)
    {
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source.ObjectId);
        game.Resolve(Decision.Take(action.Id));
    }

    private static (Game Game, World World, Card Attachment) Playing(
        bool hero, Action<World> prepare, string probeEffect = """{"ready":"you"}""")
    {
        // The actual attachment row is paired with a synthetic probe so its
        // permissions can be challenged by card effects and projections.
        var probe = AbilityCatalog.Parse($$$$$"""
            {"cards":[{"card":"01008","abilities":[
              {"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
               "effect":{{{{{probeEffect}}}}}}]}]}
            """);
        var runner = new AbilityRunner(new AbilityBook(
            [.. AuthoredCards.Book.On("02048"), .. probe.Abilities],
            new HashSet<string>(["02048", "01008"], StringComparer.Ordinal),
            new Dictionary<string, AbilityValue> { ["02048"] = AuthoredCards.Book.Attaches("02048")! }));
        Card? attachment = null;
        var (game, world) = ActionAbilitySupportFixtures6.Playing(board =>
        {
            attachment = board.CreateCard("02048", board.AreaOf(DeckType.UpgradesArea,
                PlayArea.Of(0), host: board.Seats[0].IdentityCard.ObjectId));
            board.CreateCard("01008", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            prepare(board);
        }, hero: hero, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        return (game, world, attachment!);
    }
}
