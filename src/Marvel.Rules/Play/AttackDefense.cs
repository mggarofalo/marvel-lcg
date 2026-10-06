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
        var opportunity = DefenseOpportunity.Next(world, attack,
            AttackDefenderCandidates.Choice(world, facts, abilities, attack));
        if (opportunity is null)
        {
            return null;
        }

        var choice = opportunity.Choice;
        var seat = world.Seats[opportunity.Player];
        Card enemy = world.Cards[attack.Enemy];
        long attackValue = StateFields.Modified(
            world, enemy, "attack", facts, world.Players);
        return new Prompt(
            Player: opportunity.Player,
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
                // The answering player controls every offered character.
                // rr:defend-defense.5 permits their defense of another player.
                .. choice.Candidates.Select(card => new Affordance(
                    Id: card.ObjectId,
                    Verb: DefenseVerb,
                    AnchorId: card.ObjectId,
                    AnchorPlayer: card.Area.PlayArea.Player,
                    Label: DefenseVerb,
                    Description: DefenseDescription(world, facts, card))),
            ])
        {
            ContextCardIds = [attack.Enemy, attack.Target],
            DisplayQuestion = opportunity.Player == attack.Player
                ? "Declare your defender"
                : $"Defend {EffectiveCards.Title(world.Cards[attack.Target], facts)}?",
            DeclineLabel = DeclineLabel(attack, opportunity),
            Description = Description(world, facts, attack, opportunity, attackValue),
        };
    }

    private static string DeclineLabel(EnemyAttack attack, DefenseOpportunity opportunity)
    {
        if (opportunity.HasLaterPlayer) return "Pass defense opportunity";
        if (attack.IsDefended) return "Keep current defense";
        return opportunity.Player == attack.Player
            ? "Leave attack undefended"
            : "Pass; leave attack undefended";
    }

    private static string Description(
        World world, ICardFacts facts, EnemyAttack attack, DefenseOpportunity opportunity, long attackValue)
    {
        var enemy = world.Cards[attack.Enemy];
        var choice = opportunity.Choice;
        return $"{EffectiveCards.Title(enemy, facts)} is attacking "
                + $"{EffectiveCards.Title(world.Cards[attack.Target], facts)}. "
                + $"ATK {attackValue} {AttackBoostDescription.Before(world, facts, enemy, Steps.DeclareDefender)}. "
                + (attack.IsDefended
                    ? $"{EffectiveCards.Title(world.Cards[attack.Defender], facts)} is already defending. "
                        + (choice.Required ? "Use basic defense." : "Use basic defense or keep the current defense.")
                    : choice.Required ? "Choose a ready hero or ally to defend."
                    : opportunity.Player != attack.Player
                        ? HelperDescription(opportunity)
                        : "Choose a ready hero or ally to defend, or leave the attack undefended.");
    }

    private static string HelperDescription(DefenseOpportunity opportunity)
        => "Choose one of your ready heroes or allies to defend, or pass your defense opportunity. "
            + (opportunity.HasLaterPlayer
                ? "Passing offers the next eligible player a defense opportunity."
                : "No other player can use basic defense. Passing leaves this attack undefended.");

    // rr:defend-defense.2: "A hero must exhaust to use this power" and damage is reduced by DEF.
    // rr:defend-defense.3: "An ally can exhaust to defend"; attack damage is dealt to that ally.
    private static string DefenseDescription(World world, ICardFacts facts, Card card)
    {
        Card enemy = world.Cards[AttackCompletion.Current(world).Enemy];
        string uncertainty = AttackBoostDescription.Remaining(world, facts, enemy, Steps.DeclareDefender);
        return EffectiveCards.Kind(card, facts) == CardKind.Hero
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
        var opportunity = DefenseOpportunity.Next(world, attack,
            AttackDefenderCandidates.Choice(world, facts, abilities, attack))
            ?? throw new RulesNotImplementedException("no basic-defense opportunity is pending");
        var choice = opportunity.Choice;
        if (input.IsDecline)
        {
            if (choice.Required)
            {
                throw new RulesNotImplementedException(
                    $"the attack by card {attack.Enemy} requires a defender and cannot be "
                    + "declined");
            }

            world.Attack = attack with
            {
                DefensePlayersPassed = [.. attack.DefensePlayersPassed, opportunity.Player],
            };
            return;
        }

        var defender = choice.Candidates
            .FirstOrDefault(card => card.ObjectId == input.Affordance)
            ?? throw new RulesNotImplementedException(
                $"card {input.Affordance} was not offered as a defender");

        AttackDefenseCommitment.Apply(world, facts, defender, events);
    }

}
