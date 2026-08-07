from . import *

# Leviathan Soldier

def GetAbilities() -> Sequence['Ability']:

    def leviathan_soldier(effect: 'Effect', message: 'Message.AfterUnitSchemeEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = this.GetEngagedPlayer()
        identity = player.GetIdentity()
        this.DealDamage([identity], 1, effect)


    return [
        AbilityFactory.AfterUnitSchemeEnd(
            AbilityType.ForcedResponse,
            "This",
            leviathan_soldier
        ),
    ]

