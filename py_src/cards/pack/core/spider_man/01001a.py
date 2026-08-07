from . import *

# * Spider-Man

def GetAbilities() -> Sequence['Ability']:

    def spider_man(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenUnitInitiatesAttackAgainst(
            AbilityType.Interrupt,
            Villain,
            "You",
            spider_man
        ).SetName("Spider-Sense")
    ]

