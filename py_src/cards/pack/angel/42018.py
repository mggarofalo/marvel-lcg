from . import *

# * Angel's Aerie

def GetAbilities() -> Sequence['Ability']:

    def angels_aerie_respone(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'fatigue', effect)

    def angels_aerie(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        value = effect.cost_func.Get(CostFunc.RemoveEachCounter).return_remove_counter
        this.HealthUnits(effect.targets, value, effect)


    return [
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.Response,
            "You",
            angels_aerie_respone
        ).SetTarget("This"),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            angels_aerie
        ).SetCostFunc(CostFunc.RemoveEachCounter("This", 'fatigue'))
        .SetTarget("YourIdentity", canbe_heal=True),
    ]

