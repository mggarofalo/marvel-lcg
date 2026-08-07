from . import *

# * Sugar Man

def GetAbilities() -> Sequence['Ability']:

    def sugar_man(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        message.GainPiercing(effect)

        def action(face: 'Unit2'):
            this.HealthUnits([this], 5, effect)
        message.IfThisAttackDefeats(Unit2, action, effect)

    return [
        AbilityFactory.ThisMinionEngageFirstPlayer(),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            "This",
            sugar_man
        ),
    ]

