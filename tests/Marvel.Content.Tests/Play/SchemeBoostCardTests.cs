using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// The scheming enemy's boost cards — <c>rr:scheme-enemy-activation.step.1</c>
/// and <c>.step.2</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Plural, and held by the enemy.</b> Step 1 says "give <b>it</b> one
/// facedown boost card"; step 2 resolves "each of the scheming enemy's boost
/// cards, one at a time and in the order in which they were dealt"; and
/// <c>.step.2.e</c> closes the loop with "if the enemy has any boost cards
/// remaining, repeat these steps with the next boost card."
/// </para>
/// <para>
/// So a scheming enemy holds a set of boost cards and the set is drained one at
/// a time, which is what makes <c>.step.2.e</c> sayable at all. The attack
/// writes the same two steps — <c>rr:attack-enemy-activation</c> step 1 word
/// for word, and its step 3 sub-step for sub-step, differing only in naming ATK
/// where the scheme names SCH — so they are the same two steps for both.
/// </para>
/// </remarks>
public sealed class SchemeBoostCardTests
{
    private const string Campaign = "unus";
    private const uint Seed = 12345;

    /// <summary>`01186` Advance — an encounter card printing no boost icon.</summary>
    private const string NoIcons = "01186";

    /// <summary>`01101` Hydra Mercenary — the same, printing one.</summary>
    private const string OneIcon = "01101";

    /// <summary>`01182` Hydra Soldier — a minion without <c>rr:villainous</c>.</summary>
    private const string PlainMinion = "01182";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:scheme-enemy-activation.step.1")]
    [Fact]
    public void TheBoostCardWaitsFacedownOnTheSchemingEnemy()
    {
        // "Give **it** one facedown boost card from the encounter deck." The
        // card is the enemy's and it is facedown, which means there is a moment
        // at which the enemy is holding it -- step 2 is what ends that moment.
        var (world, runner, unus) = Board();
        var events = new List<GameEvent>();

        var deck = world.AreaOf(DeckType.EncounterDeck);
        var top = deck.Cards[^1];

        Step(world, runner, Steps.Scheme, unus, events);
        Step(world, runner, Steps.GiveBoostCard, unus, events);

        var held = Held(world, unus);
        Assert.Equal(top.ObjectId, Assert.Single(held.Cards).ObjectId);
        Assert.False(top.FaceUp);

        // And step 2 has not happened: it is still on the agenda, and the card
        // is not in the discard pile it ends in.
        Assert.Contains(world.Agenda.Outstanding, step => step.What == Steps.FlipBoostCards);
    }

    [Rule("rr:scheme-enemy-activation.step.2.e")]
    [Rule("rr:boost-boost-icon.6.1")]
    [Fact]
    public void EveryBoostCardTheEnemyHoldsIsResolved()
    {
        // "If the enemy has any boost cards remaining, repeat these steps with
        // the next boost card." A second card put on the enemy before the
        // activation is one the rule says to resolve, and the engine could not
        // even represent it before: the scheme drew its own card and discarded
        // it without ever looking at what the enemy held.
        var (world, runner, unus) = Board();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = main.Tokens.GetValueOrDefault("k_threat");

        // One icon waiting on the enemy, and one more on top of the encounter
        // deck for step 1 to deal.
        world.CreateCard(OneIcon, Held(world, unus));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        World.MoveToTop(world.CreateCard(OneIcon, deck), deck);

        Scheme(world, runner, unus);

        // Unus's first stage prints SCH 1, and each card is worth one more.
        Assert.Equal(3, main.Tokens.GetValueOrDefault("k_threat") - before);
        Assert.Empty(Held(world, unus).Cards);
    }

    [Rule("rr:scheme-enemy-activation.step.2")]
    [Fact]
    public void TheyAreResolvedInTheOrderTheyWereDealt()
    {
        // "One at a time and in the order in which they were dealt." The
        // discard pile records that order: `.step.2.d` discards each card
        // before the next is flipped, so the first resolved ends up underneath
        // the second.
        var (world, runner, unus) = Board();

        var waiting = world.CreateCard(NoIcons, Held(world, unus));

        var deck = world.AreaOf(DeckType.EncounterDeck);
        var dealt = world.CreateCard(OneIcon, deck);
        World.MoveToTop(dealt, deck);

        Scheme(world, runner, unus);

        // The one already on the enemy was dealt first, so it is resolved and
        // discarded first -- and lands below the one step 1 added.
        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        int first = Position(discard, waiting);
        int second = Position(discard, dealt);

        Assert.True(first >= 0 && second >= 0, "both boost cards reach the discard pile");
        Assert.True(first < second, "the card dealt first is discarded first");
    }

