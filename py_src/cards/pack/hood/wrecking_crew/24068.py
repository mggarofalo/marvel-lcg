from . import *

# * Thunderball

def GetAbilities() -> Sequence['Ability']:

    def thunderball(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        this.DealDamage(player.GetControlCharacters(), 1, effect)


    return [
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedResponse,
            "This",
            "You",
            thunderball
        ),
    ]

