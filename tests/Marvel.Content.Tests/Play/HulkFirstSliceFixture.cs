using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Tests.Play;

internal static class HulkFirstSliceFixture
{
    // Nonpublic prepared card scene; no Hulk product setup or admission.
    internal static (Game Game, World World) PlayingHulk(Action<World> prepare,
        bool hero = true, int owner = 0, AbilityRunner? runner = null) =>
        ActionAbilitySupportFixtures6.Playing(world =>
        {
            var seat = world.Seats[owner];
            World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea));
            seat.IdentityCard = world.CreateCard("10001a,10001b", seat.Hero);
            if (!hero) seat.IdentityCard.TurnTo("10001b");
            prepare(world);
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner ?? AuthoredCards.Runner());

    internal static void DiscardWholeHand(Game game, World world, int owner)
    {
        if (game.Pending is { Asking: Marvel.Rules.Prompts.Question.Element } prompt
            && prompt.Affordances.Count == 1 && prompt.Affordances[0].Targets is not null)
            game.Resolve(new Marvel.Rules.Play.Decision(prompt.Affordances[0].Id,
                world.Seats[owner].Hand.Cards.Select(card => card.ObjectId).ToArray(), []));
    }

    internal static void EmptyHand(World world, int owner)
    {
        foreach (var card in world.Seats[owner].Hand.Cards.ToArray())
            World.MoveToTop(card, world.Seats[owner].Deck);
    }
}
