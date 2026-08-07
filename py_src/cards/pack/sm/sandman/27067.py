from . import *

# Sand Clone

def GetAbilities() -> Sequence['Ability']:

    def sand_clone(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        ResolveSurgingSandsOnCityStreets(effect)

    def get_city_streets_sand(effect: 'Effect', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Minion)
        Unused(this)

        city_streets = GetCityStreets(effect)
        if city_streets:
            ui.append(city_streets)
            return city_streets.GetCounters('sand')
        return 0

    return [
        AbilityFactory.ThisGainKeyword(
            get_city_streets_sand,
            attack=1,
            change_on_event=OnEvent.Counter(CardFinder(name="City Streets"), 'sand')
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            sand_clone
        ),
    ]

