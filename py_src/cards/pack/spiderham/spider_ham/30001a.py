from . import *

# * Spider-Ham

def GetAbilities() -> Sequence['Ability']:

    def spider_ham(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'toon', effect)

    def available_times(effect: 'Effect', message: 'Message.WhenPlayerPayingResources') -> int:
        this = effect.this.CastTo(Hero)
        name = 'toon'
        num = this.GetCounters(name)
        return num

    return [
        AbilityFactory.CanGenerateResources(
            # `AbilityType.NonKeyword` will cause this ability become a forced
            AbilityType.HeroResource,
            Resources("G")
        ).SetCostFunc(CostFunc.Counter("This", 1, 'toon'))
        .SetToAvailableTimesFn(available_times),
        AbilityFactory.AfterUnitTookDamage(
            AbilityType.Response,
            "This",
            spider_ham
        ).SetName("Spider-Nonsense")
    ]

