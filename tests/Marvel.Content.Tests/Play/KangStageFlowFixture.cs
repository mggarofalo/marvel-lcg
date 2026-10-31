using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// A nonpublic Kang board dealt with the full authored book: stages 1, 2 and 4
/// form the main scheme deck and the stage-3 schemes, every Kang (II), Kang (III)
/// and Kang's Dominion start set aside. Kang's public admission stays closed.
/// </summary>
internal static class KangStageFlowFixture
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    private static readonly string[] Identities = ["01001b,01001a", "08001b,08001a", "01040b,01040a", "01029b,01029a"];
    private static readonly string[] Names = ["Spider-Man", "Black Widow", "Black Panther", "Iron Man"];
    private static readonly string[] Nemeses = ["01167", "08026", "01157", "01172"];

    /// <summary>
    /// Deals the board. The encounter deck holds <paramref name="encounter"/>
    /// in order, then Hydra Mercenaries up to twenty cards.
    /// </summary>
    public static World Board(int players, bool expert, IReadOnlyList<string>? encounter = null,
        List<GameEvent>? events = null, uint seed = 42)
    {
        var blueprints = new List<CardBlueprint>
        {
            new("11007a,11007b", SetupSlot.MainScheme, -1),
            new("11008a,11008b", SetupSlot.MainScheme, -1),
            new("11013a,11013b", SetupSlot.MainScheme, -1),
            new(expert ? "11034" : "11001", SetupSlot.Villain, -1),
            new(expert ? "11039" : "11006", SetupSlot.SetAside, -1),
        };
        for (int player = 0; player < players; player++)
        {
            blueprints.Add(new(Identities[player], SetupSlot.Identity, player));
            blueprints.Add(new(Nemeses[player], SetupSlot.Nemesis, player));
            for (int card = 0; card < 12; card++) blueprints.Add(new("01088", SetupSlot.PlayerDeck, player));
        }
        for (int index = 0; index < 4; index++)
        {
            blueprints.Add(new($"{11009 + index}a,{11009 + index}b", SetupSlot.SetAside, -1));
            blueprints.Add(new((expert ? 11035 + index : 11002 + index).ToString(
                System.Globalization.CultureInfo.InvariantCulture), SetupSlot.SetAside, -1));
            blueprints.Add(new("11023", SetupSlot.SetAside, -1));
        }
        var deck = (encounter ?? []).ToList();
        while (deck.Count < 20) deck.Add("01101");
        blueprints.AddRange(deck.Select(face => new CardBlueprint(face, SetupSlot.Encounter, -1)));
        return WorldSetup.Deal(Cards, blueprints, Names.Take(players).ToArray(), seed, AuthoredCards.Runner(),
            events, expert);
    }

    /// <summary>Defeats a villain inside a bare occurrence and drains the agenda.</summary>
    public static void Defeat(World world, Card villain, List<GameEvent> events)
    {
        Agendas.Happening(world);
        Marvel.Rules.Play.Defeat.Character(world, Cards, villain, "prepared defeat", events);
        Sequence.Finish(world, Cards, world.Abilities, events);
    }

    /// <summary>Ends a player phase and returns the first decision it reaches.</summary>
    public static Prompt? EndPlayerPhase(World world, int round, List<GameEvent> events)
    {
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), round, 4));
        return Sequence.Work(world, Cards, world.Abilities, events);
    }

    /// <summary>Answers the first player's join prompt with the game area holding <paramref name="seat"/>.</summary>
    public static Prompt? JoinAreaOf(World world, Prompt? prompt, int seat, List<GameEvent> events)
    {
        Assert.NotNull(prompt);
        Assert.Equal(KangJoinOperation.Join, prompt.Trigger);
        Assert.Equal(world.FirstPlayer, prompt.Player);
        var destination = world.GameAreaOf(PlayArea.Of(seat))!;
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(destination.Id), events);
        return Sequence.Work(world, Cards, world.Abilities, events);
    }

    /// <summary>The Kang villain in the game area holding <paramref name="seat"/>.</summary>
    public static Card LocalKang(World world, int seat) =>
        Assert.Single(world.AreaOf(DeckType.VillainArea, PlayArea.Of(seat)).Cards);

    /// <summary>The main scheme in the game area holding <paramref name="seat"/>.</summary>
    public static Card LocalScheme(World world, int seat) =>
        Assert.Single(world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(seat)).Cards);

    /// <summary>Deals the board, defeats Kang I and ends the player phase into the split.</summary>
    public static World Split(int players, bool expert, List<GameEvent> events, IReadOnlyList<string>? encounter = null)
    {
        var world = Board(players, expert, encounter, events);
        Defeat(world, Assert.Single(world.AreaOf(DeckType.VillainArea).Cards), events);
        Assert.Null(EndPlayerPhase(world, 1, events));
        Assert.Null(world.GameAreaOf(PlayArea.Villains));
        return world;
    }

    /// <summary>Runs one villain phase, declining every optional decision.</summary>
    public static void VillainPhase(World world, int round, List<GameEvent> events)
    {
        Marvel.Rules.Play.VillainPhase.Schedule(world.Agenda, round);
        var prompt = Sequence.Work(world, Cards, world.Abilities, events);
        for (int step = 0; prompt is not null; step++)
        {
            Assert.True(step < 80, prompt.Description);
            var decision = prompt.Cancellable ? Decision.Decline : Decision.Take(prompt.Affordances[0].Id);
            Sequence.Answer(world, Cards, world.Abilities, prompt, decision, events);
            prompt = Sequence.Work(world, Cards, world.Abilities, events);
        }
    }
}
