using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual authored rows on nonpublic research boards; Green Goblin remains excluded.
public sealed class MutagenStageAbilityTests : CoreCardTestBase
{
    [Theory]
    [InlineData("02014", 1, false)]
    [InlineData("02015", 1, false)]
    [InlineData("02016", 2, false)]
    [InlineData("02016", 0, true)]
    public void AttackThreatRequiresActualIdentityDamage(string face, int threat, bool tough)
    {
        // Printed: "After Green Goblin attacks and damages you"; prevention leaves no damage.
        var world = Hero("01001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var scheme = world.CreateCard("02018b", world.AreaOf(DeckType.MainSchemesArea));
        var villain = world.CreateCard(face, world.AreaOf(DeckType.VillainArea));
        for (int i = 0; i < 10; i++) world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        if (tough) Statuses.Give(world, world.Seats[1].IdentityCard, Statuses.Tough);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: villain.ObjectId, Seat: 1));
        Finish(world);
        Assert.Equal(threat, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(!tough, world.Seats[1].IdentityCard.Damage > 0);
    }

    [Theory]
    [InlineData("02015", 2)]
    [InlineData("02016", 3)]
    public void StageRevealDealsWithoutRevealingToEveryPlayer(string face, int count)
    {
        // Printed: "Deal 2 [3] encounter cards to each player."
        var world = Hero("01001a", 4);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var villain = world.CreateCard(face, world.AreaOf(DeckType.VillainArea));
        for (int i = 0; i < 20; i++) world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        runner.ExecuteReveal(world, villain, 2);
        Finish(world);
        Assert.All(world.Seats, seat => Assert.Equal(count,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(seat.Index)).Cards.Count));
        Assert.Empty(world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.All(world.Seats, seat => Assert.Equal(0, seat.IdentityCard.Damage));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:when-completed-abilities.1")]
    [Rule("rr:main-scheme-main-scheme-deck.2.1")]
    public void CompletionSkipsEngagedGoblinAndRecruitsFirstPhysicalMatchBeforeAdvancement(bool reset)
    {
        // "When Completed" is a Forced Interrupt. 02017b: "each player not engaged with a
        // Goblin minion must discard 3 cards ... put the first Goblin minion ... into play".
        // 02018b: "If this stage is completed, the players lose the game."
        var world = Hero("01001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.FirstPlayer = 1;
        var scheme = world.CreateCard("02017b", world.AreaOf(DeckType.MainSchemesArea));
        var next = world.CreateCard("02018a,02018b", world.AreaOf(DeckType.MainSchemesDeck));
        var engaged = world.CreateCard("02024", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        if (!reset) world.CreateCard("01186", deck);
        var later = world.CreateCard("02024", deck);
        var first = world.CreateCard("02023", deck);
        world.CreateCard("01186", deck);
        Threat.Place(world, Cards, runner, scheme, 14, "test", []);
        Finish(world);
        Assert.Equal(DeckType.EngagedEnemiesArea, first.Area.Type);
        Assert.Equal(PlayArea.Of(1), first.Area.PlayArea);
        Assert.NotEqual(DeckType.EngagedEnemiesArea, later.Area.Type);
        Assert.Equal(PlayArea.Of(0), engaged.Area.PlayArea);
        Assert.Equal(reset ? 2 : 1, deck.Cards.Count);
        Assert.Equal("02018b", next.FaceId);
        Assert.Equal(8, next.Tokens["k_threat"]);
        Assert.Equal(Outcome.Unfinished, world.Result);
        Threat.Place(world, Cards, runner, next, 14, "test", []);
        Finish(world);
        Assert.Equal(Outcome.VillainWins, world.Result);
    }

    [Theory]
    [InlineData("11017")]
    public void UnauthoredEncounterFaceFailsClosed(string face)
    {
        var world = Hero("01001a");
        var card = world.CreateCard(face, world.AreaOf(DeckType.RevealingArea));
        var error = Assert.Throws<RulesNotImplementedException>(() =>
            AuthoredCards.Runner().ExecuteReveal(world, card, 0));
        Assert.Contains("no ability data", error.Message, StringComparison.Ordinal);
    }

    private static void Finish(World world)
    {
        var runner = AuthoredCards.Runner();
        for (int i = 0; i < 30; i++)
        {
            var prompt = Sequence.Work(world, Cards, runner, []);
            if (prompt is null) return;
            Decision answer;
            if (prompt.Cancellable) answer = Decision.Decline;
            else
            {
                Assert.Equal(Question.Order, prompt.Asking);
                var offer = Assert.Single(prompt.Affordances);
                answer = Decision.Take(offer.Id, offer.Targets!.Legal, []);
            }
            Sequence.Answer(world, Cards, runner, prompt, answer, []);
        }
        Assert.Fail("Mutagen stage procedure did not settle.");
    }
}
