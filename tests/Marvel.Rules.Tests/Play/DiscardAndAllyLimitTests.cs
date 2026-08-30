using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>Discard destinations and the choice imposed by the ally limit.</summary>
public sealed class DiscardAndAllyLimitTests
{
    [Rule("rr:attach-to.2")]
    [Fact]
    public void AnAttachedCardExhaustsIndependentlyOfItsHost()
    {
        // "An attached card exhausts and readies independently of the game
        // element it is attached to." Exhausting either must not move both.
        var world = Board(new Facts());
        var host = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var attachment = world.CreateCard(
            "upgrade",
            world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(0), host.ObjectId, cardOwner: 0));

        attachment.Exhaust();

        Assert.True(host.Ready);
        Assert.False(attachment.Ready);
    }

    [Rule("rr:tough.2")]
    [Fact]
    public void ASpentStatusLeavesTheEncounterDeckCycle()
    {
        // "Prevent all of that damage and discard a tough status card from that
        // character instead." A status component is not an encounter card, so
        // the engine's out-of-play representation is RemovedArea rather than a
        // pile that could later be shuffled into the encounter deck.
        var facts = new Facts();
        var world = Board(facts);
        var identity = world.Seats[0].IdentityCard;
        Statuses.Give(world, identity, Statuses.Tough);
        var tough = Assert.Single(Statuses.On(world, identity, Statuses.Tough));

        Discard.Card(world, tough, "Tough", []);

        Assert.Equal(DeckType.RemovedArea, tough.Area.Type);
        Assert.DoesNotContain(
            world.AreaOf(DeckType.EncounterDiscardPile).Cards,
            card => card.ObjectId == tough.ObjectId);
    }

    [Rule("rr:attach-to.1")]
    [Fact]
    public void AnAttachmentIsDiscardedAndDetachedWhenItsHostLeavesPlay()
    {
        // "If the game element [a card] is attached to leaves play, [...] the
        // attached card is discarded." The detach event is the wire-visible
        // half of that same transition.
        var facts = new Facts();
        var world = Board(facts);
        var allies = world.AreaOf(
            DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        var host = world.CreateCard("ally", allies);
        var attachment = world.CreateCard(
            "upgrade",
            world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(0), host.ObjectId, cardOwner: 0));
        var events = new List<GameEvent>();

        Discard.Card(world, host, "Defeat", events);

        Assert.Equal(DeckType.DiscardPile, host.Area.Type);
        Assert.Equal(DeckType.DiscardPile, attachment.Area.Type);
        Assert.Contains(events.OfType<CardDetached>(), detached =>
            detached.Card == attachment.ObjectId && detached.Host == host.ObjectId);
    }

    [Rule("rr:permanent.5")]
    [Fact]
    public void AHostLeavingRefusesToGuessWhereAPermanentAttachmentReattaches()
    {
        // A permanent attachment resolves its "attach to" text again, or is
        // removed if no valid target exists. Discarding it like an ordinary
        // attachment would produce a plausible but rules-invalid board.
        var facts = new Facts();
        var world = Board(facts);
        var host = world.CreateCard(
            "ally",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard(
            "permanent",
            world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(0), host.ObjectId, cardOwner: 0));

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => Discard.Card(world, host, "Defeat", []));

        Assert.Contains("rr:permanent.5", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.AlliesArea, host.Area.Type);
    }

    [Rule("rr:loses")]
    [Rule("rr:attach-to.1")]
    [Fact]
    public void AHostedCardThatLosesPermanentIsDiscardedWithItsHost()
    {
        // Permanent remains printed but no longer functions, so the ordinary
        // attachment rule discards this card when its host leaves play.
        var facts = new Facts();
        var world = Board(facts);
        var host = world.CreateCard(
            "ally",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var permanent = world.CreateCard(
            "permanent",
            world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(0), host.ObjectId, cardOwner: 0));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Characteristics.LossOf("permanent"),
            Affects: permanent.ObjectId));

        Discard.Card(world, host, "Defeat", []);

        Assert.Equal(DeckType.DiscardPile, host.Area.Type);
        Assert.Equal(DeckType.DiscardPile, permanent.Area.Type);
    }

    [Rule("rr:permanent.5")]
    [Fact]
    public void ANestedPermanentRefusesTheWholeHostMoveBeforeAnythingChanges()
    {
        // The refusal is atomic across the attachment tree. An ordinary
        // sibling must not be discarded before a permanent descendant proves
        // that the host's departure cannot yet be resolved.
        var facts = new Facts();
        var world = Board(facts);
        var host = world.CreateCard(
            "ally",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var sibling = world.CreateCard(
            "upgrade",
            world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(0), host.ObjectId, cardOwner: 0));
        var bridge = world.CreateCard(
            "upgrade",
            world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(0), host.ObjectId, cardOwner: 0));
        var permanent = world.CreateCard(
            "permanent",
            world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(0), bridge.ObjectId, cardOwner: 0));

        Assert.Throws<RulesNotImplementedException>(
            () => Discard.Card(world, host, "Defeat", []));

        Assert.Equal(DeckType.AlliesArea, host.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, sibling.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, bridge.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, permanent.Area.Type);
    }

    [Rule("rr:ally-limit")]
    [Fact]
    public void AFourthAllyIsChosenAndDiscardedBeforeCardPlayed()
    {
        // A player over their ally limit "must immediately choose and discard
        // from play ally cards they control" and this "occurs before abilities
        // that resolve upon entering play."
        var facts = new Facts();
        var world = Board(facts);
        var seat = world.Seats[0];
        var allies = world.AreaOf(
            DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        var chosen = world.CreateCard("ally", allies);
        world.CreateCard("ally", allies);
        world.CreateCard("ally", allies);
        var fourth = world.CreateCard("toughally", seat.Hand);

        CardPlay.Play(world, facts, new NoCardAbilities(), seat, fourth, [], []);

        Assert.Equal(
            [Steps.ChooseAllyForLimit, Steps.FinalizeAllyEntry, Steps.CardPlayed],
            world.Agenda.Outstanding.Select(step => step.What));
        Assert.Empty(Statuses.On(world, fourth, Statuses.Tough));
        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, facts, new NoCardAbilities(), events);
        Assert.NotNull(asked);
        Assert.Equal(Question.Element, asked.Asking);
        Assert.False(asked.Cancellable);
        Assert.Contains(asked.Affordances, option => option.Id == chosen.ObjectId);

        Sequence.Answer(
            world, facts, new NoCardAbilities(), asked,
            Decision.Take(chosen.ObjectId), events);

        Assert.Equal(DeckType.DiscardPile, chosen.Area.Type);
        Assert.Contains(events.OfType<CardsMoved>(), moved =>
            moved.Cards.Any(card => card.Card == chosen.ObjectId));
        Sequence.Finish(world, facts, new NoCardAbilities(), events);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Single(Statuses.On(world, fourth, Statuses.Tough));
    }

    [Rule("rr:ally-limit")]
    [Fact]
    public void AModifiedLimitOfFourPermitsAFourthAlly()
    {
        // "Ally limit" is the maximum number of allies this player may
        // control. A modifier that raises that value makes four equal to the
        // limit, so no discard choice is due.
        var facts = new Facts();
        var world = Board(facts);
        var seat = world.Seats[0];
        var allies = world.AreaOf(
            DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        world.CreateCard("ally", allies);
        world.CreateCard("ally", allies);
        world.CreateCard("ally", allies);
        var support = world.CreateCard(
            "support",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.ConstantAbility,
            "ally_limit",
            Amount: 1,
            Card: support.ObjectId,
            Affects: seat.IdentityCard.ObjectId,
            Lasts: Duration.WhileInPlay));
        var fourth = world.CreateCard("ally", seat.Hand);

        Assert.Equal(4, StateFields.Modified(
            world, seat.IdentityCard, "ally_limit", facts, world.Players));
        CardPlay.Play(world, facts, new NoCardAbilities(), seat, fourth, [], []);

        var played = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.CardPlayed, played.What);
        Assert.Equal(DeckType.AlliesArea, fourth.Area.Type);
        Assert.Equal(4, allies.Cards.Count);
    }

    [Rule("rr:ally-limit")]
    [Fact]
    public void AnAllyPutIntoPlayAlsoRequiresTheLimitChoice()
    {
        // The rule explicitly permits allies to be "played or put into play"
        // beyond the limit, then immediately requires the same discard choice
        // in either case.
        var facts = new Facts();
        var world = Board(facts);
        var allies = world.AreaOf(
            DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        world.CreateCard("ally", allies);
        world.CreateCard("ally", allies);
        world.CreateCard("ally", allies);
        var fourth = world.CreateCard(
            "ally",
            world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));

        CardPlay.PutAllyIntoPlay(
            world, facts, new NoCardAbilities(), fourth, 0, "test", []);

        Assert.Equal(
            [Steps.ChooseAllyForLimit, Steps.FinalizeAllyEntry],
            world.Agenda.Outstanding.Select(step => step.What));
        Assert.Equal(DeckType.AlliesArea, fourth.Area.Type);
    }

    [Rule("rr:ally-limit")]
    [Fact]
    public void LosingAnAllyLimitModifierRequiresAnImmediateChoice()
    {
        // "If a player ever controls a number of allies greater than their
        // ally limit" includes the limit falling while four allies remain.
        var facts = new Facts();
        var world = Board(facts);
        var seat = world.Seats[0];
        var allies = world.AreaOf(
            DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        for (int count = 0; count < 4; count++)
        {
            world.CreateCard("ally", allies);
        }
        var support = world.CreateCard(
            "support",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.ConstantAbility,
            "ally_limit",
            Amount: 1,
            Card: support.ObjectId,
            Affects: seat.IdentityCard.ObjectId,
            Lasts: Duration.WhileInPlay));

        Discard.Card(world, support, "test", []);

        var choice = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.ChooseAllyForLimit, choice.What);
        Assert.Equal(0, choice.Seat);
    }

    private static World Board(Facts facts)
    {
        var world = new World(facts, players: 1);
        world.CreateSeat("p0");
        world.Seats[0].IdentityCard =
            world.CreateCard("alterego,hero", world.Seats[0].Hero);

        // Keep a player discard from immediately resetting an empty deck.
        world.CreateCard("resource", world.Seats[0].Deck);
        return world;
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "alterego" => CardKind.AlterEgo,
            "hero" => CardKind.Hero,
            "ally" or "toughally" => CardKind.Ally,
            "support" => CardKind.Support,
            "resource" => CardKind.Resource,
            _ => CardKind.Upgrade,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            faceId is "ally" or "toughally"
                ? new Dictionary<string, string>(StringComparer.Ordinal) { ["Cost"] = "0" }
                : new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) =>
            faceId == "permanent" && attribute == "Permanent" ? 1
                : faceId == "toughally" && attribute == "Toughness" ? 1
                : attribute == "Cost" ? 0
                : fallback;
    }
}
