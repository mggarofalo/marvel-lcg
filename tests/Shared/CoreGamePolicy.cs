using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Tests;

/// <summary>A deterministic policy that tries to defeat the core villain.</summary>
/// <remarks>
/// The encounter-deck policy declines every optional choice. This policy is
/// its complement: it pays for cards and actions, changes to hero form, and
/// directs attacks at the villain when the rules permit it. It makes no hidden
/// choices and only returns targets and generators offered by the prompt.
/// </remarks>
public sealed class CoreGamePolicy(ICardFacts facts)
{
    private readonly HashSet<string> villainStages = new(StringComparer.Ordinal);

    /// <summary>How many prompts the policy answered.</summary>
    public int Answered { get; private set; }

    /// <summary>How many cards it chose to play.</summary>
    public int CardsPlayed { get; private set; }

    /// <summary>How many player attacks it chose.</summary>
    public int PlayerAttacks { get; private set; }

    /// <summary>How many costs it paid with at least one generator.</summary>
    public int Payments { get; private set; }

    /// <summary>How many resource abilities it used while paying.</summary>
    public int ResourceAbilitiesUsed { get; private set; }

    /// <summary>The villain stages observed in play.</summary>
    public IReadOnlySet<string> VillainStages => villainStages;

    /// <summary>Answers the game's current prompt.</summary>
    public Decision Answer(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        var asked = game.Pending
            ?? throw new InvalidOperationException("a finished game has no prompt to answer");
        var world = game.State;
        Answered++;

        if (world.TheCardIn(DeckType.VillainArea) is { } villain)
        {
            villainStages.Add(villain.FaceId);
        }

        if (asked.Affordances.Any(option =>
                string.Equals(option.Verb, Game.ResolveMulligans, StringComparison.Ordinal)))
        {
            return Decision.Decline;
        }

        if (asked.Asking == Question.Defender)
        {
            var defender = asked.Affordances.LastOrDefault(option => option.IsLegal);
            return defender is null ? Decision.Decline : Taking(game, defender, []);
        }

        if (asked.Asking is Question.Element or Question.Option or Question.Order)
        {
            var choice = asked.Affordances.First(option => option.IsLegal);
            return Taking(game, choice, Payment(choice) ?? []);
        }

        var ending = asked.Affordances.FirstOrDefault(option =>
            option.IsLegal
            && string.Equals(option.Verb, Game.EndPhaseVerb, StringComparison.Ordinal));
        if (ending is not null)
        {
            return Taking(game, ending, [], Excess(world, asked.Player));
        }

        if (asked.Asking == Question.TurnOption)
        {
            return Turn(game, asked);
        }

        return asked.Cancellable
            ? Decision.Decline
            : Taking(game, asked.Affordances.First(option => option.IsLegal), []);
    }

    private Decision Turn(Game game, Prompt asked)
    {
        var world = game.State;
        var seat = world.Seats[asked.Player];
        bool hero = Forms.In(world, seat, facts, Forms.Hero);

        if (!hero && ResourceAbilityPlay(asked, seat.IdentityCard.ObjectId) is { } resourcePlay)
        {
            return Taking(game, resourcePlay.Option, resourcePlay.Payment);
        }

        if (!hero && Find(asked, Game.ChangeForm) is { } change)
        {
            return Taking(game, change, []);
        }

        if (Find(asked, Game.ActionVerb) is { } action
            && Payment(action) is { } actionPayment)
        {
            return Taking(game, action, actionPayment);
        }

        if (asked.Affordances.FirstOrDefault(option =>
                option.IsLegal
                && string.Equals(option.Verb, CardPlay.Verb, StringComparison.Ordinal)
                && Payment(option) is not null) is { } play)
        {
            return Taking(game, play, Payment(play)!);
        }

        long threat = world.TheCardIn(DeckType.MainSchemesArea)
            ?.Tokens.GetValueOrDefault("k_threat") ?? 0;
        string preferredPower = threat >= 5
            ? BasicPowers.ThwartVerb
            : BasicPowers.AttackVerb;
        var power = Find(asked, preferredPower)
            ?? Find(asked, BasicPowers.ThwartVerb)
            ?? Find(asked, BasicPowers.AttackVerb);

        return power is null ? Decision.Decline : Taking(game, power, []);
    }

