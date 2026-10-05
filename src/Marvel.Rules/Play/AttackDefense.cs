using static Marvel.Rules.Play.Attack;
using static Marvel.Rules.Play.AttackBoost;
using static Marvel.Rules.Play.AttackDamage;
using static Marvel.Rules.Play.AttackCompletion;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Defender selection and commitment for an enemy attack.</summary>
internal static class AttackDefense
{
    /// <summary>
    /// Step 2. Whether anybody defends —
    /// <c>rr:attack-enemy-activation.step.2</c>.
    /// </summary>
    /// <remarks>
    /// Asked only where there is something to ask. A player with no ready
    /// character cannot defend (<c>rr:defend-defense.2</c> and <c>.3</c> both
    /// require exhausting one), so there is no question to put and the step
    /// passes in silence — the same rule <see cref="Offering"/> applies to
    /// windows.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">Card-specific defender restrictions.</param>
    /// <returns>The question, or null when nobody could defend.</returns>
    public static Prompt? DeclareDefender(
        World world, ICardFacts facts, IAttackCardAbilities abilities)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);

        // `rr:activation.6` -- "if an activating minion leaves play, that
        // minion's activation ends immediately and no further steps of that
        // activation resolve."
        if (AttackCompletion.Over(world))
        {
            return null;
        }

        var attack = AttackCompletion.Current(world);
        var choice = AttackDefenderCandidates.Choice(world, facts, abilities, attack);
        if (choice.Candidates.Count == 0)
        {
            return null;
        }

        var seat = world.Seats[attack.Player];
        Card enemy = world.Cards[attack.Enemy];
        long attackValue = StateFields.Modified(
            world, enemy, "attack", facts, world.Players);
        return new Prompt(
            Player: attack.Player,
            Asking: Question.Defender,
            When: TimingPriority.Untimed,
            Trigger: Steps.AttackInitiated,
            Label: $"{seat.Name} declares a defender",

            // `rr:attack-enemy-activation.4` ordinarily permits no defender.
            // A card instruction can make defending mandatory when its stated
            // kind of defender is able, which is why this is card-provided.
            Cancellable: !choice.Required,
            Affordances:
            [
                // `AnchorPlayer` is whose character it is, which is not
                // always the player being asked: `rr:defend-defense.5` lets
                // somebody else's hero or ally defend, and taking over makes
                // them the attack's new target.
                .. choice.Candidates.Select(card => new Affordance(
                    Id: card.ObjectId,
                    Verb: DefenseVerb,
                    AnchorId: card.ObjectId,
                    AnchorPlayer: card.Area.PlayArea.Player,
                    Label: DefenseVerb,
                    Description: DefenseDescription(world, facts, card))),
            ])
        {
            DeclineLabel = attack.IsDefended ? "Keep current defense" : "Leave attack undefended",
            Description = $"{FacedownDrones.Title(enemy, facts)} is attacking "
                + $"{FacedownDrones.Title(world.Cards[attack.Target], facts)}. "
                + $"ATK {attackValue} {AttackBoostDescription.Before(world, facts, enemy, Steps.DeclareDefender)}. "
                + (attack.IsDefended
                    ? $"{FacedownDrones.Title(world.Cards[attack.Defender], facts)} is already defending. "
                        + (choice.Required ? "Use basic defense." : "Use basic defense or keep the current defense.")
                    : choice.Required ? "Choose a ready hero or ally to defend."
                    : "Choose a ready hero or ally to defend, or leave the attack undefended."),
        };
    }

    // rr:defend-defense.2: "A hero must exhaust to use this power" and damage is reduced by DEF.
    // rr:defend-defense.3: "An ally can exhaust to defend"; attack damage is dealt to that ally.
    private static string DefenseDescription(World world, ICardFacts facts, Card card)
    {
        Card enemy = world.Cards[AttackCompletion.Current(world).Enemy];
        string uncertainty = AttackBoostDescription.Remaining(world, facts, enemy, Steps.DeclareDefender);
        return facts.Kind(card.FaceId) == CardKind.Hero
            ? $"Exhaust {facts.Title(card.FaceId)}. Reduce attack damage by DEF {StateFields.Modified(world, card, "defense", facts, world.Players)}; this hero takes remaining damage. {uncertainty}"
            : $"Exhaust {facts.Title(card.FaceId)}. This ally takes the attack damage; no basic DEF reduction. {uncertainty}";
    }

    /// <summary>The character that answer names becomes the defender.</summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">Card-specific defender restrictions.</param>
    /// <param name="input">The player's answer.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Defend(
        World world, ICardFacts facts, IAttackCardAbilities abilities, Decision input,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(events);

        var attack = AttackCompletion.Current(world);
        var choice = AttackDefenderCandidates.Choice(world, facts, abilities, attack);
        if (input.IsDecline)
        {
            if (choice.Required)
            {
                throw new RulesNotImplementedException(
                    $"the attack by card {attack.Enemy} requires a defender and cannot be "
                    + "declined");
            }

            return;
        }

        var defender = choice.Candidates
            .FirstOrDefault(card => card.ObjectId == input.Affordance)
            ?? throw new RulesNotImplementedException(
                $"card {input.Affordance} was not offered as a defender");

        // rr:defend-defense.2 and .3 -- both require exhausting the defender.
        defender.Exhaust();
        events.Add(new FieldSet(defender.ObjectId, "is_exhaust", 0, 1)
        {
            Trigger = Steps.AttackInitiated,
            Verb = DefenseVerb,
        });

        // **The defender becomes the target, whoever they are.**
        // `rr:defend-defense.3.1` for an ally: "that ally becomes the target
        // character for that attack, and its controller becomes the target
        // player". `rr:defend-defense.2` for a hero: "any remaining damage is
        // dealt to that hero". And `.5`: "if a player defends against an enemy
        // attack that targets a different player [...] the defending player
        // becomes the new target of that attack."
        //
        // Three clauses, one move. When the target player defends with their
        // own hero it changes nothing, which is why it read as "the target does
        // not change" while one player was all the engine had.
        //
        // `BasicDefense` is the hero's alone: `rr:defend-defense.2`'s reduction
        // belongs to the basic defense power, and `.3` gives an ally none.
        world.Attack = attack with
        {
            Defender = defender.ObjectId,
            Target = defender.ObjectId,
            Player = defender.Area.PlayArea.Player,
            BasicDefense = facts.Kind(defender.FaceId) != CardKind.Ally,
        };
        if (world.Activation is { Attacking: true } activation)
        {
            world.Activation = activation with { Player = defender.Area.PlayArea.Player };
        }
    }

}
