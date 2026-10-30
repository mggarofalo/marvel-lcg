using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CrossbonesMachineGunAttackTests : CoreCardTestBase
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:you-your.7")]
    [Rule("rr:interrupt.3")]
    [Rule("rr:attack-enemy-activation.step.1")]
    public void CrossbonesAttackSpendsAmmoAndDealsTheDiscardedBoostIcons(int attacked)
    {
        // 04064: "When Crossbones attacks you, remove 1 ammo counter from this card and
        // discard the top card of the encounter deck → take indirect damage equal to the
        // number of boost icons on the discarded card." rr:you-your.7: "For abilities that
        // trigger “after [enemy] attacks you,” “you” refers to the attacked player." The
        // clause is worded for "after"; this "when" interrupt applies it by analogy, since
        // "you" in the same attack can only be the attacked player.
        // rr:interrupt.3: an interrupt "is resolved when its triggering condition initiates,
        // but before that triggering condition resolves", and the attack resolves from
        // rr:attack-enemy-activation step 1, "Give boost card". So the gun discards the top
        // card, Power Drain (02041, three boost icons), and the boost card is the next card,
        // Hard to Keep Down (01104, no boost icons).
        // A gun without ammo cannot pay its cost, so it is the control attack, whose boost
        // card is Power Drain.
        var (world, gun, _) = Board(ammo: 2, attacked);
        var (control, _, _) = Board(ammo: 0, attacked);
        int other = 1 - attacked;

        Assert.Equal(1, gun.Tokens["c_ammo"]);
        Assert.Equal(["02041", "01104"], Faces(world));
        Assert.Equal(["02041"], Faces(control));
        // Crossbones' ATK 1 boosted by Power Drain's three icons.
        Assert.Equal(4, control.Seats[attacked].IdentityCard.Damage);
        // ATK 1 boosted by no icons, plus three indirect damage for the discarded Power Drain.
        Assert.Equal(1 + 0 + 3, world.Seats[attacked].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[other].IdentityCard.Damage);
        Assert.Equal(0, control.Seats[other].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:player-elimination.4")]
    public void LethalGunDamageToTheLastPlayerEndsTheGame()
    {
        // rr:player-elimination.4: "If all players are eliminated, the game ends and the
        // players lose." Power Drain's three icons exceed Captain Marvel's last hit point.
        var (world, gun, events) = Board(ammo: 2, attacked: 0, players: 1, remainingHealth: 1);

        Assert.True(world.Seats[0].Eliminated);
        Assert.True(world.IsOver);
        Assert.Equal(Outcome.PlayersLose, world.Result);
        Assert.Equal(1, gun.Tokens["c_ammo"]);
        Assert.Equal(["02041"], Faces(world));
    }

    [Fact]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:player-elimination.5.1")]
    public void LethalGunDamageToOnePlayerEndsTheAttackAndTheGameContinues()
    {
        // rr:player-elimination.5: "If a player is eliminated partway through the resolution
        // of an ability, resolve the entire ability." rr:player-elimination.5.1: "If a player
        // is eliminated in the middle of an attack, that attack ends." No boost card is dealt.
        var (world, gun, events) = Board(ammo: 2, attacked: 0, players: 2, remainingHealth: 1);

        Assert.True(world.Seats[0].Eliminated);
        Assert.False(world.IsOver);
        Assert.Equal(1, gun.Tokens["c_ammo"]);
        Assert.Equal(["02041"], Faces(world));
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.Null(world.Attack);
        AssertResolvedAndDrained(world, gun, events);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:player-elimination.5.1")]
    public void AnEffectAfterLethalDamageStillResolves(int remainingHealth)
    {
        // rr:player-elimination.5: "resolve the entire ability." A synthetic gun row
        // places 1 threat on the main scheme after its indirect damage; with one hit
        // point left that damage eliminates the attacked player and ends the attack.
        // The 0 case is the unharmed control: the threat is placed after a normal attack.
        var (world, gun, events) = Board(ammo: 2, attacked: 0, players: 2, remainingHealth,
            ThreatAfterDamage());

        Assert.Equal(remainingHealth > 0, world.Seats[0].Eliminated);
        Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
        Assert.False(world.IsOver);
        AssertResolvedAndDrained(world, gun, events);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:response.1")]
    public void AReactionToTheGunDamageResolvesBeforeItsFollowOnEffect(int remainingHealth)
    {
        // A synthetic forced response on Crossbones places 2 threat after damage is dealt.
        // It reacts in the window of the gun's damage, which closes before the gun's
        // synthetic follow-on places its 1 threat, whether the damage eliminates the
        // attacked player (1) or the attack continues normally (0).
        var (world, _, events) = Board(ammo: 2, attacked: 0, players: 2, remainingHealth,
            ThreatAfterDamage(reactToDamage: true));
        var threat = events.OfType<FieldSet>().Where(set => set.Field == "k_threat").ToList();
        int reaction = threat.FindIndex(set => set.Trigger == "WhenDamageDealt");
        int followOn = threat.FindIndex(set => set.Trigger == "WhenEnemyActivates");

        Assert.Equal(remainingHealth > 0, world.Seats[0].Eliminated);
        Assert.InRange(reaction, 0, followOn - 1);
        Assert.Equal((0, 2), (threat[reaction].From, threat[reaction].To));
        Assert.Equal((2, 3), (threat[followOn].From, threat[followOn].To));
    }

    private static void AssertResolvedAndDrained(World world, Card gun, List<GameEvent> events)
    {
        // The ability's window result is recorded once, after its last instruction, and
        // no agenda work (including a suspended continuation) is left behind.
        var result = Assert.Single(events.OfType<AbilityWindowResult>(), happened => happened.Source == gun.ObjectId);
        Assert.Equal(events.FindLastIndex(happened => happened.AbilitySource == gun.ObjectId) + 1,
            events.IndexOf(result));
        Assert.Equal(0, world.Agenda.Count);
    }

    private static AbilityRunner ThreatAfterDamage(bool reactToDamage = false)
    {
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        if (reactToDamage)
            book["cards"]!.AsArray().Single(row => row!["card"]!.GetValue<string>() == "04058")!
                ["abilities"]!.AsArray().Add(JsonNode.Parse("""
                    { "trigger": { "event": "WhenDamageDealt", "timing": "ForcedResponse", "subject": "game" },
                      "effect": { "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 2 } } }
                    """));
        var ability = book["cards"]!.AsArray().Single(row => row!["card"]!.GetValue<string>() == "04064")!
            ["abilities"]![0]!;
        var damage = ability["effect"]!.DeepClone();
        ability["effect"] = new JsonObject
        {
            ["seq"] = new JsonArray(damage, JsonNode.Parse(
                """{ "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } }""")),
        };
        return new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
    }

    private static string[] Faces(World world) =>
        [.. world.AreaOf(DeckType.EncounterDiscardPile).Cards.Select(card => card.FaceId)];

    private static (World World, Card Gun, List<GameEvent> Events) Board(
        int ammo, int attacked, int players = 2, int remainingHealth = 0, AbilityRunner? abilities = null)
    {
        var world = new World(Cards, players);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == 0 ? "01010a,01010b" : "01001a,01001b", seat.Hero);
            for (int i = 0; i < 12; i++) world.CreateCard("01088", seat.Deck);
        }
        // Captain Marvel's printed 12 hit points.
        if (remainingHealth > 0) world.Seats[attacked].IdentityCard.TakeDamage(12 - remainingHealth);
        var crossbones = world.CreateCard("04058", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 6; i++) world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("02041", world.AreaOf(DeckType.EncounterDeck));
        var gun = world.CreateCard("04064", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, crossbones.ObjectId));
        if (ammo > 0) gun.PlaceTokens("c_ammo", ammo);
        var runner = abilities ?? AuthoredCards.Runner();
        world.Abilities = runner;
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Index: attacked, Subject: crossbones.ObjectId, Seat: attacked));
        var events = new RedSkullFixture(world, runner).Finish();
        return (world, gun, events);
    }
}
