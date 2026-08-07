from . import *

# Heavy Cannon

def GetAbilities() -> Sequence['Ability']:

    def heavy_cannon(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        targets = effect.targets[:]

        initiator = effect.GetInitiator()
        if initiator.IsControl(CardFinder(name="Milano")):
            villain = Worlds.FindVillain(effect)
            if villain:
                targets.append(villain)

        this.DealDamage(targets, 1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            heavy_cannon
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Enemy, range="All"),
    ]

