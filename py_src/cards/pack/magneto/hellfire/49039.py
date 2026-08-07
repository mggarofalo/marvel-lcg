from . import *

# * Selene

def GetAbilities() -> Sequence['Ability']:

    def selene_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        player.DiscardControlCards(effect, ally=True)


    return [
        *AbilityFactory.UnitCannotAttackTarget(
            Ally,
            cannot_attack="This",
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            selene_boost
        ),
    ]

