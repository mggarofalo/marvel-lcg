from . import *

# Hobgoblin

def GetAbilities() -> Sequence['Ability']:

    def hobgoblin(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        if player:
            identity = player.GetIdentity()
            identity.TakeIndirectDamage(this, 2, effect)


    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            hobgoblin
        ),
    ]

