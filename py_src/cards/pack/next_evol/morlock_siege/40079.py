from . import *

# Morlock

def GetAbilities() -> Sequence['Ability']:

    def morlock(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        message.ReplaceTarget(this)

    return [
        *AbilityFactory.ThisNotCountAllyLimit(),
        AbilityFactory.ThisCannotRemoveFromPlay(),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            Enemy,
            morlock,
            against_player="You",
        ).SetTarget("Attacker")
    ]

