from . import *

# * Radioactive Man

def GetAbilities() -> Sequence['Ability']:

    def radioactive_man(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        this.DealDamage(player.GetControlCharacters(), 1, effect)


    return [
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "This",
            radioactive_man
        ),
    ]

