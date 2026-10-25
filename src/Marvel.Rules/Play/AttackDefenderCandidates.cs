using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Ready defender candidates and card-owned restrictions.</summary>
internal static class AttackDefenderCandidates
{
    /// <summary>The characters one player could exhaust to defend.</summary>
    internal static List<Card> Defenders(World world, ICardFacts facts)
    {
        var candidates = new List<Card>();

        // rr:defend-defense.5: a player may defend an attack targeting a
        // different player. Candidate legality is independent of whose current
        // opportunity authorizes the commitment.
        foreach (int player in world.PlayerOrder)
        {
            candidates.AddRange(For(world, facts, player));
        }

        return candidates;
    }

    /// <summary>Applies card-specific defender constraints to the rules candidates.</summary>
    internal static DefenderChoice Choice(
        World world, ICardFacts facts, IAttackCardAbilities abilities, EnemyAttack attack)
    {
        var legal = AvailableForAttack(world, facts, attack);
        var required = AttackAllyDefenseRequirement.RequiredAllies(world);
        if (!attack.IsDefended && required.Count > 0)
            legal = [.. legal.Where(card => required.Contains(card))];
        var choice = abilities.Defenders(world, attack, legal);
        if (!attack.IsDefended && required.Count > 0)
            choice = new DefenderChoice(choice.Candidates, Required: true);
        if (choice.Required && choice.Candidates.Count == 0)
        {
            throw new RulesNotImplementedException(
                $"card {attack.Enemy} requires a defender but offers no legal candidate");
        }

        var legalIds = legal.Select(card => card.ObjectId).ToHashSet();
        if (choice.Candidates.Any(card => !legalIds.Contains(card.ObjectId)))
        {
            throw new RulesNotImplementedException(
                $"card {attack.Enemy} offered a character that cannot defend");
        }

        return choice;
    }

    private static List<Card> AvailableForAttack(World world, ICardFacts facts, EnemyAttack attack)
    {
        if (!attack.IsDefended) return Defenders(world, facts);
        var current = world.Cards[attack.Defender];
        return !attack.BasicDefense && current.Ready
            && EffectiveCards.Kind(current, facts) == CardKind.Hero
            && BasicPowers.CanUsePower(facts, current, "DEF") ? [current] : [];
    }

    /// <summary>One player's characters that could defend.</summary>
    internal static List<Card> For(World world, ICardFacts facts, int player)
    {
        var seat = world.Seats[player];
        if (seat.Eliminated) return [];

        var candidates = new List<Card>();
        var identity = seat.IdentityCard;

        // rr:defend-defense.2 -- the basic defense power belongs to a hero. An
        // alter-ego has no DEF and cannot make one, and an exhausted hero has
        // nothing left to exhaust.
        if (identity.Ready
            && EffectiveCards.Kind(identity, facts) == CardKind.Hero
            && BasicPowers.CanUsePower(facts, identity, "DEF"))
        {
            candidates.Add(identity);
        }

        // rr:defend-defense.3 -- "an ally can exhaust to defend against an
        // enemy attack. Damage from the attack is dealt to that ally."
        candidates.AddRange(world.Areas
            .Where(area => area.Type == DeckType.AlliesArea
                && area.PlayArea == PlayArea.Of(player))
            .SelectMany(area => area.Cards)
            .Where(ally => ally.Ready));

        return candidates;
    }

}
