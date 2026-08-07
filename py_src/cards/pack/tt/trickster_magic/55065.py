from . import *

# * Whirlwind: David Cannon

def GetAbilities() -> Sequence['Ability']:

    def whirlwind(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        value = this.RemoveThreatFromSchemes(effect.targets, 1, effect)
        if value:
            message.GainATKForThisAttack(value, effect)


    return [
        *AbilityFactory.ThisNotCountAllyLimit(),
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.Interrupt,
            "This",
            whirlwind
        ).SetTarget(Scheme2, range="All"),
    ]

