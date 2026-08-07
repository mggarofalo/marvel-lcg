from . import *

# * Jolt

def GetAbilities() -> Sequence['Ability']:

    def jolt_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        this.PlaceThreatOnSchemes("MainScheme", 3, effect)

    def jolt(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'parley', effect)
        if this.GetCounters('parley') >= 3:
            Faces.RemoveAllFromGame([this], effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            jolt_defeated
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            jolt
        ).SetCostFunc(CostFunc.Exhaust("YourHero")),
    ]

