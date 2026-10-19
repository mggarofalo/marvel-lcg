using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Prompts;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PhotographicReflexesTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReflectionCoversEveryDamageInstanceAndEndsWithItsAttack(bool ally)
    {
        // ruling:a5ab2d45d8eb09ac: all damage from initiation through attack conclusion is redirected.
        var (world, villain, attachment) = Board();
        var identity = world.Seats[0].IdentityCard;
        var attacker = ally ? world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0))) : identity;
        Begin(world, attacker, villain);
        var runner = AuthoredCards.Runner();
        var occurrence = world.Agenda.Occurrence!;
        var interrupt = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt));
        runner.ResolveWindow(world, occurrence, interrupt, Decision.Take(interrupt.Card));
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
        // Separate damage sources model an interrupt followed by the attack's damage.
        DamagePlacement.Resolve(world, world.Facts, new(attacker, villain, 1, "interrupt", "Damage"), []);
        DamagePlacement.Resolve(world, world.Facts, new(attacker, villain, 2, "attack", "Damage"), []);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(3, identity.Damage);
        if (ally) Assert.Equal(0, attacker.Damage);
        world.Agenda.Abandon();
        Begin(world, attacker, villain);
        DamagePlacement.Resolve(world, world.Facts, new(attacker, villain, 2, "next", "Damage"), []);
        Assert.Equal(2, villain.Damage);
        Assert.Equal(3, identity.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BasicAttackUsesNormalIdentityToughAndLeavesTaskmastersTough(bool identityTough)
    {
        // Prevention occurs before Taskmaster would take damage; the redirected instance uses the identity's Tough.
        var (world, villain, attachment) = Board();
        var identity = world.Seats[0].IdentityCard;
        Statuses.Give(world, villain, "tough");
        if (identityTough) Statuses.Give(world, identity, "tough");
        BasicPowers.BasicAttack(world, world.Facts, 0, villain, []);
        Sequence.Finish(world, world.Facts, AuthoredCards.Runner(), []);
        Assert.Equal(0, villain.Damage);
        Assert.True(Statuses.Has(world, villain, "tough"));
        Assert.Equal(identityTough ? 0 : 2, identity.Damage);
        Assert.False(Statuses.Has(world, identity, "tough"));
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
        Assert.Null(world.Agenda.PlayerAttackScope());
    }

    [Fact]
    public void ASecondCopyWaitsForAnotherAttack()
    {
        var (world, villain, first) = Board();
        var second = world.CreateCard("04104", first.Area);
        Begin(world, world.Seats[0].IdentityCard, villain);
        var runner = AuthoredCards.Runner();
        var occurrence = world.Agenda.Occurrence!;
        var offered = runner.Waiting(world, occurrence, WindowKind.Interrupt);
        Assert.Equal(2, offered.Count);
        runner.ResolveWindow(world, occurrence, offered[0], Decision.Take(offered[0].Card));
        Assert.Empty(runner.Waiting(world, occurrence, WindowKind.Interrupt));
        Assert.Equal(DeckType.UpgradesArea, second.Area.Type);
        world.Agenda.Abandon();
        Begin(world, world.Seats[0].IdentityCard, villain);
        Assert.Single(runner.Waiting(world, world.Agenda.Occurrence!, WindowKind.Interrupt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RedirectedDamageWaitsForTheIdentitysReplacementChoice(bool prevent)
    {
        var (world, villain, _) = Board();
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var rows = book["cards"]!.AsArray();
        foreach (var row in rows.Where(row => row!["card"]!.GetValue<string>() == "01092").ToArray()) rows.Remove(row);
        rows.Add(JsonNode.Parse("""
            {"card":"01092","abilities":[{
              "trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","target":"you"},
              "effect":{"choose":{"options":[{"preventDamage":"trigger.target"},{"draw":{"player":"you","count":1}}]}}
            }]}
            """));
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        world.Abilities = runner;
        world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        world.CreateCard("01014", world.Seats[0].Deck);
        world.CreateCard("01014", world.Seats[0].Deck);
        BasicPowers.BasicAttack(world, world.Facts, 0, villain, []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(Question.Option, prompt.Asking);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, villain.Damage);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(prompt.Affordances[prevent ? 0 : 1].Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(prevent ? 0 : 2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, villain.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    private static void Begin(World world, Card attacker, Card target)
    {
        world.Agenda.Now(new PhaseStep(new CharacterAttackOperation(new CharacterAttack(attacker.ObjectId, target.ObjectId, 0)), 1, 2));
        world.Agenda.Begin(world, world.Facts);
    }

    private static (World World, Card Villain, Card Attachment) Board()
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        world.Abilities = AuthoredCards.Runner();
        var villain = world.CreateCard("04093", world.AreaOf(DeckType.VillainArea));
        var attachment = world.CreateCard("04104", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, villain.ObjectId));
        return (world, villain, attachment);
    }
}
