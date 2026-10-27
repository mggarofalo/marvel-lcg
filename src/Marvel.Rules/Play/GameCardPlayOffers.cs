using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Describes affordable card plays and their attachment-host prices.</summary>
internal static class GameCardPlayOffers
{
    internal static void Add(Game game, Seat seat, List<Affordance> options)
    {
        foreach (Card card in CardPlayOrigin.Candidates(game.world, seat.Index))
        {
            var hosts = CardPlayLegality.LegalAttachmentTargets(
                game.world, game.facts, seat, card, game.world.CardPlayAbilities);
            var ordinary = CardPayment.Price(game.world, game.facts, seat, card);
            if (hosts is null)
            {
                if (ordinary is not null) options.Add(game.Priced(seat, card, ordinary, null));
                continue;
            }
            var prices = hosts.Select(host => CardPayment.Price(game.world, game.facts, seat, card, host))
                .OfType<CostOption>().ToArray();
            if (prices.Length == 0) continue;
            // Preserve ordinary offer identity when no attachment changes the price.
            if (ordinary is not null && prices.All(price => price.Cost == ordinary.Cost))
                options.Add(game.Priced(seat, card, ordinary, hosts));
            else
                options.Add(game.Priced(seat, card, prices[0], [.. prices.Select(price => price.Target)])
                    with { Costs = prices });
        }
    }
}
