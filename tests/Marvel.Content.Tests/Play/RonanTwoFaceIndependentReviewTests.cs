using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RonanTwoFaceIndependentReviewTests
{
    [Fact]
    [Rule("rr:attack-enemy-activation.step.3.b")]
    public void AccusedAttackBoostAttachesAfterInitiationAndOnlyBonusesTheNextAttack()
    {
        // "Resolve any Boost abilities" follows initiation and declaration of defense.
        var f = new RonanRemainingFixture();
        var accused = f.Card("90003", DeckType.EncounterDeck);
        f.Activate(true);
        f.Finish();
        Assert.Equal(f.World.Seats[1].IdentityCard.ObjectId, accused.Area.Host);
        Assert.Equal(2, f.World.Seats[1].IdentityCard.Damage);
        f.Activate(true);
        f.Finish();
        Assert.Equal(5, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(2, f.Events.OfType<AttackCompleted>().Count());
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(0, 1)]
    [Rule("rr:attack-enemy-activation.step.2")]
    public void AccusedBonusUsesOriginalIdentityBeforeForeignHeroDefends(int attacked, int damage)
    {
        // "The defending character becomes the target character for the attack."
        var f = new RonanRemainingFixture();
        f.World.Seats[0].IdentityCard = f.World.CreateCard("01029a,01029b", f.World.Seats[0].Hero);
        var defender = f.World.Seats[0].IdentityCard;
        f.Reveal("90003");
        f.Finish();
        f.Activate(true, attacked);
        f.Finish(prompt => prompt.Asking == Question.Defender
            && prompt.Affordances.Any(option => option.AnchorId == defender.ObjectId)
            ? Decision.Take(prompt.Affordances.Single(option => option.AnchorId == defender.ObjectId).Id)
            : Decision.Decline);
        Assert.Equal(damage, defender.Damage);
        Assert.Equal(0, f.World.Seats[1].IdentityCard.Damage);
        Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.Equal(2, StateFields.Modified(f.World, f.Villain, "attack", f.World.Facts, 2));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:encounter-deck.1")]
    [Rule("rr:encounter-deck.2")]
    public void OpposeStopsDiscardingAtResetAndDealsOnlyItsExactPhysicalMatches(bool lastMatches)
    {
        // "immediately shuffled to create a new encounter deck"; "Do not continue
        // the discard effect with the newly shuffled encounter deck."
        var f = new RonanRemainingFixture();
        var discard = f.World.AreaOf(DeckType.EncounterDiscardPile);
        foreach (var card in f.World.AreaOf(DeckType.EncounterDeck).Cards.ToArray())
            World.MoveToTop(card, discard);
        var priorMatch = f.Card("90003", DeckType.EncounterDiscardPile);
        var last = f.Card(lastMatches ? "90003" : "01101", DeckType.EncounterDeck);
        var middle = f.Card("01101", DeckType.EncounterDeck);
        var first = f.Card("90004", DeckType.EncounterDeck);
        var effect = f.Reveal("90005");
        f.Finish();
        int[] expected = lastMatches ? [first.ObjectId, last.ObjectId] : [first.ObjectId];
        var dealt = f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards;
        Assert.Equal(expected, dealt.Select(card => card.ObjectId));
        Assert.All(dealt, card => Assert.False(card.FaceUp));
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Equal(DeckType.EncounterDeck, priorMatch.Area.Type);
        Assert.Equal(DeckType.EncounterDeck, middle.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, effect.Area.Type);
        Assert.Equal(1, f.Main.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        var discarded = f.Events.OfType<CardsMoved>().Where(move => move.Verb == "Discard")
            .SelectMany(move => move.Cards).Select(card => card.Card).ToArray();
        Assert.Equal(new[] { first.ObjectId, middle.ObjectId, last.ObjectId }, discarded);
    }

    [Fact]
    public void OpposeWithoutAnyKreeDiscardDealsNoCard()
    {
        var f = new RonanRemainingFixture();
        var unrelated = f.Card("90003", DeckType.EncounterDiscardPile);
        f.Reveal("90005");
        f.Finish();
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Equal(DeckType.EncounterDiscardPile, unrelated.Area.Type);
        Assert.Equal(5, f.Events.OfType<CardsMoved>().Count(move => move.Verb == "Discard"
            && move.From == Places.Reference(f.World.AreaOf(DeckType.EncounterDeck))));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OpposeSchemeBoostCannotBorrowASuspendedOuterAttack(bool nested)
    {
        // A synthetic observer pauses the next boost after actual 90005 resolves.
        // The actual 90005 row is unchanged. Synthetic nesting isolates activation scope;
        // printed activation requests ordinarily queue after their parent activation.
        var data = JsonNode.Parse(AuthoredCards.Text)!;
        var rows = data["cards"]!.AsArray();
        rows.Remove(rows.Single(row => row!["card"]!.GetValue<string>() == "01092"));
        rows.Add(JsonNode.Parse("""
            {"card":"01092","abilities":[{
              "trigger":{"event":"WhenBoostCardTurnedFaceup","timing":"Interrupt","subject":"game"},
              "cost":{"exhaust":"this"},"effect":{"draw":{"player":"you","count":1}}
            }]}
            """));
        var runner = new AbilityRunner(AbilityCatalog.Parse(data.ToJsonString()));
        var f = new RonanRemainingFixture();
        f.World.Abilities = runner;
        f.World.Seats[1].IdentityCard.TurnTo("01001b");
        var observer = f.World.CreateCard("01092",
            f.World.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        if (nested)
        {
            f.Activate(true, 0);
            var defense = Sequence.Work(f.World, f.World.Facts, runner, f.Events)!;
            while (defense.Asking != Question.Defender)
            {
                Sequence.Answer(f.World, f.World.Facts, runner, defense, Decision.Decline, f.Events);
                defense = Sequence.Work(f.World, f.World.Facts, runner, f.Events)!;
            }
            Assert.NotNull(f.World.Attack);
        }
        var oppose = f.World.CreateCard("90005", f.World.AreaOf(DeckType.BoostCardsDeck,
            host: f.Villain.ObjectId));
        f.World.Agenda.Now(new PhaseStep(new EnemySchemeOperation(), 1, 2,
            Subject: f.Villain.ObjectId, Seat: 1));
        var response = Sequence.Work(f.World, f.World.Facts, runner, f.Events)!;
        for (int i = 0; i < 5 && !f.Events.OfType<BoostResolved>().Any(item => item.Card == oppose.ObjectId); i++)
        {
            Sequence.Answer(f.World, f.World.Facts, runner, response, Decision.Decline, f.Events);
            response = Sequence.Work(f.World, f.World.Facts, runner, f.Events)!;
        }
        Assert.NotNull(response);
        Assert.Contains(response.Affordances, option => option.AnchorId == observer.ObjectId);
        Assert.False(f.World.Activation!.Attacking);
        Assert.Equal(nested, f.World.Attack is not null);
        var boost = Assert.Single(f.Events.OfType<BoostResolved>(), item => item.Card == oppose.ObjectId);
        Assert.False(boost.Attacking);
        Assert.Equal(2, boost.Icons);
        Assert.Equal(oppose.ObjectId, boost.Card);
        Assert.Equal(0, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(Keywords.Has(f.World, f.Villain, "overkill", f.World.Facts));
    }
}
