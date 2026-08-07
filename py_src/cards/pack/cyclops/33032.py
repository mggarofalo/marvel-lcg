from . import *

# Marked

def GetAbilities() -> Sequence['Ability']:

    def marked(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.GainOverKill(effect)

    return [
        AbilityFactory.CanPlayThisUpgradeCard(Minion),
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.NonKeyword,
            None,
            "AttachedMinion",
            marked,
        ),
    ]

