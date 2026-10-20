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
    [Theory]
    [InlineData("07032", false, 2)]
    [InlineData("07032", true, 4)]
    [InlineData("07033", false, 3)]
    [InlineData("07033", true, 5)]
    public void HeroRevealUsesPiledriverAndOnlyHisToughStatusForThisAttack(string face, bool tough, int expected)
    {
        // 07044: "Piledriver attacks you. If Piledriver is tough, he gets +2 ATK
        // for this activation." Lasting effects: "expires as soon as its specified duration ends."
        var world = Hero("01001a");
        world.Seats[0].IdentityCard.TurnTo("01001a");
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
        Assert.True(Forms.In(world, world.Seats[0], Cards, Forms.Hero));
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

        Assert.Contains(events.OfType<Marvel.Rules.Events.AttackBoundary>(),
            boundary => !boundary.Started && boundary.Source == piledriver.ObjectId);
        Assert.Equal(expected, world.Seats[0].IdentityCard.Damage);
        Assert.Same(thunderball, EncounterRouting.Villain(world));
        Assert.Equal(face == "07032" ? 2 : 3, StateFields.Modified(world, piledriver, "attack", Cards, 1));
        Assert.Equal(0, thunderball.Damage);
        // pack:mc03:new-rules: "When the villain is dealt a boost card,
        // it is dealt from the active villain's deck."
        Assert.Single(other.Discard.Cards, card => card.FaceId == "07037");
        Assert.Equal(2, own.Deck.Cards.Count);
        Assert.Single(other.Deck.Cards);
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
