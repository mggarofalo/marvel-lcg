using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class JetTrooperTests
{
    private static readonly string Book = Read();

    private static string Read()
    {
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var jet = book["cards"]!.AsArray().Single(row => row!["card"]!.GetValue<string>() == "04146")!.DeepClone();
        var extra = JsonNode.Parse("""
            {"card":"01006","abilities":[{"trigger":{"timing":"ForcedInterrupt","event":"WhenAttackInitiated","actor":"villain"},"effect":{"giveAdditionalBoost":{"enemy":"trigger.actor"}}}]}
            """)!;
        return new JsonObject { ["cards"] = new JsonArray(jet, extra) }.ToJsonString();
    }

    [Theory]
    [InlineData(true, false, 2, 1)]
    [InlineData(true, true, 2, 2)]
    [InlineData(false, false, 0, 1)]
    [Rule("rr:activation.8")]
    public void BoostSchedulesHeroOnlyFollowupWithoutNormalOrAdditionalBoosts(
        bool hero, bool additional, int attacks, int boosts)
    {
        // "Resolves after the current activation has finished resolving."
        var runner = new AbilityRunner(AbilityCatalog.Parse(Book));
        var (_, world) = Playing(_ => { }, hero: hero, abilities: runner);
        if (additional) InPlay(world, "01006");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var ordinary = world.CreateCard("04076", deck);
        var jet = world.CreateCard("04146", deck);
        World.MoveToTop(ordinary, deck);
        World.MoveToTop(jet, deck);
        int before = deck.Cards.Count;
        world.Agenda.Add(new PhaseStep(hero ? new EnemyAttackOperation() : new EnemySchemeOperation(),
            1, 2, Subject: villain.ObjectId, Seat: 0));
        var events = Agendas.Finish(world, world.Facts, runner);
        Assert.Equal(attacks, events.OfType<AttackCompleted>().Count());
        Assert.Equal(before - boosts, deck.Cards.Count);
        Assert.Equal(boosts, events.OfType<CardsMoved>().Count(item => item.Verb == "Boost" && item.From.Zone == nameof(DeckType.EncounterDeck)));
        Assert.Null(world.Activation);
        if (!hero) return;
        // A later ordinary attack receives its own boosts; the prohibition is not a villain flag.
        var next = world.CreateCard("04076", deck);
        World.MoveToTop(next, deck);
        var spare = world.CreateCard("04076", deck);
        World.MoveToTop(spare, deck);
        before = deck.Cards.Count;
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 2, 2, Subject: villain.ObjectId, Seat: 0));
        Agendas.Finish(world, world.Facts, runner);
        Assert.Equal(before - (additional ? 2 : 1), deck.Cards.Count);
    }

    [Theory]
    [InlineData("enemyAttacks", "enemySchemes")]
    [InlineData("\"dealBoostCards\":\"false\"", "\"dealBoostCards\":\"maybe\"")]
    public void InvalidNoBoostEnvelopesAreRejected(string from, string to) =>
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(Book.Replace(from, to))));

    [Fact]
    [Rule("rr:player-elimination.step.5")]
    [Rule("rr:player-elimination.5")]
    public void FollowupAgainstAPlayerTheBoostedAttackEliminatedMakesNoAttack()
    {
        // 04146: "If you are in hero form, the villain attacks you after this activation."
        // The boosted attack defeats that hero first. rr:player-elimination.5: "If a player
        // is eliminated partway through the resolution of an ability, resolve the entire
        // ability." rr:player-elimination.step.5: "Eliminated players no longer participate
        // in the game", so the follow-up has no player to attack and makes no attack.
        var runner = new AbilityRunner(AbilityCatalog.Parse(Book));
        var (_, world) = Playing(_ => { }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var deck = world.AreaOf(DeckType.EncounterDeck);
        World.MoveToTop(world.CreateCard("04146", deck), deck);
        var spider = world.Seats[0].IdentityCard;
        spider.TakeDamage(DamagePlacement.Health(world, world.Facts, spider) - 1);
        int before = deck.Cards.Count;
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: villain.ObjectId, Seat: 0));

        var events = Agendas.Finish(world, world.Facts, runner);

        Assert.True(world.Seats[0].Eliminated);
        Assert.False(world.Seats[1].Eliminated);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.Single(events.OfType<AttackBoundary>(), boundary => boundary.Started);
        Assert.Equal(before - 1, deck.Cards.Count);
        Assert.Null(world.Activation);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Fact]
    [Rule("rr:boost-boost-icon.6")]
    public void ExistingFacedownBoostStillResolvesOnAnAttackThatDealsNoNewBoosts()
    {
        // A card dealt outside its own activation "remains facedown ... until that enemy activates."
        var runner = new AbilityRunner(AbilityCatalog.Parse(Book));
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var hosted = world.CreateCard("04087", world.AreaOf(DeckType.BoostCardsDeck, villain.Area.PlayArea, villain.ObjectId));
        int before = world.AreaOf(DeckType.EncounterDeck).Cards.Count;
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation { DealBoostCards = false },
            1, 2, Subject: villain.ObjectId, Seat: 0));
        var events = Agendas.Finish(world, world.Facts, runner);
        Assert.Equal(before, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Equal(DeckType.EncounterDiscardPile, hosted.Area.Type);
        Assert.DoesNotContain(events.OfType<CardsMoved>(), item => item.Verb == "Boost" && item.From.Zone == nameof(DeckType.EncounterDeck));
        Assert.Equal(2 + world.Facts.PrintedValue(hosted.FaceId, "Boost", 1), world.Seats[0].IdentityCard.Damage);
    }
}
