from . import *

# * Moondragon: Heather Douglas

def GetAbilities() -> Sequence['Ability']:

    def moondragon(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        for target in effect.targets:
            unit = target.CastTo(Minion)

            units = Worlds.GetOnFieldEnemies(effect)
            units.remove(unit)

            enemy = initiator.AskChooseFace(units, effect)
            if enemy:
                unit.BasicAttack([enemy], effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            moondragon
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Discard("This"))
        .SetTarget(Minion)
    ]

