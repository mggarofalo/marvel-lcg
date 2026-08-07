from . import *

# Skilled Strike

def GetAbilities() -> Sequence['Ability']:

    def skilled_strike(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        for target in effect.targets:
            if Unit2.IsType(target):
                target.GainForThisActive(effect, message, attack=2)

    return [
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.Interrupt,
            "YourHero",
            skilled_strike,
            is_basic_attack=True
        ).SetPlay().SetLabel()
        .SetTarget("Attacker"),
    ]

