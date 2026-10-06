using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>One controller's basic-defense opportunity inside the same attack step.</summary>
internal sealed record DefenseOpportunity(int Player, DefenderChoice Choice, bool HasLaterPlayer)
{
    internal static DefenseOpportunity? Next(World world, EnemyAttack attack, DefenderChoice legal)
    {
        // rr:attack-enemy-activation.step.2: "If a player wishes to defend,
        // that player exhausts a hero or ally as the defender."
        // Helpers first, in player order, then the attacked player is our
        // scheduling policy. Passing never spends another controller's choice.
        var players = world.PlayerOrder
            .Where(player => player != attack.Player)
            .Append(attack.Player)
            .Where(player => !attack.DefensePlayersPassed.Contains(player))
            .Where(player => legal.Candidates.Any(card => card.Area.PlayArea.Player == player))
            .ToList();
        if (players.Count == 0) return null;
        int owner = players[0];
        bool later = players.Count > 1;
        var candidates = legal.Candidates.Where(card => card.Area.PlayArea.Player == owner).ToList();
        return new DefenseOpportunity(owner,
            new DefenderChoice(candidates, legal.Required && !later), later);
    }
}
