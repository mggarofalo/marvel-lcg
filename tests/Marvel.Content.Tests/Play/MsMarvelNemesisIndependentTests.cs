using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MsMarvelNemesisIndependentTests : ChoosingCardsTestBase
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [Rule("rr:ability.9")]
    public void EdisonActuallyPreventsDamageUntilHisOtherMinionLeaves(int sourceSeat)
    {
        // "The effects of such abilities are active anytime the specific condition is met."
        // Printed 05027 says "cannot take damage"; ruling:3aba0c6abbd5c2ad binds "you" to his engaged player.
        var world = Deal("spider_man", "captain_marvel");
        world.Abilities = AuthoredCards.Runner();
        var edison = world.CreateCard("05027", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var other = world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var source = sourceSeat < 0 ? world.AreaOf(DeckType.VillainArea).Cards.Single() : world.Seats[sourceSeat].IdentityCard;
        DamagePlacement.Resolve(world, Cards, new DamageRequest(source, edison, 1, "probe", "damage"), []);
        Assert.Equal(0, edison.Damage);
        Discard.Card(world, other, "fixture", []);
        DamagePlacement.Resolve(world, Cards, new DamageRequest(source, edison, 1, "probe", "damage"), []);
        Assert.Equal(1, edison.Damage);
        world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        DamagePlacement.Resolve(world, Cards, new DamageRequest(source, edison, 1, "probe", "damage"), []);
        Assert.Equal(1, edison.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:obligation.4")]
    [Rule("rr:surge.1")]
    public void TransferredHomeByDawnCompletesForKamalaIncludingSurge(bool ownPersona)
    {
        // "That player must decide how to resolve the obligation."
        // Surge: "Deal yourself 1 facedown encounter card."
        var world = Deal("captain_marvel", "ms_marvel");
        var runner = AuthoredCards.Runner();
        world.Seats[1].IdentityCard.TurnTo("05001a");
        var foreign = world.CreateCard("05006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var persona = ownPersona ? world.CreateCard("05008", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1)) : null;
        persona?.Exhaust();
        var obligation = world.CreateCard("05025", world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        int kamalaDealt = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards.Count;
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            Round: 1, Number: 4, Subject: obligation.ObjectId, Seat: 0));
        int[] choices = persona is null ? [1, 1] : [1, 1, persona.ObjectId];
        foreach (int id in choices)
        {
            var prompt = Sequence.Work(world, Cards, runner, [])!;
            Assert.NotNull(prompt);
            Assert.Equal(1, prompt.Player);
            Assert.Contains(prompt.Affordances, offer => offer.Id == id);
            Sequence.Answer(world, Cards, runner, prompt, Decision.Take(id), []);
        }
        Assert.Null(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(DeckType.EncounterDiscardPile, obligation.Area.Type);
        Assert.Equal(DeckType.SupportsArea, foreign.Area.Type);
        Assert.Equal(kamalaDealt + (ownPersona ? 0 : 1), world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards.Count);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Equal("05001a", world.Seats[1].IdentityCard.FaceId);
        if (persona is not null) Assert.Equal(DeckType.DiscardPile, persona.Area.Type);
    }
}