    [Rule("rr:activation.6")]
    [Rule("rr:scheme-enemy-activation.step.2.c")]
    [Fact]
    public void TheIconsRaiseSchAndEndWithTheActivation()
    {
        // ".c: increase the scheming enemy's SCH value by one for each boost
        // icon on the card." A modifier, so it needs an end, and the end of a
        // scheme is `rr:activation.6` -- not the end of an attack, which a
        // scheme never reaches. Given the attack's duration the effect would
        // raise this scheme's threat correctly and then sit on the board
        // forever, silently boosting every later one.
        var (world, runner, unus) = Board();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = main.Tokens.GetValueOrDefault("k_threat");

        var deck = world.AreaOf(DeckType.EncounterDeck);
        World.MoveToTop(world.CreateCard(OneIcon, deck), deck);

        Scheme(world, runner, unus);

        Assert.Equal(2, main.Tokens.GetValueOrDefault("k_threat") - before);
        Assert.DoesNotContain(
            world.Effects.Active(),
            effect => effect.Affects == unus.ObjectId
                && effect.Kind is "scheme" or "attack");
    }

    [Rule("rr:activation.1")]
    [Fact]
    public void ASchemesBoostCardIsRecordedAsAScheme()
    {
        // The two kinds of activation are two conditions -- `rr:activation.1`
        // -- and a card can name either one, so what the boost card's movement
        // is recorded under is not decoration. Sharing the steps between the
        // two activations must not make a scheming enemy's boost card look
        // like an attacking one's on the wire.
        var (world, runner, unus) = Board();

        var deck = world.AreaOf(DeckType.EncounterDeck);
        World.MoveToTop(world.CreateCard(OneIcon, deck), deck);

        world.Agenda.Add(new PhaseStep(
            Steps.Scheme, 1, 2, Index: 0, Subject: unus.ObjectId, Seat: 0));
        var events = Agendas.Finish(world, Cards, runner);

        var boosting = events.Where(happened => happened.Verb == "Boost").ToList();
        Assert.NotEmpty(boosting);
        Assert.All(boosting, happened => Assert.Equal(Steps.EnemySchemes, happened.Trigger));
    }

    [Rule("rr:scheme-enemy-activation.step.1")]
    [Fact]
    public void AMinionWithoutVillainousIsGivenNothing()
    {
        // "*(If a minion without the villainous keyword is scheming, skip this
        // step.)*" Skipping matters beyond the icons: taking a card off the
        // encounter deck moves every later deal, so a minion that quietly drew
        // one would change the rest of the game.
        var (world, runner, _) = Board();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = main.Tokens.GetValueOrDefault("k_threat");

        var minion = world.CreateCard(
            PlainMinion, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0), cardOwner: 0));

        var deck = world.AreaOf(DeckType.EncounterDeck);
        int cards = deck.Cards.Count;

        Scheme(world, runner, minion);

        Assert.Equal(cards, deck.Cards.Count);
        Assert.Empty(Held(world, minion).Cards);

        // Hydra Soldier prints SCH 1 and nothing raised it.
        Assert.Equal(1, main.Tokens.GetValueOrDefault("k_threat") - before);
    }

    /// <summary>Where a card sits in an area, or <c>-1</c>.</summary>
    private static int Position(Area area, Card card)
    {
        for (int at = 0; at < area.Cards.Count; at++)
        {
            if (area.Cards[at].ObjectId == card.ObjectId)
            {
                return at;
            }
        }

        return -1;
    }

    /// <summary>Where one enemy's facedown boost cards wait.</summary>
    private static Area Held(World world, Card enemy) => world.AreaOf(
        DeckType.BoostCardsDeck, enemy.Area.PlayArea, host: enemy.ObjectId);

    /// <summary>Takes one named step against one enemy, without the agenda.</summary>
    private static void Step(
        World world, Marvel.Cards.Run.AbilityRunner runner, string what, Card enemy,
        List<GameEvent> events)
    {
        var step = new PhaseStep(what, 1, 2, Index: 0, Subject: enemy.ObjectId, Seat: 0);
        world.Agenda.Add(step);
        VillainPhase.Take(world, Cards, runner, step, events);
    }

    /// <summary>Schedules a whole scheme activation and runs it out.</summary>
    private static void Scheme(World world, Marvel.Cards.Run.AbilityRunner runner, Card enemy)
    {
        world.Agenda.Add(new PhaseStep(
            Steps.Scheme, 1, 2, Index: 0, Subject: enemy.ObjectId, Seat: 0));
        Agendas.Finish(world, Cards, runner);
    }

    private static (World World, Marvel.Cards.Run.AbilityRunner Runner, Card Unus) Board()
    {
        var runner = AuthoredCards.Runner();
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, ["spider_man"]), Cards),
            ["Spider-Man"],
            Seed,
            runner);

        return (world, runner, world.TheCardIn(DeckType.VillainArea)!);
    }
}
