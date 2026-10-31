using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic Quicksilver boards over the production ability book.</summary>
internal static class QuicksilverBoards
{
    internal const string Identity = "14001a,14001b";

    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    /// <summary>Quicksilver on <paramref name="face"/> (later seats Spider-Man), Rhino (stage I unless <paramref name="villain"/> says otherwise) and his main scheme.</summary>
    /// <remarks>Each deck holds twelve Energy; the encounter deck holds Hard to Keep Down, which prints no boost.</remarks>
    internal static World Board(string face = "14001a", int players = 1, string encounter = "01104", string villain = "01094")
    {
        var world = new World(Cards, players, 241);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == 0 ? Identity : "01001a,01001b", seat.Hero);
            if (player == 0) seat.IdentityCard.TurnTo(face);
            for (int i = 0; i < 12; i++) world.CreateCard("01088", seat.Deck);
        }
        world.CreateCard(villain, world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 12; i++) world.CreateCard(encounter, world.AreaOf(DeckType.EncounterDeck));
        world.Abilities = AuthoredCards.Runner();
        return world;
    }

    internal static AbilityRunner Runner(World world) => (AbilityRunner)world.Abilities;

    internal static Card Villain(World world) => world.TheCardIn(DeckType.VillainArea)!;

    internal static Card MainScheme(World world) => world.TheCardIn(DeckType.MainSchemesArea)!;

    internal static Card Engaged(World world, string face, int player = 0) =>
        world.CreateCard(face, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)));

    internal static Card Side(World world, string face, int threat)
    {
        var scheme = world.CreateCard(face, world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", threat);
        return scheme;
    }

    internal static Card InPlay(World world, string face, DeckType area, int player = 0) =>
        world.CreateCard(face, world.AreaOf(area, PlayArea.Of(player), cardOwner: player));

    internal static Card Hand(World world, string face, int player = 0) =>
        world.CreateCard(face, world.Seats[player].Hand);

    /// <summary>Plays <paramref name="card"/>'s only action, paying with <paramref name="paying"/>.</summary>
    internal static void Play(World world, Card card, IReadOnlyList<Card> paying, IReadOnlyList<int>? targets = null)
    {
        var runner = Runner(world);
        // An encounter card's action is offered to player 0 here.
        var action = Assert.Single(runner.Actions(world, Math.Max(card.Owner, 0)), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [.. paying.Select(c => c.ObjectId)], targets ?? []));
    }

    /// <summary>Drains pending work, answering with <paramref name="answer"/> and declining other optional prompts.</summary>
    internal static void Drive(World world, Func<Prompt, Decision?> answer)
    {
        var runner = Runner(world);
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        for (int step = 0; prompt is not null; step++)
        {
            Assert.True(step < 80, prompt.Description);
            var decision = answer(prompt)
                ?? (prompt.Cancellable ? Decision.Decline : throw new InvalidOperationException(prompt.Description));
            Sequence.Answer(world, world.Facts, runner, prompt, decision, events);
            prompt = Sequence.Work(world, world.Facts, runner, events);
        }
    }

    /// <summary>Takes the offer anchored to <paramref name="card"/>, if any.</summary>
    internal static Func<Prompt, Decision?> Take(Card card, List<int>? offered = null) => prompt =>
        prompt.Affordances.FirstOrDefault(a => a.AnchorId == card.ObjectId) is { } offer
            ? Record(offered, offer) : null;

    private static Decision Record(List<int>? offered, Affordance offer)
    {
        offered?.Add(offer.Id);
        return Decision.Take(offer.Id);
    }
}
