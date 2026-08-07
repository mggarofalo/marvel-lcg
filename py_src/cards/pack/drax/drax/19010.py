from . import *

# DWI Theet Mastery

def GetAbilities() -> Sequence['Ability']:

    def dwi_theet_mastery(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            player.DrawUp(1, effect)


    return [
        AbilityFactory.AfterUnitMakeBasicAttack(
            AbilityType.HeroResponse,
            "YourHero",
            dwi_theet_mastery,
        ).SetTarget("Initiator")
    ]

