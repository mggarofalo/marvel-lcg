using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Authored Pummel activation contracts; full scenario admission remains separate.</summary>
public sealed class WreckingPummelTests : CoreCardTestBase
{
    [Rule("rr:lasting-effects.5")]
    [Rule("rr:confuse-confused.6")]
    [Theory]
    [InlineData("07032", false, true, 2, false)]
    [InlineData("07032", true, true, 4, false)]
    [InlineData("07033", false, true, 3, false)]
    [InlineData("07033", true, true, 5, false)]
    [InlineData("07032", false, false, 2, false)]
    [InlineData("07032", true, false, 4, false)]
    [InlineData("07033", false, false, 2, false)]
    [InlineData("07033", true, false, 4, false)]
    [InlineData("07032", true, false, 0, true)]
    [InlineData("07033", false, false, 0, true)]
    public void RevealUsesPiledriverAndOnlyHisToughStatusForThisActivation(
        string face, bool tough, bool hero, int expected, bool confused)
    {
        // 07044: "Piledriver attacks you. If Piledriver is tough, he gets +2 ATK
        // for this activation." In alter-ego: "Piledriver schemes. If Piledriver
        // is tough, he gets +2 SCH for this activation."
        // Lasting effects: "expires as soon as its specified duration ends."
        // Confused: "If a confused villain or minion would scheme, discard the confused status card instead."
        var world = Hero(hero ? "01001a" : "01001b");
        world.Seats[0].IdentityCard.TurnTo(hero ? "01001a" : "01001b");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard("07001b", world.AreaOf(DeckType.MainSchemesArea));
        var piledriver = world.CreateCard(face, world.AreaOf(DeckType.VillainArea));
        var thunderball = world.CreateCard("07017", world.AreaOf(DeckType.VillainArea));
        var source = world.CreateCard("07044", world.AreaOf(DeckType.RevealingArea));
        var own = Group(world, piledriver, "07034", source);
        var other = Group(world, thunderball, "07019");
        world.BindEncounterTopology(new EncounterTopology(world, [own, other], thunderball));
        Statuses.Give(world, tough ? piledriver : thunderball, Statuses.Tough);
        if (confused) Statuses.Give(world, piledriver, Statuses.Confused);
        Assert.True(Forms.In(world, world.Seats[0], Cards, hero ? Forms.Hero : Forms.AlterEgo));
        Assert.Same(piledriver, new AbilitySelectorEvaluation(new AbilityQueryContext(
            world, source, new Occurrence(0, []), 0, source.Incarnation, null, null, null, []))
            .Find(new AbilityCardSelection.Titled("Piledriver")));

        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: source.ObjectId, Seat: 0));
        var events = new List<Marvel.Rules.Events.GameEvent>();
        var prompt = Sequence.Work(world, Cards, runner, events);
        for (int steps = 0; prompt is not null; steps++)
        {
            Assert.True(steps < 10);
            Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
            prompt = Sequence.Work(world, Cards, runner, events);
        }

        if (hero)
            Assert.Contains(events.OfType<Marvel.Rules.Events.AttackBoundary>(),
                boundary => !boundary.Started && boundary.Source == piledriver.ObjectId);
        AssertThreatAndDamage(world, own, other, hero, expected);
        AssertActivationEnded(world, piledriver, thunderball, face);
        AssertBoostOwnership(own, other, confused);
    }

    private static void AssertThreatAndDamage(World world, EncounterDeckGroup own,
        EncounterDeckGroup other, bool hero, int expected)
    {
        Assert.Equal(hero ? expected : 0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(hero ? 0 : expected, own.Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, other.Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }

    private static void AssertActivationEnded(World world, Card piledriver, Card thunderball, string face)
    {
        Assert.Same(thunderball, EncounterRouting.Villain(world));
        Assert.Equal(face == "07032" ? 2 : 3, StateFields.Modified(world, piledriver, "attack", Cards, 1));
        Assert.Equal(2, StateFields.Modified(world, piledriver, "scheme", Cards, 1));
        Assert.Equal(0, thunderball.Damage);
        Assert.Equal(0, Statuses.Count(world, piledriver, Statuses.Confused));
    }

    private static void AssertBoostOwnership(EncounterDeckGroup own, EncounterDeckGroup other, bool confused)
    {
        // pack:mc03:new-rules: "When the villain is dealt a boost card,
        // it is dealt from the active villain's deck."
        Assert.Equal(confused ? 0 : 1, other.Discard.Cards.Count(card => card.FaceId == "07037"));
        Assert.Equal(2, own.Deck.Cards.Count);
        Assert.Equal(confused ? 2 : 1, other.Deck.Cards.Count);
    }

    private static EncounterDeckGroup Group(World world, Card villain, string scheme, Card? source = null)
    {
        var deck = world.CreateArea(DeckType.EncounterDeck);
        var discard = world.CreateArea(DeckType.EncounterDiscardPile);
        // Actual zero-icon encounter card; each villain owns its physical copy.
        var boost = world.CreateCard("07037", deck);
        var spare = world.CreateCard("07037", deck);
        return new(villain, world.CreateCard(scheme, world.AreaOf(DeckType.SideSchemesArea)),
            deck, discard, source is null ? [boost, spare] : [boost, spare, source]);
    }
}
