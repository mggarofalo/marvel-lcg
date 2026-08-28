using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Damage a player divides — <c>rr:indirect-damage</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>101 cards in the pool deal it</b>, and most of them ask nothing: a player
/// with no ally has one character, so every point goes to their identity and
/// there is no division to choose. The question is only put when the eligible
/// characters can hold the damage more than one way.
/// </para>
/// <para>
/// Explosion is the card that needs the question — "assign X damage among
/// heroes and allies, where X is the amount of threat on Bomb Scare" — and it
/// is one of only two cards in the pool that say "assign … among".
/// </para>
/// </remarks>
public sealed class IndirectDamageTests
{
    /// <summary>"Aunt May" — a support, so it is not a character.</summary>
    private const string NotACharacter = "01006";

    /// <summary>"Spider-Man" the ally, from Miles Morales's set.</summary>
    private const string Ally = "13019";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void WithNoBombScareInPlayItSurgesInstead()
    {
        // "If Bomb Scare is not in play, this card gains surge." The scenario
        // deals Bomb Scare into the encounter deck, so a game that has not
        // revealed it is the common case.
        var world = Deal();
        int queued = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count;

        Reveal(world, AuthoredCards.Explosion);

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(
            queued + 1,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
    }

    [Rule("rr:indirect-damage.1")]
    [Fact]
    public void OneCharacterTakesItAllWithoutBeingAsked()
    {
        // A player with no ally has one character and no division to choose.
        // Being asked a question with one answer is not being given a choice --
        // which is why this asks nothing and simply deals it.
        var world = Deal();
        var scare = BombScare(world, threat: 3);

        Reveal(world, AuthoredCards.Explosion);

        Assert.Equal(3, scare.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Rule("rr:indirect-damage.2")]
    [Fact]
    public void WithAnAllyThePlayerIsAskedHowToDivideIt()
    {
        // "Indirect damage dealt to a group of players can be divided as the
        // group chooses among friendly characters in play." Two characters and
        // three damage is a real division, so this asks.
        var world = Deal();
        BombScare(world, threat: 3);
        var ally = world.CreateCard(
            Ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var identity = world.Seats[0].IdentityCard;

        var card = Reveal(world, AuthoredCards.Explosion);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.ChooseOption, waiting.What);

        var asked = AuthoredCards.Runner().Choosing(world, card, 0, waiting.Index)!;
        Assert.Equal(Question.Element, asked.Asking);

        // One entry per point, so the same character may be named twice.
        var targets = Assert.Single(asked.Affordances).Targets!;
        Assert.Equal(3, targets.Min);
        Assert.Equal(3, targets.Max);
        Assert.Equal([identity.ObjectId, ally.ObjectId], targets.Legal);

        AuthoredCards.Runner().Chose(
            world, card, 0, waiting.Index,
            Decision.Take(card.ObjectId, [ally.ObjectId, ally.ObjectId, identity.ObjectId], []));

        Assert.Equal(2, ally.Damage);
        Assert.Equal(1, identity.Damage);
    }

    [Rule("rr:indirect-damage.1")]
    [Fact]
    public void ADiscardedCardBindingShapesAndResolvesTheSuspendedAssignment()
    {
        // The assignment names one character per point. Its amount is known
        // before the question is asked, so the same "discarded this way" card
        // must shape both the prompt and the answer after the ability suspends.
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "01111", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                           "subject": "this" },
              "effect": { "seq": [
                { "discardTop": { "from": "yourDeck", "count": 1 } },
                { "indirectDamage": {
                    "among": { "query": "heroesAndAllies" },
                    "amount": { "printedResourceCountDiscarded": "Y" }
                } }
              ] }
            } ] } ] }
            """));
        var world = Deal();
        world.Abilities = runner;
        var ally = world.CreateCard(
            Ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var energy = world.CreateCard("01002", world.Seats[0].Deck);
        var source = world.CreateCard("01111", world.AreaOf(DeckType.RevealingArea));

        runner.WhenRevealed(world, source, 0);

        var waiting = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, source, 0, waiting.Index, waiting.Tier)!;
        var targets = Assert.Single(prompt.Affordances).Targets!;
        Assert.Equal(1, targets.Min);
        Assert.Equal(1, targets.Max);

        Assert.Throws<RulesNotImplementedException>(() => runner.Chose(
            world, source, 0, waiting.Index,
            Decision.Take(source.ObjectId, [], []), waiting.Tier));
        runner.Chose(
            world, source, 0, waiting.Index,
            Decision.Take(source.ObjectId, [ally.ObjectId], []), waiting.Tier);

        Assert.Equal(1, ally.Damage);
        Assert.Contains(
            energy,
            world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0).Cards);
    }

    [Rule("rr:indirect-damage.3.1")]
    [Fact]
    public void NoCharacterIsAssignedMoreThanWouldDefeatIt()
    {
        // "A character cannot be assigned more indirect damage than would cause
        // it to be defeated." Spider-Man has ten hit points and Bomb Scare has
        // more threat than that, so the rest is simply not assigned rather than
        // piling onto him.
        var world = Deal();
        BombScare(world, threat: 40);
        Agendas.Happening(world);

        Reveal(world, AuthoredCards.Explosion);

        var identity = world.Seats[0].IdentityCard;
        Assert.Equal(Damage.Health(world, Cards, identity), identity.Damage);
    }

    [Rule("rr:indirect-damage.4")]
    [Fact]
    public void ASupportIsNotACharacterAndTakesNone()
    {
        // "Characters that cannot take damage cannot be assigned indirect
        // damage." A support has no hit points at all, so it is not among the
        // heroes and allies however close it sits.
        var world = Deal();
        BombScare(world, threat: 2);
        var support = world.CreateCard(
            NotACharacter, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));

        Reveal(world, AuthoredCards.Explosion);

        Assert.Equal(0, support.Damage);
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:indirect-damage.3.1")]
    [Fact]
    public void ACharacterWithNothingLeftIsNotAssignedAnyAtAll()
    {
        // "A character cannot be assigned more indirect damage than would cause
        // it to be defeated" -- and for a character already at its last hit
        // point of damage, *any* amount would. So it is not among the eligible
        // at all, which is what stops one damage being asked about between two
        // characters when only one can take it.
        //
        // The board is contrived: an ally is put at its full damage without
        // being defeated, which the rules would not leave standing. What is
        // under test is the eligibility, and a legal board cannot show it.
        var world = Deal();
        BombScare(world, threat: 1);
        var ally = world.CreateCard(
            Ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(Damage.Health(world, Cards, ally));

        Reveal(world, AuthoredCards.Explosion);

        // One eligible character, so nothing is asked and the identity takes it.
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }

    /// <summary>Puts Bomb Scare in play with a stated amount of threat.</summary>
    private static Card BombScare(World world, long threat)
    {
        var scare = world.CreateCard(
            AuthoredCards.BombScare, world.AreaOf(DeckType.SideSchemesArea));
        scare.PlaceTokens("k_threat", threat);
        return scare;
    }

    private static Card Reveal(World world, string faceId)
    {
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
        AuthoredCards.Runner().WhenRevealed(world, card, 0);
        return card;
    }

    private static World Deal()
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        world.Abilities = AuthoredCards.Runner();
        return world;
    }
}
