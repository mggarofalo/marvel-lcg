using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual research row; no public scenario admission.
public sealed class MutagenISeeYouTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RevealAttacksEitherFormAndAlterEgoReceivesNoNormalOrAdditionalBoost(bool hero, bool hysteria)
    {
        // Printed 02030: "If you are in alter-ego form, do not give the villain
        // a boost card for this activation." Official FAQ 02030 confirms the
        // alter-ego identity is attacked. 02020 normally adds an extra boost.
        var f = Create(hero);
        if (hysteria)
        {
            f.Reveal("02020", 1);
            f.Finish();
        }
        var deck = f.World.AreaOf(DeckType.EncounterDeck);
        int before = deck.Cards.Count;
        f.Reveal("02030", 1);
        var events = f.Finish();
        var attack = Assert.Single(events.OfType<AttackCompleted>());
        Assert.Equal(f.World.Seats[1].IdentityCard.ObjectId, attack.Target);
        int boosts = hero ? hysteria ? 2 : 1 : 0;
        Assert.Equal(before - boosts, deck.Cards.Count);
        Assert.Equal(2 + boosts, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, f.World.Seats[0].IdentityCard.Damage);
        Assert.Null(f.World.Activation);
        Assert.Equal(Outcome.Unfinished, f.World.Result);
    }

    [Theory]
    [InlineData(false, "none", 0)]
    [InlineData(false, "otherPlayer", 0)]
    [InlineData(false, "nongoblin", 0)]
    [InlineData(false, "one", 1)]
    [InlineData(false, "two", 1)]
    [InlineData(true, "none", 0)]
    [InlineData(true, "otherPlayer", 0)]
    [InlineData(true, "one", 1)]
    public void BoostAddsExactlyOneIconOnlyForTheActivationPlayersEngagedGoblin(bool scheme, string engaged, int bonus)
    {
        // Printed 02030: "+1 boost icon ... if at least one Goblin minion is
        // engaged with you." Two Goblins still grant only one icon.
        var f = Create(hero: !scheme, stage: "02015");
        var deck = f.World.AreaOf(DeckType.EncounterDeck);
        f.World.CreateCard("02024", f.World.AreaOf(DeckType.EncounterDiscardPile));
        if (engaged != "none")
        {
            int player = engaged == "otherPlayer" ? 0 : 1;
            string face = engaged == "nongoblin" ? "01101" : "02024";
            f.World.CreateCard(face, f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)));
            if (engaged == "two") f.World.CreateCard(face, f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)));
        }
        var source = f.World.CreateCard("02030", deck);
        var other = f.World.CreateCard("02030", f.World.AreaOf(DeckType.EncounterDiscardPile));
        int before = deck.Cards.Count;
        AgendaOperation operation = scheme ? new EnemySchemeOperation() : new EnemyAttackOperation();
        f.World.Agenda.Add(new PhaseStep(operation, 1, 2, Subject: f.Villain.ObjectId, Seat: 1));
        var events = f.Finish();
        Assert.Equal(scheme ? 0 : 1, events.OfType<AttackCompleted>().Count());
        Assert.Equal(scheme ? 0 : 3 + bonus, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(scheme ? 3 + bonus : 1, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(before - 1, deck.Cards.Count);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, other.Area.Type);
        Assert.Equal(1, StateFields.Modified(f.World, other, "boost_const", Cards, 2));
        Assert.Equal(1, StateFields.Modified(f.World, source, "boost_const", Cards, 2));
        Assert.Null(f.World.Activation);
        Assert.Equal(Outcome.Unfinished, f.World.Result);
    }

    private static RedSkullFixture Create(bool hero, string stage = "02014")
    {
        var world = Hero("01001a", 2);
        World.MoveToTop(world.Seats[1].IdentityCard, world.AreaOf(DeckType.RemovedArea));
        world.Seats[1].IdentityCard = world.CreateCard(hero ? "01001a" : "01001b", world.Seats[1].Hero);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard(stage, world.AreaOf(DeckType.VillainArea));
        world.CreateCard("02018b", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 8; i++) world.CreateCard("02029", world.AreaOf(DeckType.EncounterDeck));
        return new(world, runner);
    }
}
