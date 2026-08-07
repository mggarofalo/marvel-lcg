from . import *

# Rapid Growth

def GetAbilities() -> Sequence['Ability']:

    def rapid_growth(effect: 'Effect', message: 'Message.WhenUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        hero.ChangeToForm(CardFinder2("GIANT"), effect)
        message.GainValue(+2, effect)


    return [
        AbilityFactory.WhenUnitUseBasicPower(
            AbilityType.HeroInterrupt,
            "You",
            rapid_growth,
            powers=["ATK", "THW", "DEF"]
        ).SetPlay().SetLabel(),
    ]

