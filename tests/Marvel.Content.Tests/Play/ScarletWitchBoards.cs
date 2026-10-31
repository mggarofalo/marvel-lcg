using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic Scarlet Witch boards over the production ability book.</summary>
internal static class ScarletWitchBoards
{
    internal const string Hero = "15001a";
    internal const string AlterEgo = "15001b";
    internal const string Crest = "15009";
    internal const string Luminous = "15025";
    // Printed boost icons: Advance 0, Stampede 1, Charge 2, Sonic Converter 3.
    internal const string ZeroBoost = "01186";
    internal const string OneBoost = "01106";
    internal const string TwoBoost = "01099";
    internal const string ThreeBoost = "01118";
    internal const string Energy = "01088";

    internal static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    /// <summary>Scarlet Witch on <paramref name="face"/>, Rhino, his main scheme and twelve one-boost encounter cards.</summary>
    internal static World Board(string face = Hero, int players = 1)
    {
        var world = new World(Cards, players, 241);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == 0 ? "15001a,15001b" : "01001a,01001b", seat.Hero);
            if (player == 0) seat.IdentityCard.TurnTo(face);
            for (int i = 0; i < 12; i++) world.CreateCard("01087", seat.Deck);
        }
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 12; i++) world.CreateCard(OneBoost, world.AreaOf(DeckType.EncounterDeck));
        world.Abilities = AuthoredCards.Runner();
        return world;
    }

    /// <summary>Put encounter cards on top of the deck; the last one named is the top card.</summary>
    internal static Card[] Stack(World world, params string[] faces) =>
        [.. faces.Select(face => world.CreateCard(face, world.AreaOf(DeckType.EncounterDeck)))];

    internal static Card InPlay(World world, string face, DeckType area, int player = 0) =>
        world.CreateCard(face, world.AreaOf(area, PlayArea.Of(player), cardOwner: player));

    internal static Card Engaged(World world, string face, int player = 0) =>
        world.CreateCard(face, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)));

    internal static Card Reveal(World world, AbilityRunner runner, string face, int player = 0)
    {
        var card = world.CreateCard(face, world.AreaOf(DeckType.RevealingArea));
        world.Abilities = runner;
        runner.ExecuteReveal(world, card, player);
        return card;
    }

    /// <summary>Work the agenda, answering each prompt with <paramref name="answer"/> or declining.</summary>
    internal static List<Prompt> Drive(World world, AbilityRunner runner, Func<Prompt, Decision?> answer)
    {
        var seen = new List<Prompt>();
        for (int step = 0; step < 200 && Sequence.Work(world, world.Facts, runner, []) is { } prompt; step++)
        {
            seen.Add(prompt);
            Sequence.Answer(world, world.Facts, runner, prompt, answer(prompt) ?? Decision.Decline, []);
        }
        return seen;
    }

    /// <summary>The one offer anchored on <paramref name="card"/>, if the prompt has one.</summary>
    internal static Affordance? OfferOn(Prompt prompt, Card card) =>
        prompt.Affordances.SingleOrDefault(offer => offer.AnchorId == card.ObjectId);

    /// <summary>Whether this prompt is the interrupt window of a boost-icon count.</summary>
    internal static bool IsCount(World world, Prompt prompt) =>
        world.Agenda.Occurrence?.Is(BoostIconCounting.Counted) == true && prompt.When == Marvel.Rules.Timing.TimingPriority.Interrupt;
}
