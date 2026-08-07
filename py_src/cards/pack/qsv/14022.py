from . import *

# Adrenaline Rush

def GetAbilities() -> Sequence['Ability']:

    def adrenaline_rush(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        hero.GainUntilPhaseEnd(effect, attack=1)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            adrenaline_rush
        ).SetCostFunc(CostFunc.Discard("This"))
    ]

