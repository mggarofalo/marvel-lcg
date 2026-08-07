from . import *

# Phased and Confused

def GetAbilities() -> Sequence['Ability']:

    def phased_and_confused(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.SetBeInstead(effect)

        Faces.DiscardAll([this], effect)
        Faces.GiveStatus([message.attacker], "Confused", effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            Enemy
        ).SetPlay(hero_form_only=True),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            "AttachedEnemy",
            phased_and_confused
        ),
    ]

