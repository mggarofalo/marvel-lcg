from . import *

# Spider-Sense

def GetAbilities() -> Sequence['Ability']:

    def spider_sense(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            player.DrawUp(1, effect)


    return [
        AbilityFactory.WhenUnitInitiatesAttackAgainst(
            AbilityType.HeroInterrupt,
            Villain,
            "You",
            spider_sense
        ).SetTarget("Initiator"),
    ]

