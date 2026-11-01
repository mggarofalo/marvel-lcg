using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic Brotherhood of Badoon boards over the production ability book.</summary>
internal static class BadoonBoards
{
    internal const string DrangOne = "16058";
    internal const string DrangTwo = "16059";
    internal const string DrangThree = "16060";
    internal const string TerrestrialInvasion = "16061a,16061b";
    internal const string ProtectThePlanet = "16062a,16062b";
    internal const string BadoonShip = "16063";
    internal const string Spear = "16064";
    internal const string Engineer = "16065";
    internal const string Assassin = "16117";
    internal const string Grunt = "16118";
    internal const string Lieutenant = "16119";
    internal const string Sentry = "16120";
    internal const string Warlord = "16121";

    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    /// <summary>
    /// Spider-Man in every seat (seat 0 on <paramref name="face"/>) against <paramref name="villain"/>
    /// and <paramref name="scheme"/> on its B side. Each deck holds twelve Energy; the encounter
    /// deck holds Hard to Keep Down, which prints no boost.
    /// </summary>
    internal static World Board(int players = 1, string face = "01001a", string villain = DrangOne,
        string scheme = TerrestrialInvasion)
    {
        var world = new World(Cards, players, 241);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
            if (player == 0) seat.IdentityCard.TurnTo(face);
            for (int i = 0; i < 12; i++) world.CreateCard("01088", seat.Deck);
        }
        world.CreateCard(villain, world.AreaOf(DeckType.VillainArea));
        world.CreateCard(scheme, world.AreaOf(DeckType.MainSchemesArea)).TurnTo(scheme.Split(',')[1]);
        for (int i = 0; i < 12; i++) world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        world.Abilities = AuthoredCards.Runner();
        return world;
    }

    /// <summary>The Badoon Ship environment in play.</summary>
    internal static Card Ship(World world) => world.CreateCard(BadoonShip, world.AreaOf(DeckType.EnvironmentArea));

    /// <summary>The barrage counters on <paramref name="ship"/>.</summary>
    internal static long Barrage(Card ship) => ship.Tokens.GetValueOrDefault("c_barrage");

    /// <summary>Puts <paramref name="face"/> on top of the encounter deck.</summary>
    internal static Card OnTop(World world, string face) => world.CreateCard(face, world.AreaOf(DeckType.EncounterDeck));

    /// <summary>
    /// Drains pending work: <paramref name="answer"/> first, then indirect damage to each
    /// identity, the first offered order or mandatory option, and declines everything else.
    /// </summary>
    internal static List<GameEvent> Drive(World world, Func<Prompt, Decision?>? answer = null)
    {
        var events = new List<GameEvent>();
        var runner = GrootBoards.Runner(world);
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        for (int step = 0; prompt is not null; step++)
        {
            Assert.True(step < 80, prompt.Description);
            var decision = answer?.Invoke(prompt) ?? GrootEncounterTests.AssignTo(world.Seats[prompt.Player].IdentityCard)(prompt)
                ?? (prompt.Asking == Question.Order && prompt.Affordances[0].Targets is { } order
                    ? new Decision(prompt.Affordances[0].Id, order.Legal)
                    : prompt.Cancellable ? Decision.Decline
                    : Decision.Take(prompt.Affordances[0].Id));
            Sequence.Answer(world, world.Facts, runner, prompt, decision, events);
            prompt = Sequence.Work(world, world.Facts, runner, events);
        }
        return events;
    }
}
