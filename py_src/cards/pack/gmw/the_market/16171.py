from . import *

# Mounted Laser

def GetAbilities() -> Sequence['Ability']:

    def mounted_laser(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        if initiator.IsControl(CardFinder(name="Milano")):
            damage = 3
        else:
            damage = 2
        this.DealDamage(effect.targets, damage, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            mounted_laser
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Enemy),
    ]

