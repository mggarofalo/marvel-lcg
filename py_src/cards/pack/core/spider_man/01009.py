from . import *

# Webbed Up

def GetAbilities() -> Sequence['Ability']:

    def webbed_up(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        message.SetBeInstead(effect)
        this = effect.this.CastTo(Upgrade)
        Faces.DiscardAll([this], effect)
        Faces.GiveStatus(effect.targets, "Stunned", effect)

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            Enemy,
        ).SetPlay(hero_form_only=True),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            "AttachedEnemy",
            webbed_up
        ).SetTarget("Trigger")
    ]

