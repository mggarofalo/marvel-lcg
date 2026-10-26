using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nonpublic research boards; no scenario admission is implied.
public sealed class RiskyBusinessEncounterAbilityTests : CoreCardTestBase
{
    [Theory]
    [InlineData("02007", false)]
    [InlineData("02008", false)]
    [InlineData("02009", false)]
    [InlineData("02011", false)]
    [InlineData("02012", false)]
    [InlineData("02007", true)]
    [InlineData("02008", true)]
    [InlineData("02009", true)]
    [InlineData("02011", true)]
    [InlineData("02012", true)]
    public void SharedBoostUsesTheLiveEnvironmentFace(string face, bool goblin)
    {
        // Printed boost: place 1 infamy; if unable, remove 1 madness.
        var world = Board(goblin);
        var environment = world.TheCardIn(DeckType.EnvironmentArea)!;
        var source = world.CreateCard(face, world.AreaOf(DeckType.BoostingArea));
        var runner = AuthoredCards.Runner();
        var events = runner.ExecuteBoost(world, source, 1).Events.ToList();
        Finish(world, events);
        Assert.Equal(goblin ? 4 : 6,
            environment.Tokens.GetValueOrDefault(goblin ? "c_madness" : "c_infamy"));
        Assert.Equal(goblin ? "02006b" : "02006a", environment.FaceId);
    }

