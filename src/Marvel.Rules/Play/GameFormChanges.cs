using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Explicit voluntary identity destinations and their live admission.</summary>
internal static class GameFormChanges
{
    internal static void AddOffers(Game game, Seat seat, List<Affordance> options)
    {
        if (seat.FormChangedInRound == game.Round
            || !FormChangePermissions.CanChange(game.world, seat.IdentityCard)) return;
        if (seat.IdentityCard.Faces.Count == 2)
        {
            options.Add(game.Anchored(Game.ChangeForm, seat));
            return;
        }
        foreach (string destination in Forms.Destinations(seat, game.facts))
        {
            // Engine choice: the existing semantic option label is the printed
            // destination id. Handle allocation distinguishes each destination;
            // clients display the supplied wording and submit the offered handle.
            string name = game.facts.Title(destination);
            string traits = string.Join(", ", game.facts.PrintedTraits(destination));
            string display = $"Change to {name} ({traits})";
            options.Add(game.Anchored(Game.ChangeForm, seat) with
            {
                Id = game.Handle($"{Game.ChangeForm}:{destination}", seat.IdentityCard.ObjectId),
                Label = destination,
                DisplayLabel = display,
                CommitLabel = display,
                Description = $"Change your identity to {destination}. Uses your voluntary form change this round.",
            });
        }
    }

    internal static string? Destination(Game game, Decision input)
    {
        var offer = game.Pending!.Affordances.Single(item => item.Id == input.Affordance);
        return offer.Label == Game.ChangeForm ? null : offer.Label;
    }

    internal static void Validate(Game game, Decision input)
    {
        if (input.IsDecline || game.Phase != GamePhase.PlayerTurn || !game.IsRootPrompt) return;
        var offer = game.Pending!.Affordances.Single(item => item.Id == input.Affordance);
        if (offer.Verb != Game.ChangeForm) return;
        if (!StillLegal(game, offer, Destination(game, input)))
            throw new ArgumentException("the offered identity form change is no longer legal", nameof(input));
    }

    private static bool StillLegal(Game game, Affordance offer, string? destination)
    {
        var seat = game.world.Seats[game.Active];
        return seat.FormChangedInRound != game.Round
            && offer.AnchorId == seat.IdentityCard.ObjectId
            && FormChangePermissions.CanChange(game.world, seat.IdentityCard)
            && (destination is null || Forms.Destinations(seat, game.facts).Contains(destination));
    }
}
