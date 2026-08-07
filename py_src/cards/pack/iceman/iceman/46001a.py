from . import *

# * Iceman

def GetAbilities() -> Sequence['Ability']:

    def iceman(effect: 'Effect', message: 'Message.WhenUnitWouldAttack|Message.WhenUnitWouldDefend') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        face = GetSetAsideFrostbite(initiator)

        if face:
            enemy = effect.targets[0]
            face.AttachTo2(enemy, effect)


    return [
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.Interrupt,
            "This",
            iceman,
            is_basic_attack=True
        ).SetName('"Freeze!"')
        .SetTarget("Targets"),
        AbilityFactory.WhenUnitDefendAgainstAttack(
            AbilityType.Interrupt,
            "This",
            iceman,
        ).SetName('"Freeze!"')
        .SetTarget("Attacker"),
    ]

