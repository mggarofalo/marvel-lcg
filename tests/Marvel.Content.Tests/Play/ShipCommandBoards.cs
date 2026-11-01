using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic Ship Command and Power Stone boards over the production ability book.</summary>
internal static class ShipCommandBoards
{
    internal const string SpiderMan = "01001a,01001b";
    internal const string Milano = "16142";
    internal const string Energy = "01088";
    internal const string Genius = "01089";
    internal const string Strength = "01090";
    internal const string PowerStone = "16149";

    /// <summary>Spider-Man in every seat (seat 0 on <paramref name="face"/>) against Rhino.</summary>
    internal static World Board(int players = 1, string face = "01001a", string villain = "01094") =>
        GrootBoards.Board(face, players, villain: villain, identity: SpiderMan, others: SpiderMan);

    /// <summary>A Power Stone board: <paramref name="identity"/> in seat 0, Spider-Man in the others.</summary>
    internal static World PowerStoneBoard(string identity, int players = 1, string villain = "01094") =>
        GrootBoards.Board(identity.Split(',')[0], players, villain: villain, identity: identity, others: SpiderMan);

    /// <summary>A scenario-owned <paramref name="face"/> attached to <paramref name="host"/>.</summary>
    internal static Card Attach(World world, Card host, string face)
    {
        var card = world.CreateCard(face, world.AreaOf(DeckType.EncounterDiscardPile));
        World.MoveToTop(card, world.AreaOf(DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId, host.Area.CardOwner));
        return card;
    }

    /// <summary>The Milano, owned by the scenario, in the first player's supports.</summary>
    internal static Card PutMilano(World world)
    {
        var milano = world.CreateCard(Milano, world.AreaOf(DeckType.EncounterDiscardPile));
        World.MoveToTop(milano, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(world.FirstPlayer), cardOwner: world.FirstPlayer));
        return milano;
    }

    /// <summary>Deals <paramref name="face"/> to <paramref name="player"/> and schedules its reveal.</summary>
    internal static Card Reveal(World world, string face, int player = 0)
    {
        var card = world.CreateCard(face, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player)));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            Round: 1, Number: 4, Subject: card.ObjectId, Seat: player));
        return card;
    }

    /// <summary>The actions <paramref name="player"/> is offered on <paramref name="card"/>.</summary>
    internal static IReadOnlyList<PendingAbility> ActionsOn(World world, Card card, int player) =>
        [.. GrootBoards.Runner(world).Actions(world, player).Where(action => action.Card == card.ObjectId)];

    /// <summary>Resolves <paramref name="player"/>'s only action on <paramref name="card"/>, paying with <paramref name="paying"/>.</summary>
    internal static void Act(World world, Card card, int player, params Card[] paying)
    {
        var action = Assert.Single(ActionsOn(world, card, player));
        world.Agenda.AddPlayerAction(player, new PlayerAction(action, [.. paying.Select(c => c.ObjectId)], []));
        GrootBoards.Drive(world, _ => null);
    }

    /// <summary>The resource sources <paramref name="player"/>'s payment offers.</summary>
    internal static IReadOnlyList<int> Sources(World world, int player) =>
        [.. GrootBoards.Runner(world).ResourceAbilities(world, player).Select(source => source.Effect)];

    /// <summary>Answers the next prompt with the affordance <paramref name="choose"/> picks.</summary>
    internal static Prompt Answer(World world, Func<Prompt, Affordance> choose)
    {
        var runner = GrootBoards.Runner(world);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(choose(prompt).Id), []);
        return prompt;
    }

    /// <summary>
    /// Drains pending work: takes the option whose label or description contains
    /// <paramref name="option"/>, pays a spend option with <paramref name="paying"/>, orders
    /// players as offered, takes a sole mandatory option and declines everything else.
    /// </summary>
    internal static List<Prompt> Choose(World world, string? option = null, params Card[] paying)
    {
        var asked = new List<Prompt>();
        GrootBoards.Drive(world, prompt =>
        {
            asked.Add(prompt);
            if (Spend(prompt, paying) is { } spent) return spent;
            if (option is not null && prompt.Affordances.FirstOrDefault(a =>
                    (a.Description ?? "").Contains(option, StringComparison.Ordinal)
                    || a.Label.Contains(option, StringComparison.Ordinal)) is { } chosen)
                return Decision.Take(chosen.Id);
            if (prompt.Asking == Question.Order)
                return new Decision(prompt.Affordances[0].Id, prompt.Affordances[0].Targets!.Legal);
            if (prompt.Affordances.FirstOrDefault(a => a.Label == "effect") is { } resolve)
                return Decision.Take(resolve.Id);
            return !prompt.Cancellable && prompt.Affordances.Count == 1 ? Decision.Take(prompt.Affordances[0].Id) : null;
        });
        return asked;
    }

    /// <summary>Pays the prompt's spend option with <paramref name="paying"/>, when both exist.</summary>
    private static Decision? Spend(Prompt prompt, Card[] paying)
    {
        if (paying.Length == 0 || prompt.Affordances.FirstOrDefault(a => a.Label == "spend") is not { } spend) return null;
        int[] cards = [.. paying.Select(card => card.ObjectId)];
        return Decision.Take(spend.Id, [], cards, new Dictionary<string, long>(),
            ResourcePayment.Allocate(Assert.Single(spend.CostOptions), cards)!);
    }

    /// <summary>The next prompt without answering it.</summary>
    internal static Prompt Next(World world) =>
        Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, GrootBoards.Runner(world), []));
}
