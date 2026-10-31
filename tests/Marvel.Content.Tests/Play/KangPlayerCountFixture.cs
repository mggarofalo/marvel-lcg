using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nonpublic actual-card overlay. The deal fixes physical nemesis associations;
// this bounded fixture does not admit the incomplete Kang product.
public sealed class KangPlayerCountFixture : CoreCardTestBase
{
    public static World Board(int players, bool expert, bool completion = false)
    {
        string[] identities = ["01001b,01001a", "08001b,08001a", "01040b,01040a", "01029b,01029a"];
        string[] names = ["Spider-Man", "Black Widow", "Black Panther", "Iron Man"];
        string[] nemeses = ["01167", "08026", "01157", "01172"];
        var blueprints = new List<CardBlueprint>
        {
            new("11007a,11007b", SetupSlot.MainScheme, -1),
            new(expert ? "11034" : "11001", SetupSlot.Villain, -1),
            new("11008a,11008b", SetupSlot.MainScheme, -1),
            new("11013a,11013b", SetupSlot.MainScheme, -1),
            new(expert ? "11039" : "11006", SetupSlot.SetAside, -1),
        };
        for (int player = 0; player < players; player++)
        {
            blueprints.Add(new(identities[player], SetupSlot.Identity, player));
            blueprints.Add(new(nemeses[player], SetupSlot.Nemesis, player));
            for (int card = 0; card < 12; card++)
                blueprints.Add(new("01088", SetupSlot.PlayerDeck, player));
        }
        for (int index = 0; index < 4; index++)
        {
            blueprints.Add(new($"{11009 + index}a,{11009 + index}b", SetupSlot.SetAside, -1));
            blueprints.Add(new((expert ? 11035 + index : 11002 + index).ToString(
                System.Globalization.CultureInfo.InvariantCulture), SetupSlot.SetAside, -1));
            blueprints.Add(new("11023", SetupSlot.SetAside, -1));
        }
        for (int card = 0; card < 20; card++) blueprints.Add(new("01101", SetupSlot.Encounter, -1));
        var world = WorldSetup.DealWithoutCardAbilities(Cards, blueprints, names.Take(players).ToArray(), 42, expert: expert);
        world.FirstPlayer = players - 1;
        var rows = KangCentralTransitionTests.AbilityRows(expert, false, completion);
        // The overlay admits ordinary Dominion revelation only; its damage
        // prohibition and defeat reward remain outside this packet.
        rows.Add(new { card = "11023", abilities = Array.Empty<object>() });
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(JsonSerializer.Serialize(new { cards = rows })));
        return world;
    }

    public static void Defeat(World world, Card villain, List<GameEvent> events)
    {
        Agendas.Happening(world);
        Marvel.Rules.Play.Defeat.Character(world, Cards, villain, "prepared actual-card defeat", events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    public static void Boundary(World world, int round, List<GameEvent> events)
    {
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(
            PhaseTransitionKind.EndPlayerPhase), round, 4));
    }

    public static string Snapshot(World world, List<GameEvent> events) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(),
        Topology = world.GameAreas.Select(area => new { area.Id,
            Players = area.PlayAreas.OrderBy(play => play.Player).Select(play => play.Player) }),
        Pending = world.Agenda.KangTransitions.Pending.Select(item => new { item.Defeated, item.Source.Id, item.NextPhase }),
        Agenda = world.Agenda.Outstanding.Select(step => new { step.What, step.Round, step.Number, step.Subject, step.Seat }),
        Random = world.Random.Generator.GetState(), world.Random.Generator.WordsConsumed,
        Events = events.Select(item => JsonSerializer.Serialize(item, item.GetType())).ToArray(),
    });
}
