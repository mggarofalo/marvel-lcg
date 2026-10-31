using System.Text.Json;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual 90001 entry, activation ownership, status restriction and complete modular admission.</summary>
public sealed class RonanActualCardTests : CoreCardTestBase
{
    [Fact]
    public void AllFiveAuthoredFacesAreAdmittedTogetherWhileKangRemainsExcluded()
    {
        using var data = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")));
        var rows = data.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(AuthoredCards.ExpectedFaceCount, rows.Length);
        Assert.Equal(["90001", "90003", "90005", "90002", "90004"], rows.Select(row => row.GetProperty("card").GetString()!)
            .Where(face => Cards.Pack(face) == "ron"));
        Assert.Equal("Kang (The Conqueror)", Cards.Title("11001"));
        Assert.DoesNotContain("11007a", AuthoredCards.Book.Authored);
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var admitted = RuntimeProductAdmission.Discover(setup, Cards, AuthoredCards.Book.Authored);
        Assert.Equal(["90001", "90002", "90003", "90004", "90005"],
            admitted.Playable.Where(face => Cards.Pack(face) == "ron").Order(StringComparer.Ordinal));
        Assert.Contains("01001a", admitted.Playable);
        Assert.DoesNotContain("11001", admitted.Playable);
    }

    [Fact]
    [Rule("rr:cannot")]
    [Rule("rr:toughness.1")]
    public void ActualEntryGrantsToughAndForbidsOnlyStun()
    {
        // "cannot" is absolute. Toughness: "After this character enters play, give it a tough status card."
        var world = Hero("01001a,01001b");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var ronan = world.CreateCard("90001", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var events = new List<GameEvent>();
        Reveal.EnterPlay(world, Cards, ronan, events, abilities: runner);
        Finish(world, runner, events);
        Assert.Equal(DeckType.EngagedEnemiesArea, ronan.Area.Type);
        Assert.Equal(1, Statuses.Count(world, ronan, Statuses.Tough));
        Assert.Null(Statuses.Inflict(world, Cards, ronan, Statuses.Stunned));
        Assert.False(Statuses.Has(world, ronan, Statuses.Stunned));
        Assert.NotNull(Statuses.Inflict(world, Cards, ronan, Statuses.Confused));
        var mercenary = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.NotNull(Statuses.Inflict(world, Cards, mercenary, Statuses.Stunned));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:boost-boost-icon.2")]
    [Rule("rr:toughness.1")]
    public void BoostEntersOnceWithTheNonFirstActivationRecipientAndSurvivesCleanup(bool attack)
    {
        // "Resolve any Boost abilities"; Toughness gives its status after entry.
        // Printed 90001: "Put Ronan the Accuser into play engaged with you."
        var world = new World(Cards, 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
        }
        world.FirstPlayer = 0;
        if (!attack) world.Seats[1].IdentityCard.TurnTo("01001b");
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 8; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        var ronan = world.CreateCard("90001", world.AreaOf(DeckType.EncounterDeck));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(attack ? new EnemyAttackOperation() : new EnemySchemeOperation(),
            Round: 1, Number: 2, Subject: villain.ObjectId, Seat: 1));
        Finish(world, runner, events);
        Assert.False(world.IsOver);
        Assert.Null(world.Activation);
        Assert.Equal(DeckType.EngagedEnemiesArea, ronan.Area.Type);
        Assert.Equal(PlayArea.Of(1), ronan.Area.PlayArea);
        Assert.Equal(1, Statuses.Count(world, ronan, Statuses.Tough));
        Assert.False(Statuses.Has(world, ronan, Statuses.Stunned));
        Assert.Null(Statuses.Inflict(world, Cards, ronan, Statuses.Stunned));
        Assert.Equal(attack ? 1 : 0, events.OfType<AttackCompleted>().Count());
        Assert.Equal(attack ? 2 : 0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(attack ? 0 : 1, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Single(events.OfType<CardAttached>(), item => item.Host == ronan.ObjectId && item.Trigger == "toughness" && item.Verb == "Enter_Play");
        Assert.DoesNotContain(world.AreaOf(DeckType.EncounterDiscardPile).Cards, card => card == ronan);
    }

    private static void Finish(World world, Marvel.Cards.Run.AbilityRunner runner, List<GameEvent> events)
    {
        for (int i = 0; i < 30; i++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null)
            {
                Assert.Empty(world.Agenda.Outstanding);
                return;
            }
            Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        }
        Assert.Fail("Ronan lifecycle did not finish");
    }
}
