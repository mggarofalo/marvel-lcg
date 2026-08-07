from . import *

# Befuddle

def GetAbilities() -> Sequence['Ability']:

    def befuddle(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        attacker = message.attacker
        attack = 0
        thwart = 0
        if HasAttack.IsType(attacker):
            attack = attacker.attack
        if HasThwart.IsType(attacker):
            thwart = attacker.thwart
        value = thwart - attack
        message.GainAttackForThisAttack(value, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(Minion),
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.Interrupt,
            None,
            "AttachedMinion",
            befuddle,
            is_basic_attack=True,
        ),
    ]

