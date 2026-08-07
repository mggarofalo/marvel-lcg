from . import *

# Propulsion Gauntlet

def GetAbilities() -> Sequence['Ability']:

    def propulsion_gauntlet(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

        initiator = effect.GetInitiator()
        initiator.GetHero().GainUntilPhaseEnd(
            effect,
            thwart=1,
            attack=1,
            defense=1
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            propulsion_gauntlet
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.TakeIndirectDamage(2))
        .SetTarget("YourHero", canbe_ready=True)
        .SetTarget2("YourHero"),
    ]

