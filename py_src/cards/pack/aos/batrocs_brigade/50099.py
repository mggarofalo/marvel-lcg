from . import *

# * Rapido

def GetAbilities() -> Sequence['Ability']:

    def rapido(effect: 'Effect', message: 'Message.WhenCardRevealed|Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        this.DealDamage(player.GetControlCharacters(), 1, effect)

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            ranged=True,
            indirect_damage=True,
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            rapido
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            rapido
        ),
    ]

