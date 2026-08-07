from . import *

# * Jack Flag: Jack Harrison

def GetAbilities() -> Sequence['Ability']:

    def jack_flag(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)

    def place_ammo(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        Faces.PlaceCountersOn([this], 1, 'ammo', effect)

    return [
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.Response,
            "This",
            place_ammo
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            jack_flag
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'ammo'))
        .SetTarget(Enemy),
    ]

