from . import *

# Cargo Hold

def GetAbilities() -> Sequence['Ability']:

    def cargo_hold(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        this.HealthUnits(effect.targets, 1, effect)
        if initiator.IsControl(CardFinder(name="Milano")):
            this.HealthUnits([initiator.GetIdentity()], 1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            cargo_hold
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Friend, canbe_heal=True),
    ]

