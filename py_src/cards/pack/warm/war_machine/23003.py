from . import *

# Munitions Bunker

def GetAbilities() -> Sequence['Ability']:

    def munitions_bunker(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        Faces.PlaceCountersOn([this], 2, 'ammo', effect)

    def munitions_bunker_hero(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        value = this.GetCounters('ammo')
        Faces.RemoveCountersOn([this], "All", 'ammo', effect)
        Faces.PlaceCountersOn(effect.targets, value, 'ammo', effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            munitions_bunker
        ).SetCostFunc(CostFunc.Exhaust("This")),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            munitions_bunker_hero
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(name="War Machine"),
    ]

