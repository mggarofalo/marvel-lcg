from . import *

# Battery Pack

def GetAbilities() -> Sequence['Ability']:

    def battery_pack(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        upgrade = effect.targets[0].CastTo(Upgrade)
        Faces.PlaceCountersOn([upgrade], 1, 'charge', effect)


    return [
        AbilityFactory.ThisEnterPlayWithCounters(2, "charge"),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            battery_pack
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Upgrade, trait="TECH", another=True, from_where=["YouControlCards"])
        .SetCostFunc(CostFunc.Counter("This", 1, "charge")),
    ]

