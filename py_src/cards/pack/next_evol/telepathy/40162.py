from . import *

# One Step Ahead

def GetAbilities() -> Sequence['Ability']:

    def one_step_ahead_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = Worlds.FindVillain(effect)
        if villain and villain.HasTrait("AERIAL"):
            value = 2
        else:
            value = 1

        player.DiscardRandomHandCards(value, effect)

    def one_step_ahead_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        if message.would_atk_message.attacker and \
            Villain.IsType(message.would_atk_message.attacker):
            message.would_atk_message.GainOverKill(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            one_step_ahead_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            one_step_ahead_boost,
            during_attack=True
        ),
    ]