    [Theory]
    [InlineData(false, 5, 7, "02006a")]
    [InlineData(true, 5, 3, "02006b")]
    [InlineData(true, 1, 4, "02006a")]
    public void DaysWorkPlacesTwoOrRemovesAvailableMadness(bool goblin, int counters,
        int expected, string finalFace)
    {
        // 02012: "Place 2 infamy ... If you cannot, remove 2 madness."
        var world = Board(goblin, counters);
        var source = world.CreateCard("02012", world.AreaOf(DeckType.RevealingArea));
        var events = AuthoredCards.Runner().ExecuteReveal(world, source, 1).Events.ToList();
        Finish(world, events);
        var environment = world.TheCardIn(DeckType.EnvironmentArea)!;
        Assert.Equal(finalFace, environment.FaceId);
        Assert.Equal(expected, environment.Tokens.GetValueOrDefault(
            finalFace == "02006a" ? "c_infamy" : "c_madness"));
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    public void OscorpAddsPerPlayerThreatOnlyForNorman(bool goblin, int additional)
    {
        var world = Board(goblin);
        var source = world.CreateCard("02010", world.AreaOf(DeckType.SideSchemesArea));
        var events = new List<GameEvent>();
        Reveal.EnterPlay(world, Cards, source, events, abilities: world.Abilities);
        var before = source.Tokens.GetValueOrDefault("k_threat");
        events.AddRange(AuthoredCards.Runner().ExecuteReveal(world, source, 1).Events);
        Finish(world, events);
        Assert.Equal(4, before);
        Assert.Equal(before + additional, source.Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void HiredGunChoiceBelongsToRevealingPlayerAndAppliesChosenOption(int option)
    {
        var world = Board(false);
        var boost = world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01187", world.AreaOf(DeckType.EncounterDeck));
        World.MoveToTop(boost, boost.Area);
        var source = world.CreateCard("02007", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();
        var events = runner.ExecuteReveal(world, source, 1).Events.ToList();
        var prompt = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(1, prompt.Player);
        Assert.False(prompt.Cancellable);
        Assert.Equal(2, prompt.Affordances.Count);
        Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(prompt.Affordances[option].Id), events);
        Finish(world, events);
        Assert.Equal(option == 0 ? DeckType.BoostCardsDeck : DeckType.EncounterDeck, boost.Area.Type);
        Assert.Equal(option == 0 ? 5 : 7,
            world.TheCardIn(DeckType.EnvironmentArea)!.Tokens["c_infamy"]);
        if (option == 0) Assert.Equal(world.TheCardIn(DeckType.VillainArea)!.ObjectId, boost.Area.Host);
    }

    [Fact]
    public void HiredGunCannotChooseInfamyWhileStateOfMadnessIsInPlay()
    {
        var world = Board(true);
        world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01187", world.AreaOf(DeckType.EncounterDeck));
        var source = world.CreateCard("02007", world.AreaOf(DeckType.RevealingArea));
        var events = AuthoredCards.Runner().ExecuteReveal(world, source, 1).Events.ToList();
        var prompt = Sequence.Work(world, Cards, AuthoredCards.Runner(), events);
        if (prompt is not null)
        {
            var option = Assert.Single(prompt.Affordances);
            Assert.Equal(1, prompt.Player);
            Sequence.Answer(world, Cards, AuthoredCards.Runner(), prompt, Decision.Take(option.Id), events);
        }
        Finish(world, events);
        Assert.Single(world.AreaOf(DeckType.BoostCardsDeck,
            world.TheCardIn(DeckType.VillainArea)!.Area.PlayArea,
            host: world.TheCardIn(DeckType.VillainArea)!.ObjectId).Cards);
        Assert.Equal(5, world.TheCardIn(DeckType.EnvironmentArea)!.Tokens["c_madness"]);
    }

    [Fact]
    public void NormanMadGeniusMillsOnlyTheRevealingPlayersDeckByTotalInfamy()
    {
        var world = Board(false, 3);
        for (int seat = 0; seat < 2; seat++)
            for (int i = 0; i < 5; i++)
                world.CreateCard("01003", world.AreaOf(DeckType.PlayerDeck, PlayArea.Of(seat), cardOwner: seat));
        var source = world.CreateCard("02013", world.AreaOf(DeckType.RevealingArea));
        var events = AuthoredCards.Runner().ExecuteReveal(world, source, 1).Events.ToList();
        Finish(world, events);
        Assert.Equal(5, world.Seats[0].Deck.Cards.Count);
        Assert.Equal(2, world.Seats[1].Deck.Cards.Count);
        Assert.Equal(3, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1).Cards.Count);
        Assert.DoesNotContain(events, e => e is AttackCompleted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MadGeniusTargetsLeastRemainingHeroAndSurgesOnlyIfStunCancels(bool stunned)
    {
        var world = Board(true);
        world.Seats[1].IdentityCard.TakeDamage(4);
        world.CreateCard("01110", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01110", world.AreaOf(DeckType.EncounterDeck));
        if (stunned) Statuses.Give(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Stunned);
        var source = world.CreateCard("02013", world.AreaOf(DeckType.RevealingArea));
        var events = AuthoredCards.Runner().ExecuteReveal(world, source, 0).Events.ToList();
        Finish(world, events);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(stunned ? 4 : 8, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(stunned ? 0 : 1, events.OfType<AttackCompleted>().Count());
        Assert.Equal(stunned ? 1 : 0, world.AreaOf(DeckType.DealtEncounterCardsDeck,
            PlayArea.Of(0), cardOwner: 0).Cards.Count);
    }

    [Fact]
    [Rule("rr:first-player.1")]
    public void MadGeniusHeroTieAsksNonzeroFirstPlayer()
    {
        // "If an encounter card targets a specific player or card, and there are multiple
        // eligible targets, the first player selects among the eligible options."
        var world = Board(true);
        world.FirstPlayer = 1;
        var source = world.CreateCard("02013", world.AreaOf(DeckType.RevealingArea));
        var events = AuthoredCards.Runner().ExecuteReveal(world, source, 0).Events.ToList();
        var prompt = Sequence.Work(world, Cards, AuthoredCards.Runner(), events)!;
        Assert.Equal(1, prompt.Player);
        Assert.Equal(Question.Element, prompt.Asking);
        Assert.Equal(world.Seats.Select(s => s.IdentityCard.ObjectId).Order(),
            prompt.Affordances.Select(a => a.AnchorId).Order());
    }

    [Fact]
    public void MadGeniusWithNoHeroesSurgesForTheRevealingPlayer()
    {
        var world = Hero("01001b");
        world.Abilities = AuthoredCards.Runner();
        world.CreateCard("02001b", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01118", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01118", world.AreaOf(DeckType.EncounterDeck));
        var source = world.CreateCard("02013", world.AreaOf(DeckType.RevealingArea));
        var events = AuthoredCards.Runner().ExecuteReveal(world, source, 0).Events.ToList();
        Finish(world, events);
        Assert.DoesNotContain(events, e => e is AttackCompleted);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck,
            PlayArea.Of(0), cardOwner: 0).Cards);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    private static World Board(bool goblin, int counters = 5)
    {
        var world = Hero("01001a", 2);
        world.Abilities = AuthoredCards.Runner();
        var villain = world.CreateCard("02001a,02001b", world.AreaOf(DeckType.VillainArea));
        var environment = world.CreateCard("02006a,02006b", world.AreaOf(DeckType.EnvironmentArea));
        if (goblin) { villain.TurnTo("02001b"); environment.TurnTo("02006b"); }
        environment.PlaceTokens(goblin ? "c_madness" : "c_infamy", counters);
        world.CreateCard("02004b", world.AreaOf(DeckType.MainSchemesArea));
        return world;
    }

    private static void Finish(World world, List<GameEvent> events)
    {
        var runner = AuthoredCards.Runner();
        for (int i = 0; i < 16; i++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) return;
            if (prompt.Cancellable)
                Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
            else
                Assert.Fail($"Unexpected prompt {prompt.Asking} for {prompt.Player}");
        }
        Assert.Fail("Encounter ability did not settle.");
    }
}



