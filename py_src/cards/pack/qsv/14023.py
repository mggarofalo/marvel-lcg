from . import *

# Civic Duty

def GetAbilities() -> Sequence['Ability']:

    def civic_duty(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        hero.GainUntilPhaseEnd(effect, thwart=1)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            civic_duty
        ).SetCostFunc(CostFunc.Discard("This"))
    ]