    private Decision Taking(
        Game game,
        Affordance option,
        IReadOnlyList<int> payment,
        IReadOnlyList<int>? targets = null)
    {
        var world = game.State;
        if (string.Equals(option.Verb, CardPlay.Verb, StringComparison.Ordinal))
        {
            CardsPlayed++;
        }

        if (string.Equals(option.Verb, BasicPowers.AttackVerb, StringComparison.Ordinal))
        {
            PlayerAttacks++;
        }

        if (payment.Count > 0)
        {
            Payments++;
            var cardsInHands = world.Seats
                .SelectMany(seat => seat.Hand.Cards)
                .Select(card => card.ObjectId)
                .ToHashSet();
            ResourceAbilitiesUsed += payment.Count(id => !cardsInHands.Contains(id));
        }

        return Decision.Take(option.Id, targets ?? Targets(world, option), payment);
    }

    private static Affordance? Find(Prompt asked, string verb) =>
        asked.Affordances.FirstOrDefault(option =>
            option.IsLegal && string.Equals(option.Verb, verb, StringComparison.Ordinal));

    private static (Affordance Option, IReadOnlyList<int> Payment)? ResourceAbilityPlay(
        Prompt asked,
        int identity)
    {
        foreach (var option in asked.Affordances.Where(option =>
                     option.IsLegal
                     && string.Equals(option.Verb, CardPlay.Verb, StringComparison.Ordinal)))
        {
            if (Payment(option) is [var source] payment && source == identity)
            {
                return (option, payment);
            }
        }

        return null;
    }

    private static IReadOnlyList<int> Targets(World world, Affordance option)
    {
        if (option.Targets is not { } wanted)
        {
            return [];
        }

        if (wanted.IsGrouped && wanted.Groups is { Count: > 0 } groups)
        {
            return groups[0];
        }

        var selected = wanted.Legal.Take(wanted.Min).ToList();
        int villain = world.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        if (wanted.Min == 1 && wanted.Legal.Contains(villain))
        {
            selected[0] = villain;
        }

        return selected;
    }

    private static IReadOnlyList<int> Excess(World world, int player)
    {
        var seat = world.Seats[player];
        long limit = PhaseEnd.HandSize(world, seat, facts: world.Facts);
        return [.. seat.Hand.Cards
            .Take(Math.Max(0, seat.Hand.Cards.Count - (int)limit))
            .Select(card => card.ObjectId)];
    }

    private static IReadOnlyList<int>? Payment(Affordance option)
    {
        if (option.CostOptions.Count == 0)
        {
            return [];
        }

        var price = option.CostOptions[0];
        long cost = long.Parse(
            price.Cost,
            System.Globalization.CultureInfo.InvariantCulture);
        string required = string.Concat(price.Rule ?? []);

        for (int count = 0; count <= price.Generators.Count; count++)
        {
            var chosen = new List<ResourceSource>(count);
            if (Choose(price.Generators, cost, required, count, 0, chosen) is { } payment)
            {
                return payment;
            }
        }

        return null;
    }

    private static IReadOnlyList<int>? Choose(
        IReadOnlyList<ResourceSource> sources,
        long cost,
        string required,
        int remaining,
        int start,
        List<ResourceSource> chosen)
    {
        if (remaining == 0)
        {
            string generated = string.Concat(chosen.Select(source => source.Generates));
            return Resources.Pays(generated, cost, required)
                ? [.. chosen.Select(source => source.Effect)]
                : null;
        }

        for (int index = start; index <= sources.Count - remaining; index++)
        {
            chosen.Add(sources[index]);
            var payment = Choose(
                sources, cost, required, remaining - 1, index + 1, chosen);
            chosen.RemoveAt(chosen.Count - 1);
            if (payment is not null)
            {
                return payment;
            }
        }

        return null;
    }
}
