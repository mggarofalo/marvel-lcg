using System.Text.Json.Nodes;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CounterspellIndependentReviewTests
{
    [Theory]
    [InlineData("valid", true)]
    [InlineData("otherOwner", false)]
    [InlineData("hand", false)]
    [InlineData("upgrade", false)]
    [InlineData("otherCondition", false)]
    [InlineData("nonzero", false)]
    [InlineData("missing", false)]
    public void ProspectiveCancellationIsOnlyAnAdmissionQuery(string variation, bool admitted)
    {
        // Engine choice: id zero names a prospective paid event, never a cancellable frame.
        var world = DoctorStrangeNemesisFixture.Board();
        var source = world.CreateCard(variation == "upgrade" ? "09010" : "09016",
            variation == "hand" ? world.Seats[1].Hand
                : world.AreaOf(DeckType.RevealingArea, cardOwner: 1));
        var occurrence = new Occurrence(variation == "nonzero" ? 1 : 0,
            [variation == "otherCondition" ? "WhenCardPlayed" : EventPlayTiming.Initiated],
            Subject: variation == "missing" ? world.Cards.Count : source.ObjectId,
            Player: variation == "otherOwner" ? 0 : 1);
        var digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Equal(admitted, AbilityOccurrenceCancellation.CanResolve(world, occurrence));
        Assert.Equal(admitted, AbilityOccurrenceCancellation.CanResolve(world, occurrence with { }));
        Assert.False(AbilityOccurrenceCancellation.Cancel(world, occurrence));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:forced.5")]
    [Rule("rr:and.1")]
    public void FirstPlayerChoosesWhichCopyCancelsAndEitherAndOrderCompletes(bool secondFirst, bool discardFirst)
    {
        // "The first player determines the order in which these abilities resolve."
        // "Individual effects connected by 'and' are not dependent on each other."
        var world = DoctorStrangeNemesisFixture.Board();
        var first = DoctorStrangeNemesisFixture.Counterspell(world);
        var second = DoctorStrangeNemesisFixture.Counterspell(world);
        var source = world.CreateCard("09016", world.Seats[1].Hand);
        var payment = world.CreateCard("01088", world.Seats[1].Hand);
        world.Seats[1].IdentityCard.TakeDamage(2);
        var action = Assert.Single(world.Abilities!.Actions(world, 1), offer => offer.Card == source.ObjectId);
        var events = world.Abilities.ExecuteAction(world,
            new PlayerAction(action, [payment.ObjectId], [world.TheCardIn(DeckType.VillainArea)!.ObjectId])).Events.ToList();
        var copies = Sequence.Work(world, world.Facts, world.Abilities, events)!;
        Assert.Equal(Question.Order, copies.Asking);
        Assert.Equal(0, copies.Player);
        Assert.Equal(2, copies.Affordances.Count);
        var winner = secondFirst ? second : first;
        var selected = Assert.Single(copies.Affordances, offer => offer.AnchorId == winner.ObjectId);
        Sequence.Answer(world, world.Facts, world.Abilities, copies, Decision.Take(selected.Id), events);
        var effects = Sequence.Work(world, world.Facts, world.Abilities, events)!;
        Assert.Equal(Question.Order, effects.Asking);
        Sequence.Answer(world, world.Facts, world.Abilities, effects,
            new Decision(Assert.Single(effects.Affordances).Id, discardFirst ? [1, 0] : [0, 1]), events);
        Sequence.Finish(world, world.Facts, world.Abilities, events);
        Assert.Equal(DeckType.EncounterDiscardPile, winner.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, (secondFirst ? first : second).Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        var discards = events.OfType<CardsMoved>().Where(move => move.Verb == "Discard").ToArray();
        var eventDiscard = Assert.Single(discards, move => move.Cards.Any(card => card.Card == source.ObjectId));
        var attachmentDiscard = Assert.Single(discards, move => move.Cards.Any(card => card.Card == winner.ObjectId));
        Assert.True(Array.IndexOf(discards, eventDiscard) < Array.IndexOf(discards, attachmentDiscard));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:status-cards.2")]
    [Rule("rr:labeled-ability.6.2")]
    public void OnlySelectedAbilityLabelsConsumeStatusesAfterArrowCost(bool labeled)
    {
        // "Status card abilities have timing priority over all conflicting triggered abilities."
        // "Each status card ... that cancels any of the labeled ability's types is removed".
        // Synthetic same-cost bodies distinguish selected labels from another ability's labels.
        var world = DoctorStrangeNemesisFixture.Board();
        var rows = new JsonArray();
        var row = JsonNode.Parse("""
            {"card":"09016","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "cost":{"exhaust":"you"},"effect":{"draw":{"player":"you","count":1}}
            },{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"alter-ego"},
              "labels":["attack","thwart"],"effect":{"draw":{"player":"you","count":1}}
            }]}
            """)!;
        if (labeled) row["abilities"]![0]!["labels"] = JsonNode.Parse("[\"attack\",\"thwart\"]");
        rows.Add(row);
        world.Abilities = DoctorStrangeAbilityFixture.WithReplacements(rows);
        var attachment = DoctorStrangeNemesisFixture.Counterspell(world);
        var hero = world.Seats[1].IdentityCard;
        Statuses.Give(world, hero, Statuses.Stunned);
        Statuses.Give(world, hero, Statuses.Confused);
        var source = world.CreateCard("09016", world.Seats[1].Hand);
        var payment = world.CreateCard("01088", world.Seats[1].Hand);
        var top = world.CreateCard("09007", world.Seats[1].Deck);
        var action = Assert.Single(world.Abilities.Actions(world, 1), offer => offer.Card == source.ObjectId);
        var events = world.Abilities.ExecuteAction(world,
            new PlayerAction(action, [payment.ObjectId], [])).Events.ToList();
        if (!labeled)
        {
            var order = Sequence.Work(world, world.Facts, world.Abilities, events)!;
            Assert.Equal(Question.Order, order.Asking);
            Sequence.Answer(world, world.Facts, world.Abilities, order,
                new Decision(Assert.Single(order.Affordances).Id, [0, 1]), events);
        }
        Sequence.Finish(world, world.Facts, world.Abilities, events);
        Assert.Equal(!labeled, Statuses.Has(world, hero, Statuses.Stunned));
        Assert.Equal(!labeled, Statuses.Has(world, hero, Statuses.Confused));
        Assert.False(hero.Ready);
        Assert.Equal(labeled ? DeckType.UpgradesArea : DeckType.EncounterDiscardPile, attachment.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Same(world.Seats[1].Deck, top.Area);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
