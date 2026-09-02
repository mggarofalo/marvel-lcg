using Marvel.Core.Random;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Sim;

/// <summary>A deterministic research policy that plays cards and attacks.</summary>
internal sealed class ActingPolicy(ICardFacts facts, IReadOnlyList<uint> seatSeeds)
{
    public const string Name = "acting";
    public const int Version = 2;
    public const string Visibility = "full_state";

    private readonly IReadOnlyList<EngineRandom> random =
        [.. seatSeeds.Select(seed => new EngineRandom(seed))];
    private PolicyMetrics pending = new(0, 0, 0, 0);

    public int CardsPlayed { get; private set; }
    public int PlayerAttacks { get; private set; }
    public int Payments { get; private set; }
    public int ResourceAbilitiesUsed { get; private set; }

    public PolicyMetrics Metrics => new(
        CardsPlayed, PlayerAttacks, Payments, ResourceAbilitiesUsed);

    public Decision Answer(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        pending = new PolicyMetrics(0, 0, 0, 0);
        var asked = game.Pending
            ?? throw new InvalidOperationException("a finished game has no prompt to answer");
        var world = game.State;

        if (asked.Affordances.Any(option =>
                string.Equals(option.Verb, Game.ResolveMulligans, StringComparison.Ordinal)))
        {
            return Taking(
                game,
                asked.Affordances.Single(option => option.IsLegal),
                [],
                []);
        }

        if (asked.Asking == Question.Defender)
        {
            var defenders = asked.Affordances.Where(option => option.IsLegal).ToList();
            return defenders.Count == 0
                ? Decision.Decline
                : Taking(game, random[asked.Player].Choice(defenders), []);
        }

        if (asked.Asking is Question.Element or Question.Option or Question.Order)
        {
            var choices = asked.Affordances.Where(option => option.IsLegal).ToList();
            var choice = random[asked.Player].Choice(choices);
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

    public void DecisionResolved()
    {
        CardsPlayed += pending.CardsPlayed;
        PlayerAttacks += pending.PlayerAttacks;
        Payments += pending.Payments;
        ResourceAbilitiesUsed += pending.ResourceAbilitiesUsed;
        pending = new PolicyMetrics(0, 0, 0, 0);
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

        var payableAction = asked.Affordances.FirstOrDefault(option =>
            option.IsLegal
            && string.Equals(option.Verb, Game.ActionVerb, StringComparison.Ordinal)
            && ActingHealth(world, option.AnchorPlayer) > 1
            && Payment(option) is not null);
        if (payableAction is { } action
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
        bool eventInHand = string.Equals(option.Verb, Game.ActionVerb, StringComparison.Ordinal)
            && option.AnchorPlayer >= 0
            && world.Seats[option.AnchorPlayer].Hand.Cards.Any(
                card => card.ObjectId == option.AnchorId)
            && facts.Kind(world.Cards[option.AnchorId].FaceId) == CardKind.Event;
        if (string.Equals(option.Verb, CardPlay.Verb, StringComparison.Ordinal) || eventInHand)
        {
            pending = pending with { CardsPlayed = pending.CardsPlayed + 1 };
        }

        if (string.Equals(option.Verb, BasicPowers.AttackVerb, StringComparison.Ordinal))
        {
            pending = pending with { PlayerAttacks = pending.PlayerAttacks + 1 };
        }

        if (payment.Count > 0)
        {
            pending = pending with { Payments = pending.Payments + 1 };
            var cardsInHands = world.Seats
                .SelectMany(seat => seat.Hand.Cards)
                .Select(card => card.ObjectId)
                .ToHashSet();
            pending = pending with
            {
                ResourceAbilitiesUsed = pending.ResourceAbilitiesUsed
                    + payment.Count(id => !cardsInHands.Contains(id)),
            };
        }

        var values = Values(option);
        return Decision.Take(
            option.Id,
            targets ?? Targets(world, option),
            payment,
            values,
            Allocations(option, payment, values));
    }

    private static Dictionary<string, long> Values(Affordance option) =>
        option.CostOptions
            .SelectMany(cost => cost.VariableRequests)
            .ToDictionary(
                variable => variable.Name,
                variable => variable.Min,
                StringComparer.Ordinal);

    private static IReadOnlyList<ResourceAllocation> Allocations(
        Affordance option,
        IReadOnlyList<int> payment,
        IReadOnlyDictionary<string, long> values) =>
        option.CostOptions
            .Select(cost => ResourcePayment.Allocate(cost, payment, values))
            .FirstOrDefault(allocation => allocation is not null)
        ?? [];

    private static Affordance? Find(Prompt asked, string verb) =>
        asked.Affordances.FirstOrDefault(option =>
            option.IsLegal && string.Equals(option.Verb, verb, StringComparison.Ordinal));

    private long ActingHealth(World world, int player)
    {
        var identity = world.Seats[player].IdentityCard;
        return Damage.Health(world, facts, identity) - identity.Damage;
    }

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
        long cost = long.TryParse(
            price.Cost,
            System.Globalization.CultureInfo.InvariantCulture,
            out long fixedCost)
            ? fixedCost
            : price.VariableRequests.Single(variable =>
                string.Equals(variable.Name, price.Cost, StringComparison.Ordinal)).Min;
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
