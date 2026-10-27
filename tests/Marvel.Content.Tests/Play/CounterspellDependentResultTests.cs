using System.Text.Json.Nodes;
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

public sealed class CounterspellDependentResultTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:then.2")]
    public void RepeatedCancellationMakesPreThenPartial(bool repeat)
    {
        // "If the pre-”then” text of an effect does not fully resolve, the
        // post-”then” text does not attempt to resolve."
        // Synthetic sequencing isolates completion; the canonical printed program keeps AND.
        var world = DoctorStrangeNemesisFixture.Board();
        var rows = new JsonArray();
        var row = JsonNode.Parse(AuthoredCards.Text)!["cards"]!.AsArray()
            .Single(node => node!["card"]!.GetValue<string>() == "09030")!.DeepClone();
        rows.Add(row);
        string effects = repeat
            ? "[{\"cancelOccurrence\":\"trigger\"},{\"cancelOccurrence\":\"trigger\"}]"
            : "[{\"cancelOccurrence\":\"trigger\"}]";
        row["abilities"]![0]!["effect"] = JsonNode.Parse(
            "{\"then\":{\"effect\":{\"seq\":" + effects + "},\"then\":{\"discard\":\"this\"}}}");
        world.Abilities = DoctorStrangeAbilityFixture.WithReplacements(rows);
        var attachment = DoctorStrangeNemesisFixture.Counterspell(world);
        var (source, payment) = Event(world);
        Play(world, source, payment, []);
        Sequence.Finish(world, world.Facts, world.Abilities, []);
        Assert.Equal(repeat ? DeckType.UpgradesArea : DeckType.EncounterDiscardPile, attachment.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:triggering-condition.1.1")]
    public void TwoCounterspellsCancelOnlyOneExactEvent()
    {
        // "Multiple copies of a card with an interrupt or response can each be
        // triggered by the same triggering condition."
        var world = DoctorStrangeNemesisFixture.Board();
        var first = DoctorStrangeNemesisFixture.Counterspell(world);
        var second = DoctorStrangeNemesisFixture.Counterspell(world);
        var (source, payment) = Event(world);
        var events = new List<GameEvent>();
        Play(world, source, payment, events);
        for (int i = 0; i < 8; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, world.Abilities, events);
            if (prompt is null) break;
            var offer = prompt.Affordances[0];
            Sequence.Answer(world, world.Facts, world.Abilities, prompt,
                prompt.Asking == Question.Order ? new Decision(offer.Id, [0, 1]) : Decision.Take(offer.Id), events);
        }
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Single(new[] { first, second }, card => card.Area.Type == DeckType.EncounterDiscardPile);
        Assert.Single(new[] { first, second }, card => card.Area.Type == DeckType.UpgradesArea);
        Assert.Single(events.OfType<CardsMoved>(), move => move.Verb == "Discard"
            && move.Cards.Any(card => card.Card == source.ObjectId));
    }

    [Fact]
    public void CancellationQueriesPreservePendingWorkAndSubsequentExecution()
    {
        var probed = Prepare();
        var control = Prepare();
        var world = probed.World;
        var digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        var effects = world.Effects.Registered.ToArray();
        var occurrence = world.Agenda.Occurrence;
        var resolutions = occurrence?.Resolutions.ToArray();
        for (int i = 0; i < 3; i++)
            Assert.Single(world.Abilities!.Actions(world, 1), action => action.Card == probed.Source.ObjectId);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
        Assert.Equal(effects, world.Effects.Registered.ToArray());
        Assert.Same(occurrence, world.Agenda.Occurrence);
        Assert.Equal(resolutions, occurrence?.Resolutions.ToArray());
        var observed = Complete(probed.World, probed.Source, probed.Payment);
        var expected = Complete(control.World, control.Source, control.Payment);
        Assert.Equal(expected, observed);
        Assert.Equal(control.World.Digest().Canonical(), world.Digest().Canonical());
    }

    [Fact]
    public void CancellationRequiresExactOutstandingOccurrenceEvenForSamePhysicalCard()
    {
        var world = DoctorStrangeNemesisFixture.Board();
        var source = world.CreateCard("09016", world.AreaOf(DeckType.RevealingArea, cardOwner: 1));
        var receipt = world.Agenda.NowEventPlayInitiated(1, source.ObjectId, 1);
        world.Agenda.Begin(world, world.Facts);
        var occurrence = world.Agenda.Occurrence!;
        Assert.False(AbilityOccurrenceCancellation.CanResolve(world, occurrence with { }));
        Assert.False(AbilityOccurrenceCancellation.Cancel(world, occurrence with { }));
        Assert.False(receipt.Cancelled);
        Assert.True(AbilityOccurrenceCancellation.Cancel(world, occurrence));
        Assert.True(receipt.Cancelled);
        Assert.False(AbilityOccurrenceCancellation.Cancel(world, occurrence));
        World.MoveToTop(source, world.Seats[1].Hand);
        World.MoveToTop(source, world.AreaOf(DeckType.RevealingArea, cardOwner: 1));
        world.Agenda.NowEventPlayInitiated(1, source.ObjectId, 1);
        Assert.False(AbilityOccurrenceCancellation.CanResolve(world, occurrence));
        Assert.False(AbilityOccurrenceCancellation.Cancel(world, occurrence));
    }

    [Fact]
    public void InactiveAttackBranchDoesNotConsumeStunBeforeNonattackEventCancellation()
    {
        // Synthetic branch control: a nonselected attack supplies no status label.
        var world = DoctorStrangeNemesisFixture.Board();
        var rows = new JsonArray();
        rows.Add(JsonNode.Parse("""
            {"card":"09016","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "effect":{"if":{"test":{"inForm":{"player":"you","form":"hero"}},
                "then":{"draw":{"player":"you","count":1}},
                "else":{"attack":{"target":"chosen","effect":{"dealAttackDamage":{"cards":"chosen","amount":1}}}}}}
            }]}
            """));
        world.Abilities = DoctorStrangeAbilityFixture.WithReplacements(rows);
        var attachment = DoctorStrangeNemesisFixture.Counterspell(world);
        Statuses.Give(world, world.Seats[1].IdentityCard, Statuses.Stunned);
        var (source, payment) = Event(world);
        Complete(world, source, payment);
        Assert.True(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Stunned));
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
    }

    private static (World World, Card Source, Card Payment) Prepare()
    {
        var world = DoctorStrangeNemesisFixture.Board();
        DoctorStrangeNemesisFixture.Counterspell(world);
        var (source, payment) = Event(world);
        return (world, source, payment);
    }

    private static (Card Source, Card Payment) Event(World world)
    {
        world.Seats[1].IdentityCard.TakeDamage(2);
        return (world.CreateCard("09016", world.Seats[1].Hand), world.CreateCard("01088", world.Seats[1].Hand));
    }

    private static void Play(World world, Card source, Card payment, List<GameEvent> events)
    {
        var action = Assert.Single(world.Abilities!.Actions(world, 1), action => action.Card == source.ObjectId);
        events.AddRange(world.Abilities.ExecuteAction(world,
            new PlayerAction(action, [payment.ObjectId], [world.TheCardIn(DeckType.VillainArea)!.ObjectId])).Events);
    }

    private static string[] Complete(World world, Card source, Card payment)
    {
        var events = new List<GameEvent>();
        Play(world, source, payment, events);
        var prompt = Sequence.Work(world, world.Facts, world.Abilities, events)!;
        Assert.Equal(Question.Order, prompt.Asking);
        Sequence.Answer(world, world.Facts, world.Abilities, prompt,
            new Decision(Assert.Single(prompt.Affordances).Id, [0, 1]), events);
        Sequence.Finish(world, world.Facts, world.Abilities, events);
        return events.Select(item => item.ToString()!).ToArray();
    }
}
