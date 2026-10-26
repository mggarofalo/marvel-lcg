using System.Text.Json.Nodes;
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

public sealed class BlackWidowPreventionReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:prevent.1")]
    public void DefensiveStanceProtectsItsHeroFromActualNonattackDamage(bool decline)
    {
        // "Reduce the amount of damage the target takes." Printed 08032 is
        // not attack-labeled; Concussive Blast deals damage to each friendly character.
        var f = new BlackWidowPreventionFixture();
        var source = f.Upgrade("08032");
        var suit = f.Upgrade("08009");
        f.Hero.Exhaust();
        Statuses.Give(f.World, f.Hero, Statuses.Stunned);
        var treachery = Reveal(f, "01154");
        var prompt = f.Find(source);
        Assert.Equal(1, prompt.Player);
        Assert.Equal(f.Hero.ObjectId, f.World.Agenda.Occurrence!.Target);
        if (decline) f.Answer(prompt, Decision.Decline);
        else
        {
            f.Use(prompt, source);
            var completion = f.Find(suit);
            Assert.Equal("08032", f.World.Agenda.Occurrence!.ActorFacts!.PreparationAbility!.Face);
            f.Use(completion, suit);
        }
        f.Finish(source, suit);
        Assert.Equal(decline ? 1 : 0, f.Hero.Damage);
        Assert.Equal(1, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(decline ? DeckType.UpgradesArea : DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(!decline, f.Hero.Ready);
        Assert.Equal(decline, suit.Ready);
        Assert.True(Statuses.Has(f.World, f.Hero, Statuses.Stunned));
        Assert.Equal(DeckType.EncounterDiscardPile, treachery.Area.Type);
        Assert.Empty(f.Events.OfType<AttackCompleted>());
        Assert.False(f.World.Agenda.IsBusy);
        Assert.False(f.World.IsOver);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cancel.4")]
    public void GrapplingHookCancelsActualInciteAndWholeTreacheryOrDeclines(bool decline)
    {
        // A canceled treachery is "still regarded as revealed" and discarded.
        // Ruling 48dd96f060641cb8 includes Incite among the canceled effects.
        var f = new BlackWidowPreventionFixture();
        var source = f.Upgrade("08008");
        foreach (var seat in f.World.Seats)
            f.World.CreateCard("01088", seat.Hand);
        var main = f.World.TheCardIn(DeckType.MainSchemesArea)!;
        var hands = f.World.Seats.Select(seat => seat.Hand.Cards.Count).ToArray();
        var treachery = Reveal(f, "04106");
        var prompt = f.Find(source);
        Assert.Equal(1, prompt.Player);
        Assert.Equal(treachery.ObjectId, f.World.Agenda.Occurrence!.Subject);
        if (decline) f.Answer(prompt, Decision.Decline); else f.Use(prompt, source);
        if (decline)
        {
            var order = f.Work();
            Assert.Equal(Question.Order, order.Asking);
            Assert.Equal(2, order.Affordances.Count);
            f.Answer(order, Decision.Take(order.Affordances[0].Id));
            order = f.Work();
            Assert.Equal(Question.Order, order.Asking);
            f.Answer(order, Decision.Take(Assert.Single(order.Affordances).Id,
                f.World.Seats.Select(seat => seat.IdentityCard.ObjectId).ToArray(), []));
        }
        f.Finish(source);
        Assert.Equal(decline ? 1 : 0, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.All(f.World.Seats, seat => Assert.Equal(decline ? 1 : 0, seat.IdentityCard.Damage));
        Assert.Equal(hands.Select(count => count - (decline ? 1 : 0)),
            f.World.Seats.Select(seat => seat.Hand.Cards.Count));
        Assert.Equal(decline ? DeckType.UpgradesArea : DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, treachery.Area.Type);
        Assert.False(f.World.Agenda.IsBusy);
        Assert.False(f.World.IsOver);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:target.3.4")]
    [Rule("rr:prevent.2")]
    public void FullyPreventedThreatStillAllowsAnotherEffectOnTheSameTarget(bool reverseOrder)
    {
        // Synthetic observer: a target is valid if "at least one of those effects
        // can affect the target" after threat is reduced "before it is placed".
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        var row = book["cards"]!.AsArray().Single(r => r!["card"]!.GetValue<string>() == "01008")!;
        row["abilities"] = JsonNode.Parse("""
            [{"trigger":{"event":"WhenThreatWouldBePlaced","timing":"Interrupt","subject":"game"},
              "cost":{"discard":"this"},"effect":{"and":[{"preventThreat":3},
              {"removeThreat":{"scheme":"trigger.target","amount":1}}]}}]
            """);
        var f = new BlackWidowPreventionFixture(runner: new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString())));
        var first = f.World.CreateCard("08017", f.World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var composite = f.Upgrade("01008");
        var second = f.Upgrade("08017");
        var main = f.World.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 1);
        Threat.Schedule(f.World, new([main], f.Villain, 1, ThreatCause.CardAbility, "review composite", 0));
        f.Use(f.Find(first), first);
        var prompt = f.Find(composite);
        Assert.Equal(1, prompt.Player);
        Assert.Equal(0, f.World.Agenda.Occurrence!.Threat!.Remaining);
        Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == second.ObjectId);
        f.Use(prompt, composite);
        var order = f.Work();
        Assert.Equal(Question.Order, order.Asking);
        f.Answer(order, Decision.Take(Assert.Single(order.Affordances).Id,
            reverseOrder ? [1, 0] : [0, 1], []));
        f.Finish(second);
        Assert.Equal(0, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.DiscardPile, composite.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, second.Area.Type);
        Assert.False(f.World.Agenda.IsBusy);
        Assert.False(f.World.IsOver);
    }

    private static Card Reveal(BlackWidowPreventionFixture f, string face)
    {
        var card = f.World.CreateCard(face, f.World.AreaOf(DeckType.RevealingArea));
        f.World.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 2, Subject: card.ObjectId, Seat: 1));
        return card;
    }
}
